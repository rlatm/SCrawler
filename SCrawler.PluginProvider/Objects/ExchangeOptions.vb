' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Namespace Plugin
    Public Structure ExchangeOptions
        Public UserName As String
        Public SiteName As String
        Public HostKey As String
        Public IsChannel As Boolean
        Public Exists As Boolean
        Public Sub New(Site As String, Name As String)
            UserName = Name
            SiteName = Site
        End Sub
        Public Sub New(Site As String, Name As String, IsChannel As Boolean)
            Me.New(Site, Name)
            Me.IsChannel = IsChannel
        End Sub
    End Structure
End Namespace