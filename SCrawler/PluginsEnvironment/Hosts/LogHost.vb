' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Namespace Plugin.Hosts
    Friend Class LogHost : Implements ILogProvider
        Friend Sub Add(Message As String) Implements ILogProvider.Add
            MyMainLOG = Message
        End Sub
        Friend Sub Add(ex As Exception, Message As String,
                       Optional ShowMainMsg As Boolean = False, Optional ShowErrorMsg As Boolean = False,
                       Optional SendInLog As Boolean = True) Implements ILogProvider.Add
            ErrorsDescriber.Execute(New ErrorsDescriber(ShowMainMsg, ShowErrorMsg, SendInLog), ex, Message)
        End Sub
    End Class
End Namespace