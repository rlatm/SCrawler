﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports SCrawler.API.Base
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Functions.UniversalFunctions

Namespace API.LPSG
    Friend Module Declarations
        Private Const FileUrlRegexDefault As String = "([^/]+?)(jpg|jpeg|gif|png|webm)"
        Private ReadOnly InputForbidRemover As Func(Of String, String) = Function(Input) If(Input.IsEmptyString, Input, Input.StringRemoveWinForbiddenSymbols(, InputFReplacer))
        Private ReadOnly InputFReplacer As New ErrorsDescriber(EDP.ReturnValue)
        Friend ReadOnly Property FileExistsRegEx As RParams = RParams.DMS(FileUrlRegexDefault, 2)

        Friend ReadOnly Property FileRegEx As New RParams(FileUrlRegexDefault, Nothing, 0) With {
            .Converter = Function(Input As String) As String
                             Input = InputForbidRemover.Invoke(Input)
                             If Not Input.IsEmptyString Then
                                 Dim lv$ = Input.Split("-").LastOrDefault
                                 If Not lv.IsEmptyString Then
                                     Input = Input.Replace($"-{lv}", String.Empty)
                                     Input &= $".{lv}"
                                 End If
                             End If
                             Return Input
                         End Function}

        Friend ReadOnly Property FileRegExExt As New RParams(FileUrlRegexDefault, 0, Nothing, InputForbidRemover)
        Friend ReadOnly Property FileRegExExt2 As New RParams("([^/]+?)(?=(\Z|&))", 0, Nothing, InputForbidRemover)
        Friend ReadOnly Property NextPageRegex As RParams = RParams.DMS("<link rel=""next"" href=""(.+?/page-(\d+))""", 2)
        Friend ReadOnly Property PhotoRegEx As RParams = RParams.DM("(https://www.lpsg.com/attachments)(.+?)(?="")", 0, RegexReturn.List)

        Friend ReadOnly Property PhotoRegExExt As New RParams("img.data.src=""(/proxy[^""]+?)""", Nothing, 1, RegexReturn.List) With {
            .Converter = Function(Input) $"https://www.lpsg.com/{SymbolsConverter.HTML.Decode(Input)}"}

        Friend ReadOnly Property TempListAddParams As New ListAddParams(LAP.NotContainsOnly) With {.Comparer = New FComparer(Of UserMedia)(Function(x, y) x.URL = y.URL)}
    End Module
End Namespace
