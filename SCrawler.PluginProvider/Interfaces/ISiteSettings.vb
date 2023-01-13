' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Drawing
Namespace Plugin
    Public Interface ISiteSettings
        Enum Download As Integer
            Main = 0
            SavedPosts = 1
            Channel = 2
        End Enum
        ReadOnly Property Icon As Icon
        ReadOnly Property Image As Image
        ReadOnly Property Site As String
        Property Logger As ILogProvider
        Function GetUserUrl(User As IPluginContentProvider, Channel As Boolean) As String
        Function IsMyUser(UserURL As String) As ExchangeOptions
        Function IsMyImageVideo(URL As String) As ExchangeOptions
        Function GetSpecialData(URL As String, Path As String, AskForPath As Boolean) As IEnumerable
        Function GetInstance(What As Download) As IPluginContentProvider
        Function GetUserPostUrl(User As IPluginContentProvider, Media As IUserMedia) As String
#Region "XML Support"
        Sub Load(XMLValues As IEnumerable(Of KeyValuePair(Of String, String)))
#End Region
#Region "Initialization"
        Sub BeginInit()
        Sub EndInit()
        Sub BeginUpdate()
        Sub EndUpdate()
        Sub BeginEdit()
        Sub EndEdit()
#End Region
#Region "Site availability"
        Function Available(What As Download, Silent As Boolean) As Boolean
        Function ReadyToDownload(What As Download) As Boolean
#End Region
#Region "Downloading"
        Sub DownloadStarted(What As Download)
        Sub BeforeStartDownload(User As Object, What As Download)
        Sub AfterDownload(User As Object, What As Download)
        Sub DownloadDone(What As Download)
#End Region
        Sub Update()
        Sub Reset()
        Sub OpenSettingsForm()
        Sub UserOptions(ByRef Options As Object, OpenForm As Boolean)
    End Interface
End Namespace