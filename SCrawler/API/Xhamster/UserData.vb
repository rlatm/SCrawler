﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Threading
Imports SCrawler.API.Base
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Tools.Web.Clients
Imports PersonalUtilities.Tools.Web.Documents.JSON
Imports UTypes = SCrawler.API.Base.UserMedia.Types
Namespace API.Xhamster
    Friend Class UserData : Inherits UserDataBase
#Region "Declarations"
        Private ReadOnly Property MySettings As SiteSettings
            Get
                Return DirectCast(HOST.Source, SiteSettings)
            End Get
        End Property
        Private Structure ExchObj
            Friend IsPhoto As Boolean
        End Structure
        Private ReadOnly _TempPhotoData As List(Of UserMedia)
        Protected Overrides Sub LoadUserInformation_OptionalFields(ByRef Container As XmlFile, Loading As Boolean)
        End Sub
#End Region
#Region "Initializer"
        Friend Sub New()
            UseInternalM3U8Function = True
            _TempPhotoData = New List(Of UserMedia)
        End Sub
#End Region
#Region "Download base functions"
        Protected Overrides Sub DownloadDataF(Token As CancellationToken)
            _TempPhotoData.Clear()
            If DownloadVideos Then DownloadData(1, True, Token)
            If DownloadImages Then
                DownloadData(1, False, Token)
                ReparsePhoto(Token)
            End If
        End Sub
        Private Overloads Sub DownloadData(Page As Integer, IsVideo As Boolean, Token As CancellationToken)
            Dim URL$ = String.Empty
            Try
                Dim MaxPage% = -1
                Dim Type As UTypes = IIf(IsVideo, UTypes.VideoPre, UTypes.Picture)
                Dim mPages$ = IIf(IsVideo, "maxVideoPages", "maxPhotoPages")
                Dim listNode$()
                Dim m As UserMedia

                If IsSavedPosts Then
                    URL = $"https://xhamster.com/my/favorites/{IIf(IsVideo, "videos", "photos-and-galleries")}{IIf(Page = 1, String.Empty, $"/{Page}")}"
                    listNode = If(IsVideo, {"favoriteVideoListComponent", "models"}, {"favoritesGalleriesAndPhotosCollection"})
                Else
                    URL = $"https://xhamster.com/users/{Name}/{IIf(IsVideo, "videos", "photos")}{IIf(Page = 1, String.Empty, $"/{Page}")}"
                    listNode = {If(IsVideo, "userVideoCollection", "userGalleriesCollection")}
                End If
                ThrowAny(Token)

                Dim r$ = Responser.GetResponse(URL)
                If Not r.IsEmptyString Then r = RegexReplace(r, HtmlScript)
                If Not r.IsEmptyString Then
                    Using j As EContainer = JsonDocument.Parse(r).XmlIfNothing
                        If j.Count > 0 Then
                            If Not MySettings.DomainsUpdated AndAlso j.Contains("trustURLs") Then _
                               MySettings.UpdateDomains(j("trustURLs").Select(Function(d) d(0).XmlIfNothingValue), False)

                            MaxPage = j.Value(mPages).FromXML(Of Integer)(-1)

                            With j(listNode)
                                If .ListExists Then
                                    For Each e As EContainer In .Self
                                        m = ExtractMedia(e, Type)
                                        If Not m.URL.IsEmptyString Then
                                            If m.File.IsEmptyString Then Continue For

                                            If m.Post.Date.HasValue Then
                                                Select Case CheckDatesLimit(m.Post.Date.Value, Nothing)
                                                    Case DateResult.Skip : Continue For
                                                    Case DateResult.Exit : Exit Sub
                                                End Select
                                            End If

                                            If IsVideo AndAlso Not _TempPostsList.Contains(m.Post.ID) Then
                                                _TempPostsList.Add(m.Post.ID)
                                                _TempMediaList.ListAddValue(m, LNC)
                                            ElseIf Not IsVideo Then
                                                If DirectCast(m.Object, ExchObj).IsPhoto Then
                                                    If Not m.Post.ID.IsEmptyString AndAlso Not _TempPostsList.Contains(m.Post.ID) Then
                                                        _TempPostsList.Add(m.Post.ID)
                                                        _TempMediaList.ListAddValue(m, LNC)
                                                    End If
                                                Else
                                                    _TempPhotoData.ListAddValue(m, LNC)
                                                End If
                                            Else
                                                Exit Sub
                                            End If
                                        End If
                                    Next
                                End If
                            End With
                        End If
                    End Using
                End If

                If MaxPage > 0 AndAlso Page < MaxPage Then DownloadData(Page + 1, IsVideo, Token)
            Catch ex As Exception
                ProcessException(ex, Token, $"data downloading error [{URL}]")
            End Try
        End Sub
#End Region
#Region "Reparse video, photo"
        Protected Overrides Sub ReparseVideo(Token As CancellationToken)
            Dim URL$ = String.Empty
            Try
                If _TempMediaList.Count > 0 AndAlso _TempMediaList.Exists(Function(tm) tm.Type = UTypes.VideoPre) Then
                    Dim m As UserMedia, m2 As UserMedia
                    For i% = _TempMediaList.Count - 1 To 0 Step -1
                        If _TempMediaList(i).Type = UTypes.VideoPre Then
                            m = _TempMediaList(i)
                            If Not m.URL_BASE.IsEmptyString Then
                                m2 = Nothing
                                If GetM3U8(m2, m.URL_BASE, Responser) Then
                                    m2.URL_BASE = m.URL_BASE
                                    _TempMediaList(i) = m2
                                Else
                                    m.State = UserMedia.States.Missing
                                    _TempMediaList(i) = m
                                End If
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                ProcessException(ex, Token, "video reparsing error", False)
            End Try
        End Sub
        Private Overloads Sub ReparsePhoto(Token As CancellationToken)
            If _TempPhotoData.Count > 0 Then
                For i% = 0 To _TempPhotoData.Count - 1 : ReparsePhoto(i, 1, Token) : Next
                _TempPhotoData.Clear()
            End If
        End Sub
        Private Overloads Sub ReparsePhoto(Index As Integer, Page As Integer, Token As CancellationToken)
            Dim URL$ = String.Empty
            Try
                Dim MaxPage% = -1
                Dim m As UserMedia
                Dim sm As UserMedia = _TempPhotoData(Index)

                URL = $"{sm.URL}{IIf(Page = 1, String.Empty, $"/{Page}")}"
                ThrowAny(Token)
                Dim r$ = Responser.GetResponse(URL)
                If Not r.IsEmptyString Then r = RegexReplace(r, HtmlScript)
                If Not r.IsEmptyString Then
                    Using j As EContainer = JsonDocument.Parse(r).XmlIfNothing
                        If j.Count > 0 Then
                            MaxPage = j.Value({"pagination"}, "maxPage").FromXML(Of Integer)(-1)
                            With j({"photosGalleryModel"}, "photos")
                                If .ListExists Then
                                    For Each e In .Self
                                        m = ExtractMedia(e, UTypes.Picture, "imageURL", False, sm.Post.Date)
                                        m.URL_BASE = sm.URL
                                        If Not m.URL.IsEmptyString Then
                                            m.Post.ID = $"{sm.Post.ID}_{m.Post.ID}"
                                            m.SpecialFolder = sm.SpecialFolder
                                            If Not _TempPostsList.Contains(m.Post.ID) Then
                                                _TempPostsList.Add(m.Post.ID)
                                                _TempMediaList.ListAddValue(m, LNC)
                                            Else
                                                Exit Sub
                                            End If
                                        End If
                                    Next
                                End If
                            End With
                        End If
                    End Using
                End If

                If MaxPage > 0 AndAlso Page < MaxPage Then ReparsePhoto(Index, Page + 1, Token)
            Catch ex As Exception
                ProcessException(ex, Token, "photo reparsing error", False)
            End Try
        End Sub
#End Region
#Region "Reparse missing"
        Protected Overrides Sub ReparseMissing(Token As CancellationToken)
            Dim rList As New List(Of Integer)
            Try
                If ContentMissingExists Then
                    Dim m As UserMedia, m2 As UserMedia
                    For i% = 0 To _ContentList.Count - 1
                        m = _ContentList(i)
                        If m.State = UserMedia.States.Missing AndAlso Not m.URL_BASE.IsEmptyString Then
                            ThrowAny(Token)
                            m2 = Nothing
                            If GetM3U8(m2, m.URL_BASE, Responser) Then
                                m2.URL_BASE = m.URL_BASE
                                _TempMediaList.ListAddValue(m2, LNC)
                                rList.Add(i)
                            End If
                        End If
                    Next
                End If
            Catch ex As Exception
                ProcessException(ex, Token, "missing data downloading error")
            Finally
                If rList.Count > 0 Then
                    For i% = rList.Count - 1 To 0 Step -1 : _ContentList.RemoveAt(rList(i)) : Next
                    rList.Clear()
                End If
            End Try
        End Sub
#End Region
#Region "GetM3U8"
        Private Overloads Function GetM3U8(ByRef m As UserMedia, URL As String, Responser As Responser,
                                           Optional e As ErrorsDescriber = Nothing) As Boolean
            Try
                If Not URL.IsEmptyString Then
                    Dim r$ = Responser.GetResponse(URL)
                    If Not r.IsEmptyString Then r = RegexReplace(r, HtmlScript)
                    If Not r.IsEmptyString Then
                        Using j As EContainer = JsonDocument.Parse(r)
                            If j.ListExists Then
                                m = ExtractMedia(j("videoModel"), UTypes.VideoPre)
                                m.URL_BASE = URL
                                Return GetM3U8(m, j)
                            End If
                        End Using
                    End If
                End If
                Return False
            Catch ex As Exception
                If Not e.Exists Then e = EDP.ReturnValue
                Return ErrorsDescriber.Execute(e, ex, $"[{ToStringForLog()}]: API.Xhamster.GetM3U8({URL})", False)
            End Try
        End Function
        Private Overloads Function GetM3U8(ByRef m As UserMedia, j As EContainer) As Boolean
            Dim url$ = j.Value({"xplayerSettings", "sources", "hls"}, "url")
            If Not url.IsEmptyString Then m.URL = url : m.Type = UTypes.m3u8 : Return True
            Return False
        End Function
#End Region
#Region "Standalone downloader"
        Friend Shared Function GetVideoInfo(URL As String, Responser As Responser, Path As SFile) As UserMedia
            Try
                Using u As New UserData With {.Responser = Responser, .HOST = Settings(XhamsterSiteKey)}
                    Dim m As UserMedia = Nothing
                    If u.GetM3U8(m, URL, Responser, EDP.ThrowException) Then
                        m.File.Path = Path.Path
                        Dim f As SFile = u.DownloadM3U8(m.URL, m, m.File)
                        If Not f.IsEmptyString Then
                            m.File = f
                            m.State = UserMedia.States.Downloaded
                            Return m
                        End If
                    End If
                End Using
                Return Nothing
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.SendInLog + EDP.ReturnValue, ex, $"XHamster standalone download error: [{URL}]", New UserMedia)
            End Try
        End Function
#End Region
#Region "Download data"
        Protected Overrides Sub DownloadContent(Token As CancellationToken)
            DownloadContentDefault(Token)
        End Sub
        Protected Overloads Overrides Function DownloadM3U8(URL As String, Media As UserMedia, DestinationFile As SFile) As SFile
            Media.File = DestinationFile
            Return M3U8.Download(Media, Responser, MySettings.DownloadUHD.Value)
        End Function
#End Region
#Region "Create media"
        Private Shared Function ExtractMedia(j As EContainer, t As UTypes, Optional UrlNode As String = "pageURL",
                                             Optional DetectGalery As Boolean = True, Optional PostDate As Date? = Nothing) As UserMedia
            If j IsNot Nothing Then
                Dim m As New UserMedia(j.Value(UrlNode).Replace("\", String.Empty), t) With {
                    .Post = New UserPost With {
                        .ID = j.Value("id"),
                        .Date = AConvert(Of Date)(j.Value("created"), DateProvider, Nothing)
                    },
                    .PictureOption = j.Value("title").StringRemoveWinForbiddenSymbols,
                    .Object = New ExchObj
                }
                If PostDate.HasValue Then m.Post.Date = PostDate
                Dim setSpecialFolder As Boolean = False
                Dim processFile As Boolean = True
                Dim ext$ = "mp4"
                If t = UTypes.Picture Then
                    ext = "jpg"
                    If (Not DetectGalery OrElse j.Contains("galleryId")) AndAlso Not j.Value("imageURL").IsEmptyString Then
                        m.Object = New ExchObj With {.IsPhoto = True}
                        m.URL = j.Value("imageURL")
                        m.URL_BASE = m.URL
                        If DetectGalery Then m.Post.ID = $"{j.Value("galleryId")}_{m.Post.ID}"
                        m.File = m.URL
                        m.File.Separator = "\"
                        processFile = m.File.File.IsEmptyString
                    Else
                        setSpecialFolder = True
                    End If
                End If
                If Not m.URL.IsEmptyString Then
                    If m.Post.ID.IsEmptyString Then m.Post.ID = m.URL.Split("/").LastOrDefault
                    If m.PictureOption.IsEmptyString Then m.PictureOption = j.Value("titleLocalized").StringRemoveWinForbiddenSymbols
                    If m.PictureOption.IsEmptyString Then m.PictureOption = m.Post.ID
                    If setSpecialFolder Then m.SpecialFolder = m.PictureOption

                    If processFile Then
                        If Not m.PictureOption.IsEmptyString Then
                            m.File = $"{m.PictureOption}.{ext}"
                        ElseIf Not m.Post.ID.IsEmptyString Then
                            m.File = $"{m.Post.ID}.{ext}"
                        End If
                    End If
                    m.File.Separator = "\"
                End If
                Return m
            Else
                Return Nothing
            End If
        End Function
#End Region
#Region "Exception"
        Protected Overrides Function DownloadingException(ex As Exception, Message As String, Optional FromPE As Boolean = False,
                                                          Optional EObj As Object = Nothing) As Integer
            Return If(Responser.Status = Net.WebExceptionStatus.ConnectionClosed, 1, 0)
        End Function
#End Region
#Region "Idisposable support"
        Protected Overrides Sub Dispose(disposing As Boolean)
            If Not disposedValue And disposing Then _TempPhotoData.Clear()
            MyBase.Dispose(disposing)
        End Sub
#End Region
    End Class
End Namespace