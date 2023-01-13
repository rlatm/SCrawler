' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Text
Imports PersonalUtilities.Functions.ArgConverter
Imports PersonalUtilities.Functions.RegularExpressions.RegexFunctions
Imports PersonalUtilities.Functions.UniversalFunctions
Imports PersonalUtilities.Tools.Web.Clients

Namespace API.Base

    Namespace M3U8Declarations
        Friend Module M3U8Defaults
            Friend ReadOnly TsFilesRegEx As RParams = RParams.DM(".+?\.ts[^\r\n]*", 0, RegexReturn.List)
        End Module
    End Namespace

    Friend NotInheritable Class M3U8Base

        Private Sub New()
        End Sub

        Friend Shared Function CreateUrl(Appender As String, File As String) As String
            File = File.StringTrimStart("/")
            If File.StartsWith("http") Then
                Return File
            Else
                If File.StartsWith("hls/") And Appender.Contains("hls/") Then _
                   Appender = LinkFormatterSecure(Appender.Replace("https://", String.Empty).Split("/").First)
                Return $"{Appender.StringTrimEnd("/")}/{File}"
            End If
        End Function

        Friend Shared Function Download(URLs As List(Of String), DestinationFile As SFile, Optional Responser As Responser = Nothing) As SFile
            Dim CachePath As SFile = Nothing
            Try
                If URLs.ListExists Then
                    Dim ConcatFile As SFile = DestinationFile
                    If ConcatFile.Name.IsEmptyString Then ConcatFile.Name = "PlayListFile"
                    ConcatFile.Extension = "mp4"
                    CachePath = $"{DestinationFile.PathWithSeparator}_Cache\{SFile.GetDirectories($"{DestinationFile.PathWithSeparator}_Cache\",,, EDP.ReturnValue).ListIfNothing.Count + 1}\"
                    If CachePath.Exists(SFO.Path) Then
                        Dim p As New SFileNumbers(ConcatFile.Name,,, New ANumbers With {.Format = ANumbers.Formats.General})
                        ConcatFile = SFile.Indexed_IndexFile(ConcatFile,, p, EDP.ReturnValue)
                        Dim i%
                        Dim eFiles As New List(Of SFile)
                        Dim dFile As SFile = CachePath
                        dFile.Extension = "ts"
                        Using w As New DownloadObjects.WebClient2(Responser)
                            For i = 0 To URLs.Count - 1
                                dFile.Name = $"ConPart_{i}"
                                w.DownloadFile(URLs(i), dFile)
                                eFiles.Add(dFile)
                            Next
                        End Using

                        Dim inputArgument = New StringBuilder(eFiles(0).File)
                        For inputFileCounter = 1 To eFiles.Count - 1
                            inputArgument.Append($"|{eFiles(inputFileCounter).File}")
                        Next

                        Process.Start(
                            New ProcessStartInfo(
                                Settings.FfmpegFile,
                                $"-i ""concat:{inputArgument}"" -c copy ""{ConcatFile}"""
                             ) With {.WorkingDirectory = CachePath}).WaitForExit()

                        eFiles.Clear()
                        Return DestinationFile
                    End If
                End If
                Return Nothing
            Finally
                CachePath.Delete(SFO.Path, SFODelete.None, EDP.None)
            End Try
        End Function

    End Class

End Namespace
