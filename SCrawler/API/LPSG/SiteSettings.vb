﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports SCrawler.Plugin
Imports SCrawler.Plugin.Attributes
Imports PersonalUtilities.Functions.RegularExpressions
Namespace API.LPSG
    <Manifest("AndyProgram_LPSG")>
    Friend Class SiteSettings : Inherits Base.SiteSettingsBase
        Friend Overrides ReadOnly Property Icon As Icon
            Get
                Return My.Resources.SiteResources.LPSGIcon_48
            End Get
        End Property
        Friend Overrides ReadOnly Property Image As Image
            Get
                Return My.Resources.SiteResources.LPSGPic_32
            End Get
        End Property
        Friend Sub New()
            MyBase.New("LPSG", "www.lpsg.com")
            UrlPatternUser = "https://www.lpsg.com/threads/{0}/"
            UserRegex = RParams.DMS(".+?lpsg.com/threads/([^/]+)", 1)
        End Sub
        Friend Overrides Function GetInstance(What As ISiteSettings.Download) As IPluginContentProvider
            Return New UserData
        End Function
        Friend Overrides Function Available(What As ISiteSettings.Download, Silent As Boolean) As Boolean
            Return If(Responser.Cookies?.Count, 0) > 0
        End Function
    End Class
End Namespace