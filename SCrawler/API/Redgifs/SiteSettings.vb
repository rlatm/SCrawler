﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports SCrawler.API.Base
Imports SCrawler.Plugin
Imports SCrawler.Plugin.Attributes
Imports PersonalUtilities.Forms
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Tools.Web.Clients
Imports PersonalUtilities.Tools.Web.Documents.JSON
Imports UTypes = SCrawler.API.Base.UserMedia.Types
Imports UStates = SCrawler.API.Base.UserMedia.States
Namespace API.RedGifs
    <Manifest(RedGifsSiteKey)>
    Friend Class SiteSettings : Inherits SiteSettingsBase
#Region "Declarations"
        Friend Overrides ReadOnly Property Icon As Icon
            Get
                Return My.Resources.SiteResources.RedGifsIcon_32
            End Get
        End Property
        Friend Overrides ReadOnly Property Image As Image
            Get
                Return My.Resources.SiteResources.RedGifsPic_32
            End Get
        End Property
        <PropertyOption(ControlToolTip:="Bearer token", AllowNull:=False), ControlNumber(1)>
        Friend ReadOnly Property Token As PropertyValue
        <PXML> Friend ReadOnly Property TokenLastDateUpdated As PropertyValue
        Private Const TokenName As String = "authorization"
#Region "TokenUpdateInterval"
        <PropertyOption(ControlText:="Token refresh interval", ControlToolTip:="Interval (in minutes) to refresh the token", AllowNull:=False, LeftOffset:=120),
            PXML, ControlNumber(0)>
        Friend ReadOnly Property TokenUpdateInterval As PropertyValue
        Private Class TokenIntervalProvider : Implements IFieldsCheckerProvider
            Private Property ErrorMessage As String Implements IFieldsCheckerProvider.ErrorMessage
            Private Property Name As String Implements IFieldsCheckerProvider.Name
            Private Property TypeError As Boolean Implements IFieldsCheckerProvider.TypeError
            Private Function Convert(Value As Object, DestinationType As Type, Provider As IFormatProvider,
                                     Optional NothingArg As Object = Nothing, Optional e As ErrorsDescriber = Nothing) As Object Implements ICustomProvider.Convert
                TypeError = False
                ErrorMessage = String.Empty
                If Not ACheck(Of Integer)(Value) Then
                    TypeError = True
                ElseIf CInt(Value) > 0 Then
                    Return Value
                Else
                    ErrorMessage = $"The value of [{Name}] field must be greater than or equal to 1"
                End If
                Return Nothing
            End Function
            Private Function GetFormat(FormatType As Type) As Object Implements IFormatProvider.GetFormat
                Throw New NotImplementedException("[GetFormat] is not available in the context of [TokenIntervalProvider]")
            End Function
        End Class
        <Provider(NameOf(TokenUpdateInterval), FieldsChecker:=True)>
        Private ReadOnly Property TokenUpdateIntervalProvider As IFormatProvider
#End Region
#End Region
#Region "Initializer"
        Friend Sub New()
            MyBase.New(RedGifsSite, "redgifs.com")
            Dim t$ = String.Empty
            With Responser
                Dim b As Boolean = Not .Mode = Responser.Modes.WebClient
                .Mode = Responser.Modes.WebClient
                t = .Headers.Value(TokenName)
                If b Then .SaveSettings()
            End With
            Token = New PropertyValue(t, GetType(String), Sub(v) UpdateResponse(v))
            TokenLastDateUpdated = New PropertyValue(Now.AddYears(-1), GetType(Date))
            TokenUpdateInterval = New PropertyValue(60 * 12, GetType(Integer))
            TokenUpdateIntervalProvider = New TokenIntervalProvider
            UrlPatternUser = "https://www.redgifs.com/users/{0}/"
            UserRegex = RParams.DMS("[htps:/]{7,8}.*?redgifs.com/users/([^/]+)", 1)
            ImageVideoContains = "redgifs"
        End Sub
#End Region
#Region "Response updater"
        Private Sub UpdateResponse(Value As String)
            Responser.Headers.Add(TokenName, Value)
            Responser.SaveSettings()
        End Sub
#End Region
#Region "Token updaters"
        Friend Function UpdateTokenIfRequired() As Boolean
            Dim d As Date? = AConvert(Of Date)(TokenLastDateUpdated.Value, AModes.Var, Nothing)
            If Not d.HasValue OrElse d.Value < Now.AddMinutes(-CInt(TokenUpdateInterval.Value)) Then
                Return UpdateToken()
            Else
                Return True
            End If
        End Function
        <PropertyUpdater(NameOf(Token))>
        Friend Function UpdateToken() As Boolean
            Try
                Dim r$
                Dim NewToken$ = String.Empty
                Using resp As New Responser : r = resp.GetResponse("https://api.redgifs.com/v2/auth/temporary",, EDP.ThrowException) : End Using
                If Not r.IsEmptyString Then
                    Dim j As EContainer = JsonDocument.Parse(r)
                    If j IsNot Nothing Then
                        NewToken = j.Value("token")
                        j.Dispose()
                    End If
                End If
                If Not NewToken.IsEmptyString Then
                    Token.Value = $"Bearer {NewToken}"
                    TokenLastDateUpdated.Value = Now
                    Return True
                Else
                    Return False
                End If
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.SendInLog, ex, "[API.RedGifs.SiteSettings.UpdateToken]", False)
            End Try
        End Function
#End Region
#Region "Update settings"
        Private _LastTokenValue As String = String.Empty
        Friend Overrides Sub BeginEdit()
            _LastTokenValue = AConvert(Of String)(Token.Value, AModes.Var, String.Empty)
            MyBase.BeginEdit()
        End Sub
        Friend Overrides Sub Update()
            Dim NewToken$ = AConvert(Of String)(Token.Value, AModes.Var, String.Empty)
            If Not _LastTokenValue = NewToken Then TokenLastDateUpdated.Value = Now
            MyBase.Update()
        End Sub
        Friend Overrides Sub EndEdit()
            _LastTokenValue = String.Empty
            MyBase.EndEdit()
        End Sub
#End Region
        Friend Overrides Function GetInstance(What As ISiteSettings.Download) As IPluginContentProvider
            Return New UserData
        End Function
        Friend Overrides Function GetSpecialData(URL As String, Path As String, AskForPath As Boolean) As IEnumerable
            If BaseAuthExists() Then
                Using resp As Responser = Responser.Copy
                    Dim m As UserMedia = UserData.GetDataFromUrlId(URL, False, resp, Settings(RedGifsSiteKey))
                    If Not m.State = UStates.Missing And Not m.State = UserData.DataGone And (m.Type = UTypes.Picture Or m.Type = UTypes.Video) Then
                        Try
                            Dim spf$ = String.Empty
                            Dim f As SFile = GetSpecialDataFile(Path, AskForPath, spf)
                            If f.IsEmptyString Then
                                f = m.File.File
                            Else
                                f.Name = m.File.Name
                                f.Extension = m.File.Extension
                            End If
                            resp.DownloadFile(m.URL, f, EDP.ThrowException)
                            m.State = UStates.Downloaded
                            m.SpecialFolder = spf
                            Return {m}
                        Catch ex As Exception
                            ErrorsDescriber.Execute(EDP.SendInLog, ex, $"Redgifs standalone download error: [{URL}]")
                        End Try
                    End If
                End Using
            End If
            Return Nothing
        End Function
        Friend Overrides Function GetUserPostUrl(User As UserDataBase, Media As UserMedia) As String
            Return $"https://www.redgifs.com/watch/{Media.Post.ID}"
        End Function
        Friend Overrides Function BaseAuthExists() As Boolean
            Return UpdateTokenIfRequired() AndAlso ACheck(Token.Value)
        End Function
    End Class
End Namespace