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
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Tools.Web.Clients
Imports PersonalUtilities.Tools.Web.Cookies
Namespace API.Twitter
    <Manifest("AndyProgram_Twitter"), SavedPosts>
    Friend Class SiteSettings : Inherits SiteSettingsBase
        Friend Const Header_Authorization As String = "authorization"
        Friend Const Header_Token As String = "x-csrf-token"
        Friend Overrides ReadOnly Property Icon As Icon
            Get
                Return My.Resources.SiteResources.TwitterIcon_32
            End Get
        End Property
        Friend Overrides ReadOnly Property Image As Image
            Get
                Return My.Resources.SiteResources.TwitterPic_400
            End Get
        End Property
        <PropertyOption(AllowNull:=False, ControlText:="Authorization",
                        ControlToolTip:="Set authorization from [authorization] response header. This field must start from [Bearer] key word")>
        Private ReadOnly Property Auth As PropertyValue
        <PropertyOption(AllowNull:=False, ControlText:="Token", ControlToolTip:="Set token from [x-csrf-token] response header")>
        Private ReadOnly Property Token As PropertyValue
        <PropertyOption(ControlText:="Saved posts user", ControlToolTip:="Personal profile username"), PXML>
        Friend ReadOnly Property SavedPostsUserName As PropertyValue
        Friend Overrides ReadOnly Property Responser As Responser
        Friend Sub New()
            MyBase.New(TwitterSite)
            Responser = New Responser($"{SettingsFolderName}\Responser_{Site}.xml")

            Dim a$ = String.Empty
            Dim t$ = String.Empty

            With Responser
                If .File.Exists Then
                    If EncryptCookies.CookiesEncrypted Then .CookiesEncryptKey = SettingsCLS.CookieEncryptKey
                    .LoadSettings()
                    a = .Headers.Value(Header_Authorization)
                    t = .Headers.Value(Header_Token)
                Else
                    .ContentType = "application/json"
                    .Accept = "*/*"
                    .CookiesDomain = "twitter.com"
                    .Cookies = New CookieKeeper(.CookiesDomain) With {.EncryptKey = SettingsCLS.CookieEncryptKey}
                    .CookiesEncryptKey = SettingsCLS.CookieEncryptKey
                    .Decoders.Add(SymbolsConverter.Converters.Unicode)
                    .Headers.Add("sec-ch-ua", " Not;A Brand"";v=""99"", ""Google Chrome"";v=""91"", ""Chromium"";v=""91""")
                    .Headers.Add("sec-ch-ua-mobile", "?0")
                    .Headers.Add("sec-fetch-dest", "empty")
                    .Headers.Add("sec-fetch-mode", "cors")
                    .Headers.Add("sec-fetch-site", "same-origin")
                    .Headers.Add(Header_Token, String.Empty)
                    .Headers.Add("x-twitter-active-user", "yes")
                    .Headers.Add("x-twitter-auth-type", "OAuth2Session")
                    .Headers.Add(Header_Authorization, String.Empty)
                    .SaveSettings()
                End If
            End With

            Auth = New PropertyValue(a, GetType(String), Sub(v) ChangeResponserFields(NameOf(Auth), v))
            Token = New PropertyValue(t, GetType(String), Sub(v) ChangeResponserFields(NameOf(Token), v))
            SavedPostsUserName = New PropertyValue(String.Empty, GetType(String))

            UserRegex = RParams.DMS("[htps:/]{7,8}.*?twitter.com/([^/]+)", 1)
            UrlPatternUser = "https://twitter.com/{0}"
            ImageVideoContains = "twitter"
        End Sub
        Private Sub ChangeResponserFields(PropName As String, Value As Object)
            If Not PropName.IsEmptyString Then
                Dim f$ = String.Empty
                Select Case PropName
                    Case NameOf(Auth) : f = Header_Authorization
                    Case NameOf(Token) : f = Header_Token
                End Select
                If Not f.IsEmptyString Then
                    Responser.Headers.Remove(f)
                    If Not CStr(Value).IsEmptyString Then Responser.Headers.Add(f, CStr(Value))
                    Responser.SaveSettings()
                End If
            End If
        End Sub
        Friend Overrides Function GetInstance(What As ISiteSettings.Download) As IPluginContentProvider
            If What = ISiteSettings.Download.SavedPosts Then
                Return New UserData With {.IsSavedPosts = True, .User = New UserInfo With {.Name = CStr(AConvert(Of String)(SavedPostsUserName.Value, String.Empty))}}
            Else
                Return New UserData
            End If
        End Function
        Friend Overrides Function GetSpecialData(URL As String, Path As String, AskForPath As Boolean) As IEnumerable
            Return UserData.GetVideoInfo(URL, Responser)
        End Function
        Friend Overrides Function GetUserPostUrl(User As UserDataBase, Media As UserMedia) As String
            Return $"https://twitter.com/{User.Name}/status/{Media.Post.ID}"
        End Function
        Friend Overrides Function BaseAuthExists() As Boolean
            Return If(Responser.Cookies?.Count, 0) > 0 And ACheck(Token.Value) And ACheck(Auth.Value)
        End Function
    End Class
End Namespace