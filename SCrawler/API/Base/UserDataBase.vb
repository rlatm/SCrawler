﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Forms.Toolbars
Imports PersonalUtilities.Tools
Imports PersonalUtilities.Tools.Web.Clients
Imports System.IO
Imports System.Net
Imports System.Threading
Imports SCrawler.Plugin
Imports SCrawler.Plugin.Hosts
Imports UStates = SCrawler.API.Base.UserMedia.States
Imports UTypes = SCrawler.API.Base.UserMedia.Types
Namespace API.Base
    Friend MustInherit Class UserDataBase : Implements IUserData, IPluginContentProvider, IThrower
        Friend Const UserFileAppender As String = "User"
#Region "Events"
        Private ReadOnly UserUpdatedEventHandlers As List(Of IUserData.UserUpdatedEventHandler)
        Friend Custom Event UserUpdated As IUserData.UserUpdatedEventHandler Implements IUserData.UserUpdated
            AddHandler(e As IUserData.UserUpdatedEventHandler)
                If Not UserUpdatedEventHandlers.Contains(e) Then UserUpdatedEventHandlers.Add(e)
            End AddHandler
            RemoveHandler(e As IUserData.UserUpdatedEventHandler)
                If UserUpdatedEventHandlers.Contains(e) Then UserUpdatedEventHandlers.Remove(e)
            End RemoveHandler
            RaiseEvent(User As IUserData)
                Try
                    If UserUpdatedEventHandlers.Count > 0 Then
                        For i% = 0 To UserUpdatedEventHandlers.Count - 1
                            Try : UserUpdatedEventHandlers(i).Invoke(User) : Catch : End Try
                        Next
                    End If
                Catch
                End Try
            End RaiseEvent
        End Event
        Protected Sub OnUserUpdated()
            RaiseEvent UserUpdated(Me)
        End Sub
        Friend Sub RemoveUpdateHandlers()
            UserUpdatedEventHandlers.Clear()
        End Sub
#End Region
#Region "Collection buttons"
        Private _CollectionButtonsExists As Boolean = False
        Private _CollectionButtonsColorsSet As Boolean = False
        Friend WithEvents BTT_CONTEXT_DOWN As ToolStripKeyMenuItem
        Friend WithEvents BTT_CONTEXT_EDIT As ToolStripMenuItem
        Friend WithEvents BTT_CONTEXT_DELETE As ToolStripMenuItem
        Friend WithEvents BTT_CONTEXT_OPEN_PATH As ToolStripMenuItem
        Friend WithEvents BTT_CONTEXT_OPEN_SITE As ToolStripMenuItem
        Friend Sub CreateButtons()
            Dim tn$ = $"[{Site}] - {Name}"
            Dim _tn$ = $"{Site}{Name}"
            Dim tnn As Func(Of String, String) = Function(Input) $"{Input}{_tn}"
            Dim i As Image = Nothing
            With HOST.Source
                If .Icon IsNot Nothing Then
                    i = .Icon.ToBitmap
                ElseIf .Image IsNot Nothing Then
                    i = .Image
                End If
            End With
            BTT_CONTEXT_DOWN = New ToolStripKeyMenuItem(tn, i) With {.Name = tnn("DOWN"), .Tag = Me}
            BTT_CONTEXT_EDIT = New ToolStripMenuItem(tn, i) With {.Name = tnn("EDIT"), .Tag = Me}
            BTT_CONTEXT_DELETE = New ToolStripMenuItem(tn, i) With {.Name = tnn("DELETE"), .Tag = Me}
            BTT_CONTEXT_OPEN_PATH = New ToolStripMenuItem(tn, i) With {.Name = tnn("PATH"), .Tag = Me}
            BTT_CONTEXT_OPEN_SITE = New ToolStripMenuItem(tn, i) With {.Name = tnn("SITE"), .Tag = Me}
            UpdateButtonsColor()
            _CollectionButtonsExists = True
            If _UserInformationLoaded Then _CollectionButtonsColorsSet = True
        End Sub
        Private Sub UpdateButtonsColor()
            Dim cb As Color = SystemColors.Control
            Dim cf As Color = SystemColors.ControlText
            If Not UserExists Then
                cb = MyColor.DeleteBack
                cf = MyColor.DeleteFore
            ElseIf UserSuspended Then
                cb = MyColor.EditBack
                cf = MyColor.EditFore
            End If
            For Each b As ToolStripMenuItem In {BTT_CONTEXT_DOWN, BTT_CONTEXT_EDIT, BTT_CONTEXT_DELETE, BTT_CONTEXT_OPEN_PATH, BTT_CONTEXT_OPEN_SITE}
                If b IsNot Nothing Then b.BackColor = cb : b.ForeColor = cf
            Next
            If _UserInformationLoaded Then _CollectionButtonsColorsSet = True
        End Sub
#End Region
#Region "XML Declarations"
        Private Const Name_Site As String = UserInfo.Name_Site
        Private Const Name_Plugin As String = UserInfo.Name_Plugin
        Private Const Name_IsChannel As String = UserInfo.Name_IsChannel
        Friend Const Name_UserName As String = "UserName"
        Private Const Name_Model_User As String = UserInfo.Name_Model_User
        Private Const Name_Model_Collection As String = UserInfo.Name_Model_Collection
        Private Const Name_Merged As String = UserInfo.Name_Merged
        Private Const Name_SpecialPath As String = UserInfo.Name_SpecialPath
        Private Const Name_SpecialCollectionPath As String = UserInfo.Name_SpecialCollectionPath

        Private Const Name_UserExists As String = "UserExists"
        Private Const Name_UserSuspended As String = "UserSuspended"
        Private Const Name_FriendlyName As String = "FriendlyName"
        Private Const Name_UserID As String = "UserID"
        Private Const Name_Description As String = "Description"
        Private Const Name_ParseUserMediaOnly As String = "ParseUserMediaOnly"
        Private Const Name_Temporary As String = "Temporary"
        Private Const Name_Favorite As String = "Favorite"
        Private Const Name_CreatedByChannel As String = "CreatedByChannel"

        Private Const Name_SeparateVideoFolder As String = "SeparateVideoFolder"
        Private Const Name_CollectionName As String = UserInfo.Name_Collection
        Friend Const Name_LabelsName As String = "Labels"

        Private Const Name_ReadyForDownload As String = "ReadyForDownload"
        Private Const Name_DownloadImages As String = "DownloadImages"
        Private Const Name_DownloadVideos As String = "DownloadVideos"

        Private Const Name_VideoCount As String = "VideoCount"
        Private Const Name_PicturesCount As String = "PicturesCount"
        Private Const Name_LastUpdated As String = "LastUpdated"

        Private Const Name_ScriptUse As String = "ScriptUse"
        Private Const Name_ScriptData As String = "ScriptData"

        <Obsolete("Use 'Name_Merged'", False)> Friend Const Name_DataMerging As String = "DataMerging"
#End Region
#Region "Declarations"
#Region "Host, Site, Progress, Self"
        Friend Property HOST As SettingsHost Implements IUserData.HOST
        Friend ReadOnly Property Site As String Implements IContentProvider.Site
            Get
                Return HOST.Name
            End Get
        End Property
        Friend Property Progress As MyProgress
        Friend ReadOnly Property Self As IUserData Implements IUserData.Self
            Get
                Return Me
            End Get
        End Property
#End Region
#Region "User name, ID, exist, suspend"
        Friend User As UserInfo
        Friend Property IsSavedPosts As Boolean Implements IPluginContentProvider.IsSavedPosts
        Friend Overridable Property UserExists As Boolean = True Implements IUserData.Exists, IPluginContentProvider.UserExists
        Friend Overridable Property UserSuspended As Boolean = False Implements IUserData.Suspended, IPluginContentProvider.UserSuspended
        Friend Overridable Property Name As String Implements IContentProvider.Name, IPluginContentProvider.Name
            Get
                Return User.Name
            End Get
            Set(NewName As String)
                User.Name = NewName
                User.UpdateUserFile()
                Settings.UpdateUsersList(User)
            End Set
        End Property
        Friend Overridable Property ID As String = String.Empty Implements IContentProvider.ID, IPluginContentProvider.ID
        Friend Overridable Property FriendlyName As String = String.Empty Implements IContentProvider.FriendlyName
        Friend ReadOnly Property UserModel As UsageModel Implements IUserData.UserModel
            Get
                Return User.UserModel
            End Get
        End Property
        Friend Overridable ReadOnly Property CollectionModel As UsageModel Implements IUserData.CollectionModel
            Get
                Return User.CollectionModel
            End Get
        End Property
        Friend Overridable ReadOnly Property IsVirtual As Boolean Implements IUserData.IsVirtual
            Get
                Return UserModel = UsageModel.Virtual
            End Get
        End Property
#End Region
#Region "Description"
        Friend Property UserDescription As String = String.Empty Implements IContentProvider.Description, IPluginContentProvider.UserDescription
        Protected _DescriptionEveryTime As Boolean = False
        Protected _DescriptionChecked As Boolean = False
        Protected Function UserDescriptionNeedToUpdate() As Boolean
            Return (UserDescription.IsEmptyString Or _DescriptionEveryTime) And Not _DescriptionChecked
        End Function
        Protected Sub UserDescriptionUpdate(Descr As String)
            If UserDescriptionNeedToUpdate() Then
                If UserDescription.IsEmptyString Then
                    UserDescription = Descr
                ElseIf Not UserDescription.Contains(Descr) Then
                    UserDescription &= $"{vbNewLine}----{vbNewLine}{Descr}"
                End If
                _DescriptionChecked = True
            End If
        End Sub
        Protected Sub UserDescriptionReset()
            _DescriptionChecked = False
            _DescriptionEveryTime = Settings.UpdateUserDescriptionEveryTime
        End Sub
#End Region
#Region "Favorite, Temporary"
        Protected _Favorite As Boolean = False
        Friend Overridable Property Favorite As Boolean Implements IContentProvider.Favorite
            Get
                Return _Favorite
            End Get
            Set(Fav As Boolean)
                _Favorite = Fav
                If _Favorite Then _Temporary = False
            End Set
        End Property
        Protected _Temporary As Boolean = False
        Friend Overridable Property Temporary As Boolean Implements IContentProvider.Temporary
            Get
                Return _Temporary
            End Get
            Set(Temp As Boolean)
                _Temporary = Temp
                If _Temporary Then _Favorite = False
            End Set
        End Property
#End Region
#Region "Channel"
        Friend Overridable ReadOnly Property IsChannel As Boolean Implements IUserData.IsChannel
            Get
                Return User.IsChannel
            End Get
        End Property
        Friend Property CreatedByChannel As Boolean = False
#End Region
#Region "Images"
        Friend Overridable Function GetUserPicture() As Image Implements IUserData.GetPicture
            If Settings.ViewModeIsPicture Then
                Return GetPicture(Of Image)()
            Else
                Return Nothing
            End If
        End Function
        Friend Function GetUserPictureToastAddress() As SFile
            Return GetPicture(Of SFile)(False, True)
        End Function
        Friend Overridable Sub SetPicture(f As SFile) Implements IUserData.SetPicture
            Try
                If f.Exists Then
                    Using p As New UserImage(f, User.File) : p.Save() : End Using
                End If
            Catch
            End Try
        End Sub
        Protected Function GetNullPicture(MaxHeigh As XML.Base.XMLValue(Of Integer)) As Bitmap
            Return New Bitmap(CInt(DivideWithZeroChecking(MaxHeigh.Value, 100) * 75), MaxHeigh.Value)
        End Function
        Friend Function GetPicture(Of T)(Optional ReturnNullImageOnNothing As Boolean = True, Optional GetToast As Boolean = False) As T
            Dim rsfile As Boolean = GetType(T) Is GetType(SFile)
            Dim f As SFile = Nothing
            Dim p As UserImage = Nothing
            Dim DelPath As Boolean = True
BlockPictureFolder:
            On Error GoTo BlockPictureScan
            f = SFile.GetPath($"{MyFile.PathWithSeparator}Pictures")
            If f.Exists(SFO.Path, False) Then
                Dim PicList As List(Of SFile) = SFile.GetFiles(f, $"{UserImage.ImagePrefix}*.jpg")
                If PicList.ListExists Then
                    PicList.Sort()
                    Dim l As SFile, s As SFile
                    l = PicList.Find(Function(ff) ff.Name.Contains(UserImage.ImagePostfix_Large))
                    If Not l.IsEmptyString Then PicList.Remove(l)
                    s = PicList.Find(Function(ff) ff.Name.Contains(UserImage.ImagePostfix_Small))
                    If Not s.IsEmptyString Then PicList.Remove(s)
                    If PicList.Count > 0 Then
                        p = New UserImage(PicList.First, l, s, MyFile)
                        GoTo BlockReturn
                    Else
                        f.Delete(SFO.Path, Settings.DeleteMode, EDP.None)
                        DelPath = False
                    End If
                End If
            End If
BlockPictureScan:
            On Error GoTo BlockDeletePictureFolder
            Dim NewPicFile As SFile = SFile.GetFiles(MyFile.CutPath, "*.jpg|*.jpeg|*.png",,
                                                     New ErrorsDescriber(EDP.ReturnValue) With {
                                                         .ReturnValue = New List(Of SFile),
                                                         .ReturnValueExists = True}).FirstOrDefault
            If NewPicFile.Exists Then
                p = New UserImage(NewPicFile, MyFile)
                p.Save()
                GoTo BlockReturn
            End If
BlockDeletePictureFolder:
            On Error GoTo BlockReturn
            If DelPath Then
                f = SFile.GetPath($"{MyFile.PathWithSeparator}Pictures")
                If f.Exists(SFO.Path, False) Then f.Delete(SFO.Path, Settings.DeleteMode)
            End If
BlockReturn:
            On Error GoTo BlockNullPicture
            If p IsNot Nothing Then
                Dim i As Image = Nothing
                Dim a As SFile = Nothing
                If rsfile Then
                    If GetToast Then
                        a = p.Large.Address
                    Else
                        a = p.Address
                    End If
                Else
                    Select Case Settings.ViewMode.Value
                        Case View.LargeIcon : i = p.Large.OriginalImage.Clone
                        Case View.SmallIcon : i = p.Small.OriginalImage.Clone
                    End Select
                End If
                p.Dispose()
                If rsfile Then Return CObj(a) Else Return CObj(i)
            End If
BlockNullPicture:
            If ReturnNullImageOnNothing Then
                Select Case Settings.ViewMode.Value
                    Case View.LargeIcon : Return CObj(GetNullPicture(Settings.MaxLargeImageHeight))
                    Case View.SmallIcon : Return CObj(GetNullPicture(Settings.MaxSmallImageHeight))
                End Select
            End If
            Return Nothing
        End Function
#End Region
#Region "Separate folder"
        Friend Property SeparateVideoFolder As Boolean?
        Protected ReadOnly Property SeparateVideoFolderF As Boolean
            Get
                Return If(SeparateVideoFolder, Settings.SeparateVideoFolder.Value)
            End Get
        End Property
#End Region
#Region "Collections"
        Protected _IsCollection As Boolean = False
        Protected Friend ReadOnly Property IsCollection As Boolean Implements IUserData.IsCollection
            Get
                Return _IsCollection
            End Get
        End Property
        Friend Overridable Property CollectionName As String Implements IUserData.CollectionName
            Get
                Return User.CollectionName
            End Get
            Set(NewCollection As String)
                ChangeCollectionName(NewCollection, True)
            End Set
        End Property
        Friend ReadOnly Property IncludedInCollection As Boolean Implements IUserData.IncludedInCollection
            Get
                Return User.IncludedInCollection
            End Get
        End Property
        Friend Overridable Sub ChangeCollectionName(NewName As String, UpdateSettings As Boolean)
            Dim u As UserInfo = User
            u.CollectionName = NewName
            u.UpdateUserFile()
            User = u
            If UpdateSettings Then Settings.UpdateUsersList(User)
        End Sub
        Friend Overridable ReadOnly Property Labels As List(Of String) Implements IUserData.Labels
#End Region
#Region "Downloading"
        Protected _DataLoaded As Boolean = False
        Protected _DataParsed As Boolean = False
        Friend Property ParseUserMediaOnly As Boolean = False Implements IUserData.ParseUserMediaOnly, IPluginContentProvider.ParseUserMediaOnly
        Friend Overridable Property ReadyForDownload As Boolean = True Implements IUserData.ReadyForDownload
        Friend Property DownloadImages As Boolean = True Implements IUserData.DownloadImages
        Friend Property DownloadVideos As Boolean = True Implements IUserData.DownloadVideos
        Friend Property DownloadMissingOnly As Boolean = False Implements IUserData.DownloadMissingOnly
#End Region
#Region "Content"
        Protected ReadOnly _ContentList As List(Of UserMedia)
        Protected ReadOnly _ContentNew As List(Of UserMedia)
        Friend ReadOnly Property LatestData As List(Of UserMedia)
        Protected ReadOnly MissingFinder As Predicate(Of UserMedia) = Function(c) c.State = UStates.Missing
        Friend ReadOnly Property ContentMissing As List(Of UserMedia)
            Get
                If _ContentList.Count > 0 Then
                    Return _ContentList.Where(Function(c) MissingFinder(c)).ListIfNothing
                Else
                    Return New List(Of UserMedia)
                End If
            End Get
        End Property
        Friend Overridable ReadOnly Property ContentMissingExists As Boolean
            Get
                Return _ContentList.Exists(MissingFinder)
            End Get
        End Property
        Friend Sub RemoveMedia(m As UserMedia, State As UStates?)
            Dim i% = If(State.HasValue, _ContentList.FindIndex(Function(mm) mm.State = State.Value And mm.Equals(m)), _ContentList.IndexOf(m))
            If i >= 0 Then _ContentList.RemoveAt(i)
        End Sub
        Protected ReadOnly _TempMediaList As List(Of UserMedia)
        Protected ReadOnly _TempPostsList As List(Of String)
        Friend Function GetLastImageAddress() As SFile
            If _ContentList.Count > 0 Then
                Return _ContentList.LastOrDefault(Function(c) c.Type = UTypes.Picture And Not c.File.IsEmptyString And Not c.File.Extension = "gif").File
            Else
                Return Nothing
            End If
        End Function
#End Region
#Region "Files"
        Friend Overridable Property MyFile As SFile Implements IUserData.File
            Get
                Return User.File
            End Get
            Set(f As SFile)
                User.File = f
                Settings.UpdateUsersList(User)
            End Set
        End Property
        Protected MyFileData As SFile
        Protected MyFilePosts As SFile
        Friend Overridable Property FileExists As Boolean = False Implements IUserData.FileExists
        Friend Overridable Property DataMerging As Boolean
            Get
                Return User.Merged
            End Get
            Set(IsMerged As Boolean)
                If Not User.Merged = IsMerged Then
                    User.Merged = IsMerged
                    User.UpdateUserFile()
                    Settings.UpdateUsersList(User)
                End If
            End Set
        End Property
#End Region
#Region "Information, counters, error, update date"
        Friend Overridable Property LastUpdated As Date?
        Friend Overridable Property HasError As Boolean = False Implements IUserData.HasError
        Private _DownloadedPicturesTotal As Integer = 0
        Private _DownloadedPicturesSession As Integer = 0
        Friend Property DownloadedPictures(Total As Boolean) As Integer Implements IUserData.DownloadedPictures
            Get
                Return IIf(Total, _DownloadedPicturesTotal, _DownloadedPicturesSession)
            End Get
            Set(NewValue As Integer)
                If Total Then
                    _DownloadedPicturesTotal = NewValue
                Else
                    _DownloadedPicturesSession = NewValue
                End If
            End Set
        End Property
        Private _DownloadedVideosTotal As Integer = 0
        Private _DownloadedVideosSession As Integer = 0
        Friend Property DownloadedVideos(Total As Boolean) As Integer Implements IUserData.DownloadedVideos
            Get
                Return IIf(Total, _DownloadedVideosTotal, _DownloadedVideosSession)
            End Get
            Set(NewValue As Integer)
                If Total Then
                    _DownloadedVideosTotal = NewValue
                Else
                    _DownloadedVideosSession = NewValue
                End If
            End Set
        End Property
        Friend Overridable ReadOnly Property DownloadedTotal(Optional Total As Boolean = True) As Integer Implements IUserData.DownloadedTotal
            Get
                Return DownloadedPictures(Total) + DownloadedVideos(Total)
            End Get
        End Property
        Friend ReadOnly Property DownloadedInformation As String Implements IUserData.DownloadedInformation
            Get
                Dim luv$ = String.Empty
                If LastUpdated.HasValue Then luv = $"{LastUpdated.Value.ToStringDate(ADateTime.Formats.BaseDateTime)}: "
                Return $"{luv}{Name} [{Site}]{IIf(HasError, " (with errors)", String.Empty)}: P - {DownloadedPictures(False)}; V - {DownloadedVideos(False)}" &
                       $" (P - {DownloadedPictures(True)}; V - {DownloadedVideos(True)})"
            End Get
        End Property
        Friend Overridable Function GetUserInformation() As String
            Dim OutStr$ = $"User: {Name} (site: {Site}"
            If IncludedInCollection Then
                OutStr &= $"; collection: {CollectionName}"
                If CollectionModel = UsageModel.Default And UserModel = UsageModel.Virtual Then OutStr &= "; virtual"
            End If
            OutStr &= ")"
            OutStr.StringAppendLine($"Labels: {Labels.ListToString}")
            OutStr.StringAppendLine($"Path: {MyFile.CutPath.Path}")
            OutStr.StringAppendLine($"Total downloaded ({DownloadedTotal(True).NumToString(ANumbers.Formats.Number, 3)}):")
            OutStr.StringAppendLine($"Pictures: {DownloadedPictures(True).NumToString(ANumbers.Formats.Number, 3)}")
            OutStr.StringAppendLine($"Videos: {DownloadedVideos(True).NumToString(ANumbers.Formats.Number, 3)}")
            If Not UserDescription.IsEmptyString Then
                OutStr.StringAppendLine(String.Empty)
                OutStr.StringAppendLine(UserDescription)
            End If
            OutStr.StringAppendLine(String.Empty)
            OutStr.StringAppendLine($"Last updated at: {AConvert(Of String)(LastUpdated, ADateTime.Formats.BaseDateTime, "not yet")}")
            If _DataParsed Then
                OutStr.StringAppendLine("Downloaded now:")
                OutStr.StringAppendLine($"Pictures: {DownloadedTotal(False).NumToString(ANumbers.Formats.Number, 3)}")
                OutStr.StringAppendLine($"Videos: {DownloadedVideos(False).NumToString(ANumbers.Formats.Number, 3)}")
            End If
            Return OutStr
        End Function
#End Region
#Region "Script"
        Friend Overridable Property ScriptUse As Boolean = False Implements IUserData.ScriptUse
        Friend Overridable Property ScriptData As String Implements IUserData.ScriptData
#End Region
#End Region
#Region "Plugins Support"
        Protected Event ProgressChanged As IPluginContentProvider.ProgressChangedEventHandler Implements IPluginContentProvider.ProgressChanged
        Protected Event TotalCountChanged As IPluginContentProvider.TotalCountChangedEventHandler Implements IPluginContentProvider.TotalCountChanged
        Private Property IPluginContentProvider_Settings As ISiteSettings Implements IPluginContentProvider.Settings
            Get
                Return HOST.Source
            End Get
            Set(s As ISiteSettings)
            End Set
        End Property
        Private Property IPluginContentProvider_Thrower As IThrower Implements IPluginContentProvider.Thrower
        Private Property IPluginContentProvider_LogProvider As ILogProvider Implements IPluginContentProvider.LogProvider
        Friend Property ExternalPlugin As IPluginContentProvider
        Private Property IPluginContentProvider_ExistingContentList As List(Of IUserMedia) Implements IPluginContentProvider.ExistingContentList
        Private Property IPluginContentProvider_TempPostsList As List(Of String) Implements IPluginContentProvider.TempPostsList
        Private Property IPluginContentProvider_TempMediaList As List(Of IUserMedia) Implements IPluginContentProvider.TempMediaList
        Private Property IPluginContentProvider_SeparateVideoFolder As Boolean Implements IPluginContentProvider.SeparateVideoFolder
        Private Property IPluginContentProvider_DataPath As String Implements IPluginContentProvider.DataPath
        Private Sub IPluginContentProvider_XmlFieldsSet(Fields As List(Of KeyValuePair(Of String, String))) Implements IPluginContentProvider.XmlFieldsSet
        End Sub
        Private Function IPluginContentProvider_XmlFieldsGet() As List(Of KeyValuePair(Of String, String)) Implements IPluginContentProvider.XmlFieldsGet
            Return Nothing
        End Function
        Private Sub IPluginContentProvider_GetMedia() Implements IPluginContentProvider.GetMedia
        End Sub
        Private Sub IPluginContentProvider_Download() Implements IPluginContentProvider.Download
        End Sub
        Friend Overridable Function ExchangeOptionsGet() As Object Implements IPluginContentProvider.ExchangeOptionsGet
            Return Nothing
        End Function
        Friend Overridable Sub ExchangeOptionsSet(Obj As Object) Implements IPluginContentProvider.ExchangeOptionsSet
        End Sub
        Private _ExternalCompatibilityToken As CancellationToken
#End Region
#Region "IIndexable Support"
        Friend Property Index As Integer = 0 Implements IIndexable.Index
        Private Function SetIndex(Obj As Object, _Index As Integer) As Object Implements IIndexable.SetIndex
            DirectCast(Obj, UserDataBase).Index = _Index
            Return Obj
        End Function
#End Region
#Region "LVI"
        Friend ReadOnly Property LVIKey As String Implements IUserData.Key
            Get
                If Not _IsCollection Then
                    Return $"{IIf(IsChannel, "C", String.Empty)}{Site.ToString.ToUpper}_{Name}"
                Else
                    Return $"CCCC_{CollectionName}"
                End If
            End Get
        End Property
        Friend Function GetLVI(Destination As ListView) As ListViewItem Implements IUserData.GetLVI
            If Settings.ViewModeIsPicture Then
                Return ListImagesLoader.ApplyLVIColor(Me, New ListViewItem(ToString(), LVIKey, GetLVIGroup(Destination)) With {.Name = LVIKey, .Tag = LVIKey}, True)
            Else
                Return ListImagesLoader.ApplyLVIColor(Me, New ListViewItem(ToString(), GetLVIGroup(Destination)) With {.Name = LVIKey, .Tag = LVIKey}, True)
            End If
        End Function
        Friend Overridable ReadOnly Property FitToAddParams As Boolean Implements IUserData.FitToAddParams
            Get
                With Settings
                    If LastUpdated.HasValue And Not .ViewDateMode.Value = ShowingDates.Off Then
                        Dim f As Date = If(.ViewDateFrom.HasValue, .ViewDateFrom.Value.Date, Date.MinValue.Date)
                        Dim t As Date = If(.ViewDateTo.HasValue, .ViewDateTo.Value.Date, Date.MaxValue.Date)
                        Select Case DirectCast(.ViewDateMode.Value, ShowingDates)
                            Case ShowingDates.In : If Not LastUpdated.Value.ValueBetween(f, t) Then Return False
                            Case ShowingDates.Not : If LastUpdated.Value.ValueBetween(f, t) Then Return False
                        End Select
                    End If
                    If Not .Labels.ExcludedIgnore AndAlso .Labels.Excluded.ValuesList.ListContains(Labels) Then Return False
                    If .SelectedSites.Count = 0 OrElse .SelectedSites.Contains(Site) Then
                        Select Case .ShowingMode.Value
                            Case ShowingModes.Regular : Return Not Temporary And Not Favorite
                            Case ShowingModes.Temporary : Return Temporary
                            Case ShowingModes.Favorite : Return Favorite
                            Case ShowingModes.Deleted : Return Not UserExists
                            Case ShowingModes.Suspended : Return UserSuspended
                            Case ShowingModes.Labels : Return Settings.Labels.Current.ValuesList.ListContains(Labels)
                            Case ShowingModes.NoLabels : Return Labels.Count = 0
                            Case Else : Return True
                        End Select
                    Else
                        Return False
                    End If
                End With
            End Get
        End Property
        Friend Function GetLVIGroup(Destination As ListView) As ListViewGroup Implements IUserData.GetLVIGroup
            Try
                If Settings.ShowingMode.Value = ShowingModes.Labels And Not Settings.ShowGroupsInsteadLabels Then
                    If Labels.Count > 0 And Settings.Labels.Current.Count > 0 Then
                        For i% = 0 To Labels.Count - 1
                            If Settings.Labels.Current.Contains(Labels(i)) Then Return Destination.Groups.Item(Labels(i))
                        Next
                    End If
                ElseIf Settings.ShowGroups Then
                    Return Destination.Groups.Item(GetLviGroupName(HOST, Temporary, Favorite, IsCollection, IsChannel))
                End If
                Return Destination.Groups.Item(LabelsKeeper.NoLabeledName)
            Catch ex As Exception
                Return Destination.Groups.Item(LabelsKeeper.NoLabeledName)
            End Try
        End Function
#End Region
#Region "Initializer"
        ''' <summary>By using this constructor you must set UserName and MyFile manually</summary>
        Friend Sub New(Optional InvokeImageHandler As Boolean = True)
            _ContentList = New List(Of UserMedia)
            _ContentNew = New List(Of UserMedia)
            LatestData = New List(Of UserMedia)
            _TempMediaList = New List(Of UserMedia)
            _TempPostsList = New List(Of String)
            Labels = New List(Of String)
            UserUpdatedEventHandlers = New List(Of IUserData.UserUpdatedEventHandler)
            If InvokeImageHandler Then MainFrameObj.ImageHandler(Me)
        End Sub
        Friend Sub SetEnvironment(ByRef h As SettingsHost, u As UserInfo, _LoadUserInformation As Boolean,
                                  Optional AttachUserInfo As Boolean = True) Implements IUserData.SetEnvironment
            HOST = h
            If AttachUserInfo Then
                User = u
                If _LoadUserInformation Then LoadUserInformation()
            End If
        End Sub
        ''' <exception cref="ArgumentOutOfRangeException"></exception>
        Friend Shared Function GetInstance(u As UserInfo, Optional _LoadUserInformation As Boolean = True) As IUserData
            If Not u.Plugin.IsEmptyString Then
                Return Settings(u.Plugin).GetInstance(u.DownloadOption, u, _LoadUserInformation)
            Else
                Throw New ArgumentOutOfRangeException("Plugin", $"Plugin [{u.Plugin}] information does not recognized by loader")
            End If
        End Function
        Friend Shared Function GetPostUrl(u As IUserData, PostData As UserMedia) As String
            Dim uName$ = String.Empty
            Try
                If u IsNot Nothing AndAlso Not u.IsCollection Then
                    With DirectCast(u, UserDataBase)
                        If Not .User.Plugin.IsEmptyString Then
                            uName = .User.Name
                            Return Settings(.User.Plugin).GetUserPostUrl(.Self, PostData)
                        End If
                    End With
                End If
                Return String.Empty
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.SendInLog, ex, $"GetPostUrl({uName}, {PostData.Post.ID})", String.Empty)
            End Try
        End Function
#End Region
#Region "Information & Content data files loader and saver"
#Region "User information"
        Private _UserInformationLoaded As Boolean = False
        Friend Overridable Sub LoadUserInformation() Implements IUserData.LoadUserInformation
            Try
                If MyFile.Exists Then
                    FileExists = True
                    Using x As New XmlFile(MyFile) With {.XmlReadOnly = True}
                        If User.Name.IsEmptyString Then User.Name = x.Value(Name_UserName)
                        UserExists = x.Value(Name_UserExists).FromXML(Of Boolean)(True)
                        UserSuspended = x.Value(Name_UserSuspended).FromXML(Of Boolean)(False)
                        ID = x.Value(Name_UserID)
                        FriendlyName = x.Value(Name_FriendlyName)
                        UserDescription = x.Value(Name_Description)
                        ParseUserMediaOnly = x.Value(Name_ParseUserMediaOnly).FromXML(Of Boolean)(False)
                        Temporary = x.Value(Name_Temporary).FromXML(Of Boolean)(False)
                        Favorite = x.Value(Name_Favorite).FromXML(Of Boolean)(False)
                        CreatedByChannel = x.Value(Name_CreatedByChannel).FromXML(Of Boolean)(False)
                        SeparateVideoFolder = AConvert(Of Boolean)(x.Value(Name_SeparateVideoFolder), AModes.Var, Nothing)
                        ReadyForDownload = x.Value(Name_ReadyForDownload).FromXML(Of Boolean)(True)
                        DownloadImages = x.Value(Name_DownloadImages).FromXML(Of Boolean)(True)
                        DownloadVideos = x.Value(Name_DownloadVideos).FromXML(Of Boolean)(True)
                        DownloadedVideos(True) = x.Value(Name_VideoCount).FromXML(Of Integer)(0)
                        DownloadedPictures(True) = x.Value(Name_PicturesCount).FromXML(Of Integer)(0)
                        LastUpdated = AConvert(Of Date)(x.Value(Name_LastUpdated), ADateTime.Formats.BaseDateTime, Nothing)
                        ScriptUse = x.Value(Name_ScriptUse).FromXML(Of Boolean)(False)
                        ScriptData = x.Value(Name_ScriptData)
                        'TODELETE: UserDataBase remove old 'merge' constant
#Disable Warning BC40000
                        If x.Contains(Name_DataMerging) Then
                            DataMerging = x.Value(Name_DataMerging).FromXML(Of Boolean)(False)
                        Else
                            DataMerging = x.Value(Name_Merged).FromXML(Of Boolean)(False)
                        End If
#Enable Warning
                        ChangeCollectionName(x.Value(Name_CollectionName), False)
                        Labels.ListAddList(x.Value(Name_LabelsName).StringToList(Of String, List(Of String))("|", EDP.ReturnValue), LAP.NotContainsOnly, LAP.ClearBeforeAdd)
                        LoadUserInformation_OptionalFields(x, True)
                    End Using
                    UpdateDataFiles()
                    _UserInformationLoaded = True
                    If _CollectionButtonsExists And Not _CollectionButtonsColorsSet And (Not UserExists Or UserSuspended) Then UpdateButtonsColor()
                End If
            Catch ex As Exception
                LogError(ex, "user information loading error")
            End Try
        End Sub
        Friend Overridable Sub UpdateUserInformation() Implements IUserData.UpdateUserInformation
            Try
                MyFile.Exists(SFO.Path)
                Using x As New XmlFile With {.Name = "User"}
                    x.Add(Name_Site, Site)
                    x.Add(Name_Plugin, HOST.Key)
                    x.Add(Name_UserName, User.Name)
                    x.Add(Name_IsChannel, IsChannel.BoolToInteger)
                    x.Add(Name_Model_User, CInt(UserModel))
                    x.Add(Name_Model_Collection, CInt(CollectionModel))
                    x.Add(Name_SpecialPath, User.SpecialPath)
                    x.Add(Name_SpecialCollectionPath, User.SpecialCollectionPath)
                    x.Add(Name_UserExists, UserExists.BoolToInteger)
                    x.Add(Name_UserSuspended, UserSuspended.BoolToInteger)
                    x.Add(Name_UserID, ID)
                    x.Add(Name_FriendlyName, FriendlyName)
                    x.Add(Name_Description, UserDescription)
                    x.Add(Name_ParseUserMediaOnly, ParseUserMediaOnly.BoolToInteger)
                    x.Add(Name_Temporary, Temporary.BoolToInteger)
                    x.Add(Name_Favorite, Favorite.BoolToInteger)
                    x.Add(Name_CreatedByChannel, CreatedByChannel.BoolToInteger)
                    If SeparateVideoFolder.HasValue Then
                        x.Add(Name_SeparateVideoFolder, SeparateVideoFolder.Value.BoolToInteger)
                    Else
                        x.Add(Name_SeparateVideoFolder, String.Empty)
                    End If
                    x.Add(Name_ReadyForDownload, ReadyForDownload.BoolToInteger)
                    x.Add(Name_DownloadImages, DownloadImages.BoolToInteger)
                    x.Add(Name_DownloadVideos, DownloadVideos.BoolToInteger)
                    x.Add(Name_VideoCount, DownloadedVideos(True))
                    x.Add(Name_PicturesCount, DownloadedPictures(True))
                    x.Add(Name_LastUpdated, AConvert(Of String)(LastUpdated, ADateTime.Formats.BaseDateTime, String.Empty))
                    x.Add(Name_ScriptUse, ScriptUse.BoolToInteger)
                    x.Add(Name_ScriptData, ScriptData)
                    x.Add(Name_CollectionName, CollectionName)
                    x.Add(Name_LabelsName, Labels.ListToString("|", EDP.ReturnValue))
                    x.Add(Name_Merged, DataMerging.BoolToInteger)

                    LoadUserInformation_OptionalFields(x, False)

                    x.Save(MyFile)
                End Using
                If Not IsSavedPosts Then Settings.UpdateUsersList(User)
            Catch ex As Exception
                LogError(ex, "user information saving error")
            End Try
        End Sub
        ''' <param name="Loading"><see langword="True"/>: Loading; <see langword="False"/>: Saving</param>
        Protected MustOverride Sub LoadUserInformation_OptionalFields(ByRef Container As XmlFile, Loading As Boolean)
#End Region
#Region "User data"
        Friend Overridable Overloads Sub LoadContentInformation(Optional Force As Boolean = False)
            Try
                UpdateDataFiles()
                If Not MyFileData.Exists Or (_DataLoaded And Not Force) Then Exit Sub
                Using x As New XmlFile(MyFileData, Protector.Modes.All, False) With {.XmlReadOnly = True, .AllowSameNames = True}
                    x.LoadData()
                    If x.Count > 0 Then
                        For Each v As EContainer In x : _ContentList.Add(New UserMedia(v, Me)) : Next
                    End If
                    _DataLoaded = True
                End Using
            Catch ex As Exception
                LogError(ex, "history loading error")
            End Try
        End Sub
        Friend Sub UpdateContentInformation()
            Try
                UpdateDataFiles()
                If MyFileData.IsEmptyString Then Exit Sub
                MyFileData.Exists(SFO.Path)
                Using x As New XmlFile With {.AllowSameNames = True, .Name = "Data"}
                    If _ContentList.Count > 0 Then x.AddRange(_ContentList)
                    x.Save(MyFileData)
                End Using
            Catch ex As Exception
                LogError(ex, "history saving error")
            End Try
        End Sub
#End Region
#End Region
#Region "Open site, folder"
        Friend Overridable Sub OpenSite(Optional e As ErrorsDescriber = Nothing) Implements IContentProvider.OpenSite
            Try
                Dim URL$ = HOST.Source.GetUserUrl(Me, IsChannel)
                If Not URL.IsEmptyString Then Process.Start(URL)
            Catch ex As Exception
                If Not e.Exists Then e = New ErrorsDescriber(EDP.ShowAllMsg)
                MsgBoxE({$"Error when trying to open [{Site}] page of user [{Name}]", $"User [{ToString()}]"}, MsgBoxStyle.Critical, e, ex)
            End Try
        End Sub
        Friend Overridable Sub OpenFolder() Implements IUserData.OpenFolder
            GlobalOpenPath(MyFile.CutPath)
        End Sub
#End Region
#Region "Download limits"
        Protected Enum DateResult : [Continue] : [Skip] : [Exit] : End Enum
        Friend Overridable Property DownloadTopCount As Integer? = Nothing Implements IUserData.DownloadTopCount, IPluginContentProvider.PostsNumberLimit
        Friend Overridable Property IncludeInTheFeed As Boolean = True
        Private _DownloadDateFrom As Date? = Nothing
        Private _DownloadDateFromF As Date
        Friend Overridable Property DownloadDateFrom As Date? Implements IUserData.DownloadDateFrom, IPluginContentProvider.DownloadDateFrom
            Get
                Return _DownloadDateFrom
            End Get
            Set(d As Date?)
                _DownloadDateFrom = d
                If _DownloadDateFrom.HasValue Then _DownloadDateFromF = _DownloadDateFrom.Value.Date Else _DownloadDateFromF = Date.MinValue.Date
            End Set
        End Property
        Private _DownloadDateTo As Date? = Nothing
        Private _DownloadDateToF As Date
        Friend Overridable Property DownloadDateTo As Date? Implements IUserData.DownloadDateTo, IPluginContentProvider.DownloadDateTo
            Get
                Return _DownloadDateTo
            End Get
            Set(d As Date?)
                _DownloadDateTo = d
                If _DownloadDateTo.HasValue Then _DownloadDateToF = _DownloadDateTo.Value Else _DownloadDateToF = Date.MaxValue.Date
            End Set
        End Property
        Protected Function CheckDatesLimit(DateObj As Object, DateProvider As IFormatProvider) As DateResult
            Try
                If (DownloadDateFrom.HasValue Or DownloadDateTo.HasValue) AndAlso ACheck(DateObj) Then
                    Dim td As Date? = AConvert(Of Date)(DateObj, DateProvider, Nothing)
                    If td.HasValue Then
                        If td.Value.ValueBetween(_DownloadDateFromF, _DownloadDateToF) Then
                            Return DateResult.Continue
                        ElseIf td.Value > _DownloadDateToF Then
                            Return DateResult.Skip
                        Else
                            Return DateResult.Exit
                        End If
                    End If
                End If
                Return DateResult.Continue
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.SendInLog, ex, $"[UserDataBase.CheckDatesLimit({If(TypeOf DateObj Is String, CStr(DateObj), "?")})]", DateResult.Continue)
            End Try
        End Function
#End Region
#Region "Download functions and options"
        Protected Responser As Responser
        Protected UseResponserClient As Boolean = False
        Friend Overridable Sub DownloadData(Token As CancellationToken) Implements IContentProvider.DownloadData
            Dim Canceled As Boolean = False
            _ExternalCompatibilityToken = Token
            Try
                UpdateDataFiles()
                UserDescriptionReset()
                If Responser IsNot Nothing Then Responser.Dispose()
                Responser = New Responser
                If HOST.Responser IsNot Nothing Then Responser.Copy(HOST.Responser)
                'TODO: UserDataBase remove [Responser.DecodersError]
                Responser.DecodersError = New ErrorsDescriber(EDP.SendInLog + EDP.ReturnValue) With {
                    .DeclaredMessage = New MMessage($"SymbolsConverter error: [{ToStringForLog()}]", ToStringForLog())}

                Dim UpPic As Boolean = Settings.ViewModeIsPicture AndAlso GetPicture(Of Image)(False) Is Nothing
                Dim sEnvir() As Boolean = {UserExists, UserSuspended}
                Dim EnvirChanged As Func(Of Boolean) = Function() Not sEnvir(0) = UserExists Or Not sEnvir(1) = UserSuspended
                Dim _downContent As Func(Of UserMedia, Boolean) = Function(c) c.State = UStates.Downloaded
                UserExists = True
                UserSuspended = False
                DownloadedPictures(False) = 0
                DownloadedVideos(False) = 0
                _TempMediaList.Clear()
                _TempPostsList.Clear()
                LatestData.Clear()
                Dim __SaveData As Boolean = Not CreatedByChannel Or Not Settings.FromChannelDownloadTopUse

                LoadContentInformation()

                If MyFilePosts.Exists Then _TempPostsList.ListAddList(File.ReadAllLines(MyFilePosts))
                If _ContentList.Count > 0 Then _TempPostsList.ListAddList(_ContentList.Select(Function(u) u.Post.ID), LNC)

                If Not DownloadMissingOnly Then
                    ThrowAny(Token)
                    DownloadDataF(Token)
                    ThrowAny(Token)
                    If Settings.ReparseMissingInTheRoutine Then ReparseMissing(Token) : ThrowAny(Token)
                Else
                    ReparseMissing(Token)
                End If

                If _TempMediaList.Count > 0 Then
                    If Not DownloadImages Then _TempMediaList.RemoveAll(Function(m) m.Type = UTypes.GIF Or m.Type = UTypes.Picture)
                    If Not DownloadVideos Then _TempMediaList.RemoveAll(Function(m) m.Type = UTypes.Video Or
                                                                                    m.Type = UTypes.VideoPre Or m.Type = UTypes.m3u8)
                    If DownloadMissingOnly Then _TempMediaList.RemoveAll(Function(m) Not m.State = UStates.Missing)
                End If

                ReparseVideo(Token)
                ThrowAny(Token)
                If _TempPostsList.Count > 0 And Not DownloadMissingOnly And __SaveData Then _
                   TextSaver.SaveTextToFile(_TempPostsList.ListToString(Environment.NewLine), MyFilePosts, True,, EDP.None)
                _ContentNew.ListAddList(_TempMediaList, LAP.ClearBeforeAdd)
                DownloadContent(Token)
                ThrowIfDisposed()

                If IncludeInTheFeed Then LatestData.ListAddList(_ContentNew.Where(_downContent), LNC)
                Dim mcb& = If(ContentMissingExists, _ContentList.LongCount(Function(c) MissingFinder(c)), 0)
                _ContentList.ListAddList(_ContentNew.Where(Function(c) _downContent(c) Or MissingFinder(c)), LNC)
                Dim mca& = If(ContentMissingExists, _ContentList.LongCount(Function(c) MissingFinder(c)), 0)
                If DownloadedTotal(False) > 0 Or EnvirChanged.Invoke Or Not mcb = mca Then
                    If __SaveData Then
                        LastUpdated = Now
                        RunScript()
                        DownloadedPictures(True) = SFile.GetFiles(User.File.CutPath, "*.jpg|*.jpeg|*.png|*.gif|*.webm",, EDP.ReturnValue).Count
                        DownloadedVideos(True) = SFile.GetFiles(User.File.CutPath, "*.mp4|*.mkv|*.mov", SearchOption.AllDirectories, EDP.ReturnValue).Count
                        If Labels.Contains(LabelsKeeper.NoParsedUser) Then Labels.Remove(LabelsKeeper.NoParsedUser)
                        UpdateContentInformation()
                    Else
                        DownloadedVideos(False) = 0
                        DownloadedPictures(False) = 0
                        _ContentList.Clear()
                        CreatedByChannel = False
                    End If
                    If Not UserExists Then ReadyForDownload = False
                    UpdateUserInformation()
                    If _CollectionButtonsExists AndAlso EnvirChanged.Invoke Then UpdateButtonsColor()
                End If
                ThrowIfDisposed()
                If UpPic Or EnvirChanged.Invoke Then OnUserUpdated()
            Catch oex As OperationCanceledException When Token.IsCancellationRequested
                MyMainLOG = $"{ToStringForLog()}: downloading canceled"
                Canceled = True
            Catch dex As ObjectDisposedException When Disposed
                Canceled = True
            Catch ex As Exception
                LogError(ex, "downloading data error")
                HasError = True
            Finally
                If Responser IsNot Nothing Then Responser.Dispose() : Responser = Nothing
                If Not Canceled Then _DataParsed = True
                _ContentNew.Clear()
                DownloadTopCount = Nothing
                DownloadDateFrom = Nothing
                DownloadDateTo = Nothing
                DownloadMissingOnly = False
            End Try
        End Sub
        Protected Sub UpdateDataFiles()
            If Not User.File.IsEmptyString Then
                MyFileData = User.File
                MyFileData.Name &= "_Data"
                MyFilePosts = User.File
                MyFilePosts.Name &= "_Posts"
                MyFilePosts.Extension = "txt"
            Else
                Throw New ArgumentNullException("User.File", "User file not detected")
            End If
        End Sub
        Protected MustOverride Sub DownloadDataF(Token As CancellationToken)
        Protected Overridable Sub ReparseVideo(Token As CancellationToken)
        End Sub
        ''' <summary>
        ''' Missing posts must be collected from [<see cref="_ContentList"/>].<br/>
        ''' Reparsed post must be added to [<see cref="_TempMediaList"/>].<br/>
        ''' At the end of the function, reparsed posts must be removed from [<see cref="_ContentList"/>].
        ''' </summary>
        Protected Overridable Sub ReparseMissing(Token As CancellationToken)
        End Sub
        Protected MustOverride Sub DownloadContent(Token As CancellationToken)
        Private NotInheritable Class OptionalWebClient : Inherits DownloadObjects.WebClient2
            Friend Sub New(ByRef Source As UserDataBase)
                UseResponserClient = Source.UseResponserClient
                If UseResponserClient Then
                    RC = Source.Responser
                Else
                    WC = New WebClient
                End If
            End Sub
        End Class
        Protected Sub DownloadContentDefault(Token As CancellationToken)
            Try
                Dim i%
                Dim dCount% = 0, dTotal% = 0
                ThrowAny(Token)
                If _ContentNew.Count > 0 Then
                    _ContentNew.RemoveAll(Function(c) c.URL.IsEmptyString)
                    If _ContentNew.Count > 0 Then
                        MyFile.Exists(SFO.Path)
                        Dim MissingErrorsAdd As Boolean = Settings.AddMissingErrorsToLog
                        Dim MyDir$ = MyFile.CutPath.PathNoSeparator
                        Dim vsf As Boolean = SeparateVideoFolderF
                        Dim __isVideo As Boolean
                        Dim f As SFile
                        Dim v As UserMedia

                        Using w As New OptionalWebClient(Me)
                            If vsf Then CSFileP($"{MyDir}\Video\").Exists(SFO.Path)
                            Progress.Maximum += _ContentNew.Count
                            For i = 0 To _ContentNew.Count - 1
                                ThrowAny(Token)
                                v = _ContentNew(i)
                                v.State = UStates.Tried
                                If v.File.IsEmptyString Then
                                    f = v.URL
                                Else
                                    f = v.File
                                End If
                                f.Separator = "\"
                                f.Path = MyDir

                                If v.URL_BASE.IsEmptyString Then v.URL_BASE = v.URL

                                If Not v.File.IsEmptyString And Not v.URL.IsEmptyString Then
                                    Try
                                        __isVideo = v.Type = UTypes.Video Or f.Extension = "mp4"

                                        If f.Extension.IsEmptyString Then
                                            Select Case v.Type
                                                Case UTypes.Picture : f.Extension = "jpg"
                                                Case UTypes.Video : f.Extension = "mp4"
                                                Case UTypes.GIF : f.Extension = "gif"
                                            End Select
                                        ElseIf f.Extension = "webp" And Settings.DownloadNativeImageFormat Then
                                            f.Extension = "jpg"
                                        End If

                                        If Not v.SpecialFolder.IsEmptyString Then
                                            f.Path = $"{f.PathWithSeparator}{v.SpecialFolder}\".CSFileP.Path
                                            f.Exists(SFO.Path)
                                        End If
                                        If __isVideo And vsf Then
                                            f.Path = $"{f.PathWithSeparator}Video"
                                            If Not v.SpecialFolder.IsEmptyString Then f.Exists(SFO.Path)
                                        End If

                                        If v.Type = UTypes.m3u8 And UseInternalM3U8Function Then
                                            f = DownloadM3U8(v.URL, v, f)
                                            If f.IsEmptyString Then Throw New Exception("M3U8 download failed")
                                        Else
                                            w.DownloadFile(v.URL, f.ToString)
                                        End If

                                        If __isVideo Then
                                            v.Type = UTypes.Video
                                            DownloadedVideos(False) += 1
                                        Else
                                            v.Type = UTypes.Picture
                                            DownloadedPictures(False) += 1
                                        End If

                                        v.File = ChangeFileNameByProvider(f, v)
                                        v.State = UStates.Downloaded
                                        dCount += 1
                                    Catch wex As Exception
                                        v.Attempts += 1
                                        v.State = UStates.Missing
                                        If MissingErrorsAdd Then ErrorDownloading(f, v.URL)
                                    End Try
                                Else
                                    v.State = UStates.Skipped
                                End If
                                _ContentNew(i) = v
                                If DownloadTopCount.HasValue AndAlso dCount >= DownloadTopCount.Value Then
                                    Progress.Perform(_ContentNew.Count - dTotal)
                                    Exit Sub
                                Else
                                    dTotal += 1
                                    Progress.Perform()
                                End If
                            Next
                        End Using
                    End If
                End If
            Catch iex As IndexOutOfRangeException When Disposed
            Catch oex As OperationCanceledException When Token.IsCancellationRequested
            Catch dex As ObjectDisposedException When Disposed
            Catch ex As Exception
                LogError(ex, "content downloading error")
                HasError = True
            End Try
        End Sub
        Protected UseInternalM3U8Function As Boolean = False
        Protected Overridable Function DownloadM3U8(URL As String, Media As UserMedia, DestinationFile As SFile) As SFile
            Return Nothing
        End Function
        Protected Const EXCEPTION_OPERATION_CANCELED As Integer = -1
        ''' <param name="RDE">Request DownloadingException</param>
        ''' <returns>0 - exit</returns>
        Protected Function ProcessException(ex As Exception, Token As CancellationToken, Message As String,
                                            Optional RDE As Boolean = True, Optional EObj As Object = Nothing,
                                            Optional ThrowEx As Boolean = True) As Integer
            If Not ((TypeOf ex Is OperationCanceledException And Token.IsCancellationRequested) Or
                    (TypeOf ex Is ObjectDisposedException And Disposed)) Then
                If RDE Then
                    Dim v% = DownloadingException(ex, Message, True, EObj)
                    If v = 0 Then LogError(ex, Message) : HasError = True
                    Return v
                End If
            Else
                'URGENT: UserDataBase.ProcessException [Throw ex]
                If ThrowEx Then Throw ex Else Return EXCEPTION_OPERATION_CANCELED
            End If
            Return 0
        End Function
        ''' <summary>0 - Execute LogError and set HasError</summary>
        Protected MustOverride Function DownloadingException(ex As Exception, Message As String, Optional FromPE As Boolean = False, Optional EObj As Object = Nothing) As Integer
        Protected Function ChangeFileNameByProvider(f As SFile, m As UserMedia) As SFile
            Dim ff As SFile = Nothing
            Try
                If f.Exists Then
                    If Not Settings.FileReplaceNameByDate.Value = FileNameReplaceMode.None Then
                        ff = f
                        ff.Name = String.Format(FileDateAppenderPattern, f.Name, CStr(AConvert(Of String)(If(m.Post.Date, Now), FileDateAppenderProvider, String.Empty)))
                        ff = SFile.Indexed_IndexFile(ff,, New NumberedFile(ff))
                    End If
                    If Not ff.Name.IsEmptyString Then My.Computer.FileSystem.RenameFile(f, ff.File) : Return ff
                End If
                Return f
            Catch ex As Exception
                LogError(ex, $"change file name from [{f}] to [{ff}]")
                Return f
            End Try
        End Function
        Private Sub RunScript()
            Try
                Const spa$ = "{0}"
                If ScriptUse Then
                    Dim ScriptPattern$
                    If Not ScriptData.IsEmptyString Then
                        ScriptPattern = ScriptData
                    Else
                        ScriptPattern = Settings.ScriptData.Value
                    End If
                    If Not ScriptPattern.IsEmptyString Then
                        If Not ScriptPattern.Contains(spa) Then ScriptPattern &= $" ""{spa}"""
                        Using b As New BatchExecutor With {.RedirectStandardError = True}
                            b.Execute({String.Format(ScriptPattern, MyFile.CutPath(1).PathNoSeparator)}, EDP.SendInLog + EDP.ThrowException)
                            If b.HasError Or Not b.ErrorOutput.IsEmptyString Then Throw New Exception(b.ErrorOutput, b.ErrorException)
                        End Using
                    End If
                End If
            Catch ex As Exception
                LogError(ex, "script execution error")
            End Try
        End Sub
#End Region
#Region "Delete, Move, Merge, Copy"
        Friend Overridable Function Delete(Optional Multiple As Boolean = False, Optional CollectionValue As Integer = -1) As Integer Implements IUserData.Delete
            Dim f As SFile = SFile.GetPath(MyFile.CutPath.Path)
            If f.Exists(SFO.Path, False) AndAlso (User.Merged OrElse f.Delete(SFO.Path, Settings.DeleteMode)) Then
                If Not IncludedInCollection Then MainFrameObj.ImageHandler(Me, False)
                Settings.UsersList.Remove(User)
                Settings.UpdateUsersList()
                If Not IncludedInCollection Then Settings.Users.Remove(Me)
                Downloader.UserRemove(Me)
                Dispose(True)
                Return 1
            Else
                Return 0
            End If
        End Function
        Friend Overridable Function MoveFiles(__CollectionName As String, __SpecialCollectionPath As SFile) As Boolean Implements IUserData.MoveFiles
            Dim UserBefore As UserInfo = User
            Dim Removed As Boolean = True
            Dim _TurnBack As Boolean = False
            Try
                Dim f As SFile
                Dim v As Boolean = IsVirtual
                If IncludedInCollection Then
                    Settings.Users.Add(Me)
                    Removed = False
                    User.CollectionName = String.Empty
                    User.SpecialCollectionPath = String.Empty
                    User.UserModel = UsageModel.Default
                    User.CollectionModel = UsageModel.Default
                Else
                    Settings.Users.Remove(Me)
                    Removed = True
                    User.CollectionName = __CollectionName
                    User.SpecialCollectionPath = __SpecialCollectionPath
                    If Not IsVirtual Then User.SpecialPath = Nothing
                End If
                _TurnBack = True
                User.UpdateUserFile()

                If Not v Then
                    f = User.File.CutPath(, EDP.ThrowException)
                    If f.Exists(SFO.Path, False) Then
                        If If(SFile.GetFiles(f,, SearchOption.AllDirectories), New List(Of SFile)).Count > 0 AndAlso
                           MsgBoxE({$"Destination directory [{f.Path}] already exists and contains files!" & vbCr &
                                    "By continuing, this directory and all files will be deleted",
                                    "Destination directory is not empty!"}, MsgBoxStyle.Exclamation,,, {"Delete", "Cancel"}) = 1 Then
                            MsgBoxE("Operation canceled", MsgBoxStyle.Exclamation)
                            User = UserBefore
                            If Removed Then Settings.Users.Add(Me) Else Settings.Users.Remove(Me)
                            _TurnBack = False
                            Return False
                        End If
                        f.Delete(SFO.Path, Settings.DeleteMode, EDP.ThrowException)
                    End If
                    f.CutPath.Exists(SFO.Path)
                    Directory.Move(UserBefore.File.CutPath(, EDP.ThrowException).Path, f.Path)
                    If Not ScriptData.IsEmptyString AndAlso ScriptData.Contains(UserBefore.File.PathNoSeparator) Then _
                       ScriptData = ScriptData.Replace(UserBefore.File.PathNoSeparator, MyFile.PathNoSeparator)
                End If

                Settings.UsersList.Remove(UserBefore)
                Settings.UpdateUsersList(User)
                UpdateUserInformation()
                Return True
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.LogMessageValue, ex, "Files moving error")
                User = UserBefore
                If _TurnBack Then
                    If Removed Then Settings.Users.Add(Me) Else Settings.Users.Remove(Me)
                End If
                Return False
            End Try
        End Function
        Friend Overloads Sub MergeData()
            Dim UserBefore As UserInfo = User
            Try
                If DataMerging Then
                    Throw New InvalidOperationException($"{Site} - {Name}: data already merged") With {.HelpLink = 1}
                Else
                    Dim files As List(Of SFile) = Nothing
                    Dim allFiles As List(Of SFile) = Nothing
                    Dim fSource As SFile, fDest As SFile
                    Dim ReplacingPath$ = UserBefore.File.CutPath.Path
                    Dim dirs As List(Of SFile) = SFile.GetDirectories(UserBefore.File.CutPath,, SearchOption.AllDirectories)
                    Dim FilesMover As Action = Sub()
                                                   If files.ListExists Then
                                                       For Each fSource In files
                                                           fDest = fSource
                                                           fDest.Path = User.File.CutPath.PathWithSeparator & fSource.Path.Replace(ReplacingPath, String.Empty)
                                                           fDest.Exists(SFO.Path,, EDP.ThrowException)
                                                           fDest = CheckFile(fDest, allFiles)
                                                           File.Move(fSource, fDest)
                                                       Next
                                                       files.Clear()
                                                   End If
                                               End Sub
                    DataMerging = True
                    If dirs.ListExists Then
                        For Each dir As SFile In dirs
                            allFiles = SFile.GetFiles(SFile.GetPath(User.File.CutPath.PathWithSeparator &
                                                                    dir.Path.Replace(ReplacingPath, String.Empty)),,, EDP.ReturnValue)
                            files = SFile.GetFiles(dir,,, EDP.ReturnValue)
                            FilesMover.Invoke
                        Next
                    End If
                    allFiles = SFile.GetFiles(User.File.CutPath,,, EDP.ReturnValue)

                    files = SFile.GetFiles(UserBefore.File.CutPath,,, EDP.ReturnValue)
                    FilesMover.Invoke
                    If SFile.GetFiles(UserBefore.File.CutPath,, SearchOption.AllDirectories,
                                      New ErrorsDescriber(False, False, False, New List(Of SFile))).Count = 0 Then
                        UserBefore.File.CutPath.Delete(SFO.Path, Settings.DeleteMode, EDP.SendInLog)
                    End If
                    If Not ScriptData.IsEmptyString AndAlso ScriptData.Contains(UserBefore.File.PathNoSeparator) Then _
                       ScriptData = ScriptData.Replace(UserBefore.File.PathNoSeparator, MyFile.PathNoSeparator)
                    UpdateUserInformation()
                End If
            Catch ioex As InvalidOperationException When ioex.HelpLink = 1
                MsgBoxE(ioex.Message, vbCritical)
            Catch ex As Exception
                LogError(ex, "[UserDataBase.MergeData]")
            End Try
        End Sub
        Private Function CheckFile(f As SFile, ByRef List As IEnumerable(Of SFile)) As SFile
            If List.ListExists Then
                Dim p As RParams = RParams.DMS(".+?\s{0,1}\((\d+)\)|.+", 0, EDP.ReturnValue)
                Dim i% = List.Where(Function(ff) CStr(RegexReplace(ff.Name, p)).Trim.ToLower = f.Name.Trim.ToLower).Count
                If i > 0 Then f.Name &= $" ({i + 1})"
            End If
            Return f
        End Function
        Private Class FilesCopyingException : Inherits ErrorsDescriberException
            Friend Sub New(User As IUserData, Msg As String, Path As SFile)
                SendInLogOnlyMessage = True
                If User.IncludedInCollection Then _MainMessage = $"[{User.CollectionName}] - "
                _MainMessage &= $"[{User.Site}] - [{User.Name}]. {Msg}: {Path.Path}."
            End Sub
        End Class
        Friend Overridable Function CopyFiles(DestinationPath As SFile, Optional e As ErrorsDescriber = Nothing) As Boolean Implements IUserData.CopyFiles
            Dim fSource As SFile = Nothing
            Dim fDest As SFile = Nothing
            Try
                Dim pOffset%
                If IncludedInCollection Then
                    If DataMerging Then pOffset = 1 Else pOffset = 2
                Else
                    pOffset = 1
                End If
                fSource = User.File.CutPath(pOffset).Path.CSFileP

                Dim OptPath$ = String.Empty
                If IncludedInCollection Then
                    OptPath = $"Collections\{CollectionName}" 'Copying a collection based on the first file
                Else
                    OptPath = $"{Site}\{Name}"
                End If
                fDest = $"{DestinationPath.PathWithSeparator}{OptPath}".CSFileP
                If fDest.Exists(SFO.Path, False) AndAlso MsgBoxE({$"The following path already exists:{vbCr}{fDest.Path}" & vbCr &
                                                                  "Do you want to copy files here?", "Copying files"}, vbExclamation + vbYesNo) = vbNo Then _
                   Throw New FilesCopyingException(Me, "The following path already exists", fDest)

                If DestinationPath.Exists(SFO.Path, True) Then
                    My.Computer.FileSystem.CopyDirectory(fSource, fDest, FileIO.UIOption.OnlyErrorDialogs, FileIO.UICancelOption.ThrowException)
                Else
                    Throw New FilesCopyingException(Me, "Cannot create the following path", fDest)
                End If
                Return True
            Catch cex As OperationCanceledException
                Return ErrorsDescriber.Execute(e, New FilesCopyingException(Me, "Copy canceled", fDest),, False)
            Catch ex As Exception
                Return ErrorsDescriber.Execute(e, ex,, False)
            End Try
        End Function
#End Region
#Region "Errors functions"
        Protected Sub LogError(ex As Exception, Message As String)
            ErrorsDescriber.Execute(EDP.SendInLog, ex, $"{ToStringForLog()}: {Message}")
        End Sub
        Protected Sub ErrorDownloading(f As SFile, URL As String)
            If Not f.Exists Then MyMainLOG = $"Error downloading from [{URL}] to [{f}]"
        End Sub
        ''' <exception cref="ObjectDisposedException"></exception>
        Protected Sub ThrowIfDisposed()
            If Disposed Then Throw New ObjectDisposedException(ToString(), "Object disposed")
        End Sub
        ''' <inheritdoc cref="ThrowAny(CancellationToken)"/>
        Private Overloads Sub ThrowAny() Implements IThrower.ThrowAny
            ThrowAny(_ExternalCompatibilityToken)
        End Sub
        ''' <exception cref="OperationCanceledException"></exception>
        ''' <exception cref="ObjectDisposedException"></exception>
        Friend Overridable Overloads Sub ThrowAny(Token As CancellationToken)
            Token.ThrowIfCancellationRequested()
            ThrowIfDisposed()
        End Sub
#End Region
        Protected Function ToStringForLog() As String
            Return $"{IIf(IncludedInCollection, $"[{CollectionName}] - ", String.Empty)}[{Site}] - {Name}"
        End Function
        Public Overrides Function ToString() As String
            If IsCollection Then
                Return CollectionName
            Else
                Return IIf(FriendlyName.IsEmptyString, Name, FriendlyName)
            End If
        End Function
        Public Overrides Function GetHashCode() As Integer
            Dim hcStr$
            If Not CollectionName.IsEmptyString Then
                hcStr = CollectionName
            Else
                hcStr = IIf(FriendlyName.IsEmptyString, Name, FriendlyName)
            End If
            If hcStr.IsEmptyString Then hcStr = LVIKey
            Return hcStr.GetHashCode
        End Function
#Region "Buttons actions"
        Private Sub BTT_CONTEXT_DOWN_KeyClick(sender As Object, e As MyKeyEventArgs) Handles BTT_CONTEXT_DOWN.KeyClick
            Downloader.Add(Me, e.IncludeInTheFeed)
        End Sub
        Private Sub BTT_CONTEXT_EDIT_Click(sender As Object, e As EventArgs) Handles BTT_CONTEXT_EDIT.Click
            Using f As New Editors.UserCreatorForm(Me)
                f.ShowDialog()
                If f.DialogResult = DialogResult.OK Then UpdateUserInformation()
            End Using
        End Sub
        Private Sub BTT_CONTEXT_DELETE_Click(sender As Object, e As EventArgs) Handles BTT_CONTEXT_DELETE.Click
        End Sub
        Private Sub BTT_CONTEXT_OPEN_PATH_Click(sender As Object, e As EventArgs) Handles BTT_CONTEXT_OPEN_PATH.Click
            OpenFolder()
        End Sub
        Private Sub BTT_CONTEXT_OPEN_SITE_Click(sender As Object, e As EventArgs) Handles BTT_CONTEXT_OPEN_SITE.Click
            OpenSite()
        End Sub
#End Region
#Region "IComparable Support"
        Friend Overridable Function CompareTo(Other As UserDataBase) As Integer Implements IComparable(Of UserDataBase).CompareTo
            Return Name.CompareTo(Other.Name)
        End Function
        Friend Overridable Function CompareTo(Obj As Object) As Integer Implements IComparable.CompareTo
            If Obj IsNot Nothing AndAlso TypeOf Obj Is UserDataBase Then
                Return CompareTo(DirectCast(Obj, UserDataBase))
            Else
                Return False
            End If
        End Function
#End Region
#Region "IEquatable Support"
        Friend Overridable Overloads Function Equals(Other As UserDataBase) As Boolean Implements IEquatable(Of UserDataBase).Equals
            Return LVIKey = Other.LVIKey And IsSavedPosts = Other.IsSavedPosts
        End Function
        Public Overrides Function Equals(Obj As Object) As Boolean
            If Obj IsNot Nothing AndAlso TypeOf Obj Is UserDataBase Then
                Return Equals(DirectCast(Obj, UserDataBase))
            Else
                Return False
            End If
        End Function
#End Region
#Region "IDisposable Support"
        Protected disposedValue As Boolean = False
        Friend ReadOnly Property Disposed As Boolean Implements IUserData.Disposed
            Get
                Return disposedValue
            End Get
        End Property
        Protected Overridable Overloads Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    _ContentList.Clear()
                    _ContentNew.Clear()
                    LatestData.Clear()
                    _TempMediaList.Clear()
                    _TempPostsList.Clear()
                    If Responser IsNot Nothing Then Responser.Dispose()
                    If BTT_CONTEXT_DOWN IsNot Nothing Then BTT_CONTEXT_DOWN.Dispose()
                    If BTT_CONTEXT_EDIT IsNot Nothing Then BTT_CONTEXT_EDIT.Dispose()
                    If BTT_CONTEXT_DELETE IsNot Nothing Then BTT_CONTEXT_DELETE.Dispose()
                    If BTT_CONTEXT_OPEN_PATH IsNot Nothing Then BTT_CONTEXT_OPEN_PATH.Dispose()
                    If BTT_CONTEXT_OPEN_SITE IsNot Nothing Then BTT_CONTEXT_OPEN_SITE.Dispose()
                    UserUpdatedEventHandlers.Clear()
                End If
                disposedValue = True
            End If
        End Sub
        Protected Overloads Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub
        Friend Overloads Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class
#Region "Base interfaces"
    Friend Interface IContentProvider
        ReadOnly Property Site As String
        Property Name As String
        Property ID As String
        Property FriendlyName As String
        Property Description As String
        Property Favorite As Boolean
        Property Temporary As Boolean
        Sub OpenSite(Optional e As ErrorsDescriber = Nothing)
        Sub DownloadData(Token As CancellationToken)
    End Interface
    Friend Interface IUserData : Inherits IContentProvider, IComparable(Of UserDataBase), IComparable, IEquatable(Of UserDataBase), IIndexable, IDisposable
        Event UserUpdated(User As IUserData)
        Property ParseUserMediaOnly As Boolean
#Region "Images"
        Function GetPicture() As Image
        Sub SetPicture(f As SFile)
#End Region
#Region "Collection support"
        ReadOnly Property IsCollection As Boolean
        Property CollectionName As String
        ReadOnly Property IncludedInCollection As Boolean
        ReadOnly Property UserModel As UsageModel
        ReadOnly Property CollectionModel As UsageModel
        ReadOnly Property IsVirtual As Boolean
        ReadOnly Property Labels As List(Of String)
#End Region
        ReadOnly Property IsChannel As Boolean
        Property Exists As Boolean
        Property Suspended As Boolean
        Property ReadyForDownload As Boolean
        Property HOST As SettingsHost
        Property [File] As SFile
        Property FileExists As Boolean
        Property DownloadedPictures(Total As Boolean) As Integer
        Property DownloadedVideos(Total As Boolean) As Integer
        ReadOnly Property DownloadedTotal(Optional Total As Boolean = True) As Integer
        ReadOnly Property DownloadedInformation As String
        Property HasError As Boolean
        ReadOnly Property FitToAddParams As Boolean
        ReadOnly Property Key As String
        Property DownloadImages As Boolean
        Property DownloadVideos As Boolean
        Property DownloadMissingOnly As Boolean
        Property ScriptUse As Boolean
        Property ScriptData As String
        Function GetLVI(Destination As ListView) As ListViewItem
        Function GetLVIGroup(Destination As ListView) As ListViewGroup
        Sub LoadUserInformation()
        Sub UpdateUserInformation()
        ''' <summary>
        ''' 0 - Nothing removed<br/>
        ''' 1 - User removed<br/>
        ''' 2 - Collection removed<br/>
        ''' 3 - Collection split
        ''' </summary>
        Function Delete(Optional Multiple As Boolean = False, Optional CollectionValue As Integer = -1) As Integer
        Function MoveFiles(CollectionName As String, SpecialCollectionPath As SFile) As Boolean
        Function CopyFiles(DestinationPath As SFile, Optional e As ErrorsDescriber = Nothing) As Boolean
        Sub OpenFolder()
        ReadOnly Property Self As IUserData
        Property DownloadTopCount As Integer?
        Property DownloadDateFrom As Date?
        Property DownloadDateTo As Date?
        Sub SetEnvironment(ByRef h As SettingsHost, u As UserInfo, _LoadUserInformation As Boolean,
                           Optional AttachUserInfo As Boolean = True)
        ReadOnly Property Disposed As Boolean
    End Interface
    Friend Interface IChannelLimits
        Property AutoGetLimits As Boolean
        Property DownloadLimitCount As Integer?
        Property DownloadLimitPost As String
        Property DownloadLimitDate As Date?
        Overloads Sub SetLimit(Optional Post As String = "", Optional Count As Integer? = Nothing, Optional [Date] As Date? = Nothing)
        Overloads Sub SetLimit(Source As IChannelLimits)
    End Interface
    Friend Interface IChannelData : Inherits IContentProvider, IChannelLimits
        Property SkipExistsUsers As Boolean
        Property SaveToCache As Boolean
    End Interface
#End Region
End Namespace