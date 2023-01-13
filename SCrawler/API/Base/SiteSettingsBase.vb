' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Collections.Generic
Imports System.Drawing
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Functions.UniversalFunctions
Imports PersonalUtilities.Tools.Web.Clients
Imports PersonalUtilities.Tools.Web.Cookies
Imports SCrawler.Plugin
Imports Download = SCrawler.Plugin.ISiteSettings.Download

Namespace API.Base

    Friend MustInherit Class SiteSettingsBase : Implements ISiteSettings, IResponserContainer

        Friend Sub New(SiteName As String)
            Site = SiteName
        End Sub

        Friend Sub New(SiteName As String, CookiesDomain As String)
            Site = SiteName
            Responser = New Responser($"{SettingsFolderName}\Responser_{Site}.xml")
            With Responser
                If .File.Exists Then
                    If EncryptCookies.CookiesEncrypted Then .CookiesEncryptKey = SettingsCLS.CookieEncryptKey
                    .LoadSettings()
                Else
                    .CookiesDomain = CookiesDomain
                    .Cookies = New CookieKeeper(.CookiesDomain) With {.EncryptKey = SettingsCLS.CookieEncryptKey}
                    .CookiesEncryptKey = SettingsCLS.CookieEncryptKey
                    .SaveSettings()
                End If
                If .CookiesDomain.IsEmptyString Then .CookiesDomain = CookiesDomain
            End With
        End Sub

        Friend Overridable ReadOnly Property Icon As Icon Implements ISiteSettings.Icon
        Friend Overridable ReadOnly Property Image As Image Implements ISiteSettings.Image
        Friend Overridable ReadOnly Property Responser As Responser
        Friend ReadOnly Property Site As String Implements ISiteSettings.Site

        Private Property IResponserContainer_Responser As Responser Implements IResponserContainer.Responser
            Get
                Return Responser
            End Get
            Set : End Set
        End Property

        Private Property Logger As ILogProvider = LogConnector Implements ISiteSettings.Logger

        Friend MustOverride Function GetInstance(What As Download) As IPluginContentProvider Implements ISiteSettings.GetInstance

#Region "XML"

        Friend Overridable Sub Load(XMLValues As IEnumerable(Of KeyValuePair(Of String, String))) Implements ISiteSettings.Load
        End Sub

#End Region

#Region "Initialize"

        Friend Overridable Sub BeginEdit() Implements ISiteSettings.BeginEdit
        End Sub

        Friend Overridable Sub BeginInit() Implements ISiteSettings.BeginInit
        End Sub

        Friend Overridable Sub BeginUpdate() Implements ISiteSettings.BeginUpdate
        End Sub

        Friend Overridable Sub EndEdit() Implements ISiteSettings.EndEdit
        End Sub

        Friend Overridable Sub EndInit() Implements ISiteSettings.EndInit
            EncryptCookies.ValidateCookiesEncrypt(Responser)
        End Sub

        Friend Overridable Sub EndUpdate() Implements ISiteSettings.EndUpdate
        End Sub

#End Region

#Region "Before and After Download"

        Friend Overridable Sub AfterDownload(User As Object, What As Download) Implements ISiteSettings.AfterDownload
        End Sub

        Friend Overridable Sub BeforeStartDownload(User As Object, What As Download) Implements ISiteSettings.BeforeStartDownload
        End Sub

        Friend Overridable Sub DownloadDone(What As Download) Implements ISiteSettings.DownloadDone
        End Sub

        Friend Overridable Sub DownloadStarted(What As Download) Implements ISiteSettings.DownloadStarted
        End Sub

#End Region

#Region "User info"

        Protected ImageVideoContains As String = String.Empty
        Protected UrlPatternChannel As String = String.Empty
        Protected UrlPatternUser As String = String.Empty
        Protected UserRegex As RParams = Nothing

        Friend Shared Function GetSpecialDataFile(Path As String, AskForPath As Boolean, ByRef SpecFolderObj As String) As SFile
            Dim f As SFile = Path.CSFileP
            If f.Name.IsEmptyString Then f.Name = "OutputFile"
#Disable Warning BC40000
            If Path.CSFileP.IsEmptyString Or AskForPath Then f = SFile.SaveAs(f, "File destination",,,, EDP.ReturnValue) : SpecFolderObj = f.Path
#Enable Warning
            Return f
        End Function

        Friend Overridable Function GetSpecialData(URL As String, Path As String, AskForPath As Boolean) As IEnumerable Implements ISiteSettings.GetSpecialData
            Return Nothing
        End Function

        Friend Overridable Function GetUserPostUrl(User As UserDataBase, Media As UserMedia) As String
            Return String.Empty
        End Function

        Friend Overridable Function GetUserUrl(User As IPluginContentProvider, Channel As Boolean) As String Implements ISiteSettings.GetUserUrl
            If Channel Then
                If Not UrlPatternChannel.IsEmptyString Then Return String.Format(UrlPatternChannel, User.Name)
            Else
                If Not UrlPatternUser.IsEmptyString Then Return String.Format(UrlPatternUser, User.Name)
            End If
            Return String.Empty
        End Function

        Friend Overridable Function IsMyImageVideo(URL As String) As ExchangeOptions Implements ISiteSettings.IsMyImageVideo
            If Not ImageVideoContains.IsEmptyString AndAlso URL.Contains(ImageVideoContains) Then
                Return New ExchangeOptions With {.Exists = True}
            Else
                Return Nothing
            End If
        End Function

        Friend Overridable Function IsMyUser(UserURL As String) As ExchangeOptions Implements ISiteSettings.IsMyUser
            Try
                If UserRegex IsNot Nothing Then
                    Dim s$ = RegexReplace(UserURL, UserRegex)
                    If Not s.IsEmptyString Then Return New ExchangeOptions(Site, s)
                End If
                Return Nothing
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.SendInLog + EDP.ReturnValue, ex, $"[API.Base.SiteSettingsBase.IsMyUser({UserURL})]", New ExchangeOptions)
            End Try
        End Function

        Private Function ISiteSettings_GetUserPostUrl(User As IPluginContentProvider, Media As IUserMedia) As String Implements ISiteSettings.GetUserPostUrl
            Return GetUserPostUrl(User, Media)
        End Function

#End Region

#Region "Ready, Available"

        Friend Overridable Function Available(What As Download, Silent As Boolean) As Boolean Implements ISiteSettings.Available
            Return BaseAuthExists()
        End Function

        Friend Overridable Function BaseAuthExists() As Boolean
            Return True
        End Function

        Friend Overridable Function ReadyToDownload(What As Download) As Boolean Implements ISiteSettings.ReadyToDownload
            Return True
        End Function

#End Region

        Friend Overridable Sub OpenSettingsForm() Implements ISiteSettings.OpenSettingsForm
        End Sub

        Friend Overridable Sub Reset() Implements ISiteSettings.Reset
        End Sub

        Friend Overridable Sub Update() Implements ISiteSettings.Update
            If Responser IsNot Nothing Then Responser.SaveSettings()
        End Sub

        Friend Overridable Sub UserOptions(ByRef Options As Object, OpenForm As Boolean) Implements ISiteSettings.UserOptions
            Options = Nothing
        End Sub

    End Class

End Namespace
