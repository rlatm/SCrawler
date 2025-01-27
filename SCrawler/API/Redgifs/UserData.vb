﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Net
Imports System.Threading
Imports SCrawler.API.Base
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Tools.Web.Clients
Imports PersonalUtilities.Tools.Web.Documents.JSON
Imports UTypes = SCrawler.API.Base.UserMedia.Types
Imports UStates = SCrawler.API.Base.UserMedia.States
Namespace API.RedGifs
    Friend Class UserData : Inherits UserDataBase
        Friend Const DataGone As HttpStatusCode = HttpStatusCode.Gone
        Private Const PostDataUrl As String = "https://api.redgifs.com/v2/gifs/{0}?views=yes&users=yes"
#Region "Base declarations"
        Private ReadOnly Property MySettings As SiteSettings
            Get
                Return DirectCast(HOST.Source, SiteSettings)
            End Get
        End Property
        Protected Overrides Sub LoadUserInformation_OptionalFields(ByRef Container As XmlFile, Loading As Boolean)
        End Sub
#End Region
#Region "Initializer"
        Friend Sub New()
            UseResponserClient = True
        End Sub
#End Region
#Region "Download functions"
        Protected Overrides Sub DownloadDataF(Token As CancellationToken)
            DownloadData(1, Token)
        End Sub
        Private Overloads Sub DownloadData(Page As Integer, Token As CancellationToken)
            Dim URL$ = String.Empty
            Try
                Dim _page As Func(Of String) = Function() If(Page = 1, String.Empty, $"&page={Page}")
                URL = $"https://api.redgifs.com/v2/users/{Name}/search?order=recent{_page.Invoke}"
                Dim r$ = Responser.GetResponse(URL,, EDP.ThrowException)
                Dim postDate$, postID$
                Dim pTotal% = 0
                If Not r.IsEmptyString Then
                    Using j As EContainer = JsonDocument.Parse(r).XmlIfNothing
                        If j.Contains("gifs") Then
                            pTotal = j.Value("pages").FromXML(Of Integer)(0)
                            For Each g As EContainer In j("gifs")
                                postDate = g.Value("createDate")
                                Select Case CheckDatesLimit(postDate, DateProvider)
                                    Case DateResult.Skip : Continue For
                                    Case DateResult.Exit : Exit Sub
                                End Select
                                postID = g.Value("id")
                                If Not _TempPostsList.Contains(postID) Then _TempPostsList.Add(postID) Else Exit Sub
                                ObtainMedia(g, postID, postDate)
                            Next
                        End If
                    End Using
                End If
                If pTotal > 0 And Page < pTotal Then DownloadData(Page + 1, Token)
            Catch ex As Exception
                ProcessException(ex, Token, $"data downloading error [{URL}]")
            End Try
        End Sub
#End Region
#Region "Media obtain, extract"
        Private Sub ObtainMedia(j As EContainer, PostID As String,
                                Optional PostDateStr As String = Nothing, Optional PostDateDate As Date? = Nothing,
                                Optional State As UStates = UStates.Unknown)
            Dim tMedia As UserMedia = ExtractMedia(j)
            If Not tMedia.Type = UTypes.Undefined Then _
               _TempMediaList.ListAddValue(MediaFromData(tMedia.Type, tMedia.URL, PostID, PostDateStr, PostDateDate, State))
        End Sub
        Private Shared Function ExtractMedia(j As EContainer) As UserMedia
            If j IsNot Nothing Then
                With j("urls")
                    If .ListExists Then
                        Dim u$ = If(.Item("hd"), .Item("sd")).XmlIfNothingValue
                        If Not u.IsEmptyString Then
                            Dim ut As UTypes = UTypes.Undefined
                            'Type 1: video
                            'Type 2: image
                            Select Case j.Value("type").FromXML(Of Integer)(0)
                                Case 1 : ut = UTypes.Video
                                Case 2 : ut = UTypes.Picture
                            End Select
                            Return New UserMedia(u, ut)
                        End If
                    End If
                End With
            End If
            Return Nothing
        End Function
#End Region
#Region "ReparseMissing"
        Protected Overrides Sub ReparseMissing(Token As CancellationToken)
            Dim rList As New List(Of Integer)
            Try
                If _ContentList.Exists(MissingFinder) Then
                    Dim url$, r$
                    Dim u As UserMedia
                    Dim j As EContainer
                    For i% = 0 To _ContentList.Count - 1
                        If _ContentList(i).State = UserMedia.States.Missing Then
                            ThrowAny(Token)
                            u = _ContentList(i)
                            If Not u.Post.ID.IsEmptyString Then
                                url = String.Format(PostDataUrl, u.Post.ID.ToLower)
                                Try
                                    r = Responser.GetResponse(url,, EDP.ThrowException)
                                    If Not r.IsEmptyString Then
                                        j = JsonDocument.Parse(r)
                                        If j IsNot Nothing Then
                                            If If(j("gif")?.Count, 0) > 0 Then
                                                ObtainMedia(j("gif"), u.Post.ID,, u.Post.Date, UStates.Missing)
                                                rList.Add(i)
                                            End If
                                        End If
                                    End If
                                Catch down_ex As Exception
                                    u.Attempts += 1
                                    _ContentList(i) = u
                                End Try
                            Else
                                rList.Add(i)
                            End If
                        End If
                    Next
                End If
            Catch dex As ObjectDisposedException When Disposed
            Catch ex As Exception
                ProcessException(ex, Token, $"missing data downloading error",, False)
            Finally
                If Not Disposed And rList.Count > 0 Then
                    For i% = rList.Count - 1 To 0 Step -1 : _ContentList.RemoveAt(rList(i)) : Next
                End If
            End Try
        End Sub
#End Region
#Region "Downloader"
        Protected Overrides Sub DownloadContent(Token As CancellationToken)
            DownloadContentDefault(Token)
        End Sub
#End Region
#Region "Get post data statics"
        ''' <summary>
        ''' https://thumbs4.redgifs.com/abcde-large.jpg?expires -> abcde<br/>
        ''' https://thumbs4.redgifs.com/abcde.mp4?expires -> abcde<br/>
        ''' https://www.redgifs.com/watch/abcde?rel=a -> abcde
        ''' </summary>
        Friend Shared Function GetVideoIdFromUrl(URL As String) As String
            If Not URL.IsEmptyString Then
                Return RegexReplace(URL, If(URL.Contains("/watch/"), WatchIDRegex, ThumbsIDRegex))
            Else
                Return String.Empty
            End If
        End Function
        Friend Shared Function GetDataFromUrlId(Obj As String, ObjIsID As Boolean, Responser As Responser,
Host As Plugin.Hosts.SettingsHost) As UserMedia
            Dim URL$ = String.Empty
            Try
                If Obj.IsEmptyString Then Return Nothing
                If Not ObjIsID Then
                    Obj = GetVideoIdFromUrl(Obj)
                    If Not Obj.IsEmptyString Then Return GetDataFromUrlId(Obj, True, Responser, Host)
                Else
                    If Host Is Nothing Then Host = Settings(RedGifsSiteKey)
                    If Host.Source.Available(Plugin.ISiteSettings.Download.Main, True) Then
                        If Responser Is Nothing Then Responser = Host.Responser.Copy
                        URL = String.Format(PostDataUrl, Obj.ToLower)
                        Dim r$ = Responser.GetResponse(URL,, EDP.ThrowException)
                        If Not r.IsEmptyString Then
                            Using j As EContainer = JsonDocument.Parse(r)
                                If j IsNot Nothing Then
                                    Dim tm As UserMedia = ExtractMedia(j("gif"))
                                    tm.Post.ID = Obj
                                    tm.File = CStr(RegexReplace(tm.URL, FilesPattern))
                                    If tm.File.IsEmptyString Then
                                        tm.File.Name = Obj
                                        Select Case tm.Type
                                            Case UTypes.Picture : tm.File.Extension = "jpg"
                                            Case UTypes.Video : tm.File.Extension = "mp4"
                                        End Select
                                    End If
                                    Return tm
                                End If
                            End Using
                        End If
                    Else
                        Return New UserMedia With {.State = UStates.Missing}
                    End If
                End If
                Return Nothing
            Catch ex As Exception
                If Responser IsNot Nothing AndAlso (Responser.Client.StatusCode = DataGone Or Responser.Client.StatusCode = HttpStatusCode.NotFound) Then
                    Return New UserMedia With {.State = DataGone}
                Else
                    Dim m As New UserMedia With {.State = UStates.Missing}
                    Dim _errText$ = "API.RedGifs.UserData.GetDataFromUrlId({0})"
                    If Responser.Client.StatusCode = HttpStatusCode.Unauthorized Then
                        _errText = $"RedGifs credentials have expired [{CInt(Responser.Client.StatusCode)}]: {_errText}"
                        MyMainLOG = String.Format(_errText, URL)
                        Return m
                    Else
                        Return ErrorsDescriber.Execute(EDP.SendInLog, ex, String.Format(_errText, URL), m)
                    End If
                End If
            End Try
        End Function
#End Region
#Region "Create media"
        Private Shared Function MediaFromData(t As UTypes, _URL As String, PostID As String,
PostDateStr As String, PostDateDate As Date?, State As UStates) As UserMedia
            _URL = LinkFormatterSecure(RegexReplace(_URL.Replace("\", String.Empty), LinkPattern))
            Dim m As New UserMedia(_URL, t) With {.Post = New UserPost With {.ID = PostID}}
            If Not m.URL.IsEmptyString Then m.File = CStr(RegexReplace(m.URL, FilesPattern))
            If Not PostDateStr.IsEmptyString Then
                m.Post.Date = AConvert(Of Date)(PostDateStr, DateProvider, Nothing)
            ElseIf PostDateDate.HasValue Then
                m.Post.Date = PostDateDate
            Else
                m.Post.Date = Nothing
            End If
            m.State = State
            Return m
        End Function
#End Region
#Region "Exception"
        Protected Overrides Function DownloadingException(ex As Exception, Message As String, Optional FromPE As Boolean = False,
                                                          Optional EObj As Object = Nothing) As Integer
            Dim s As WebExceptionStatus = Responser.Client.Status
            Dim sc As HttpStatusCode = Responser.Client.StatusCode
            If sc = HttpStatusCode.NotFound Or s = DataGone Then
                UserExists = False
            ElseIf sc = HttpStatusCode.Unauthorized Then
                MyMainLOG = $"RedGifs credentials have expired [{CInt(sc)}]: {ToStringForLog()}"
            Else
                If Not FromPE Then LogError(ex, Message) : HasError = True
                Return 0
            End If
            Return 1
        End Function
#End Region
    End Class
End Namespace