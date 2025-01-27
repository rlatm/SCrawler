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
Imports SCrawler.DownloadObjects.Groups
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.XML.Base
Imports PersonalUtilities.Tools
Imports PersonalUtilities.Tools.Notifications
Namespace DownloadObjects
    Friend Class AutoDownloader : Inherits GroupParameters : Implements IIndexable, IEContainerProvider
        Friend Event PauseDisabled()
        Private Shared ReadOnly Property CachePath As SFile
            Get
                Return Settings.CachePath
            End Get
        End Property
        Friend Enum Modes As Integer
            None = 0
            [Default] = 1
            All = 2
            Specified = 3
            Groups = 4
        End Enum
        Friend Enum PauseModes As Integer
            Disabled = -2
            Enabled = -1
            Unlimited = -1
            Until = 0
            H1 = 1
            H2 = 2
            H3 = 3
            H4 = 4
            H6 = 6
            H12 = 12
        End Enum
        Friend Const DefaultTimer As Integer = 60
#Region "Notifications"
        Private Const KeyOpenFolder As String = "_____OPEN_FOLDER_SCRAWLER_AUTOMATION"
        Private Const KeyOpenSite As String = "_____OPEN_SITE_SCRAWLER_AUTOMATION"
        Private Const KeyBttDismiss As String = "_____DISMISS_SCRAWLER_AUTOMATION"
        Private Const KeyBttPhoto As String = "_____PHOTO_SCRAWLER_AUTOMATION"
        Private ReadOnly UserKeys As List(Of NotifiedUser)
        Private Class NotifiedUser : Implements IDisposable
            Private ReadOnly Property User As IUserData
            Friend ReadOnly Property IUserDataKey As String
            Private ReadOnly Property Key As String
            Private ReadOnly Property KeyFolder As String
            Private ReadOnly Property KeySite As String
            Private ReadOnly Property KeyDismiss As String
            Private ReadOnly Property Images As Dictionary(Of String, SFile)
            Private ReadOnly Property AutoDownloaderSource As AutoDownloader
            Private Sub New()
                Images = New Dictionary(Of String, SFile)
            End Sub
            Private Sub New(_Key As String)
                Me.New
                Key = _Key
                KeyFolder = $"{Key}{KeyOpenFolder}"
                KeySite = $"{Key}{KeyOpenSite}"
                KeyDismiss = $"{Key}{KeyBttDismiss}"
            End Sub
            Friend Sub New(_Key As String, ByRef _User As IUserData, ByRef Source As AutoDownloader)
                Me.New(_Key)
                User = _User
                IUserDataKey = _User.Key
                AutoDownloaderSource = Source
                If _User.IncludedInCollection Then
                    Dim cn$ = _User.CollectionName
                    Dim i% = Settings.Users.FindIndex(Function(u) u.IsCollection And u.Name = cn)
                    If i >= 0 Then IUserDataKey = Settings.Users(i).Key
                End If
            End Sub
            Public Shared Widening Operator CType(Key As String) As NotifiedUser
                Return New NotifiedUser(Key)
            End Operator
            Friend Sub ShowNotification()
                Try
                    If AutoDownloaderSource IsNot Nothing And Settings.ProcessNotification(SettingsCLS.NotificationObjects.AutoDownloader) Then
                        If AutoDownloaderSource.ShowNotifications Then
                            If User IsNot Nothing Then
                                Dim Text$ = $"{User.Site} - {User.Name}{vbNewLine}" &
                                            $"Downloaded: {User.DownloadedPictures(False)} images, {User.DownloadedVideos(False)} videos"
                                Dim Title$
                                If Not User.CollectionName.IsEmptyString Then
                                    Title = User.CollectionName
                                Else
                                    Title = User.ToString
                                End If
                                Using Notify As New Notification(Text, Title) With {.Key = Key}
                                    Dim uPic As SFile = Nothing
                                    Dim uif As SFile = Nothing
                                    Dim uif_orig As SFile = Nothing
                                    Dim uif_compressed As SFile = Nothing
                                    Dim uifKey$ = String.Empty
                                    If AutoDownloaderSource.ShowPictureUser Then uPic = DirectCast(User, UserDataBase).GetUserPictureToastAddress
                                    If AutoDownloaderSource.ShowPictureUser AndAlso uPic.Exists Then Notify.Images = {New ToastImage(uPic)}
                                    If AutoDownloaderSource.ShowPictureDownloaded And User.DownloadedPictures(False) > 0 Then
                                        uif = DirectCast(User, UserDataBase).GetLastImageAddress
                                        uif_orig = uif
                                        If uif.Exists Then
                                            uif_compressed = uif
                                            uif_compressed.Path = CachePath.Path
                                            uif_compressed.Name = $"360_{uif.Name}"
                                            Using imgR As New ImageRenderer(uif, EDP.SendInLog)
                                                Try : imgR.FitToWidth(360).Save(uif_compressed) : Catch : End Try
                                            End Using
                                            If uif_compressed.Exists Then uif = uif_compressed
                                            If uif.Exists Then
                                                Notify.Images = {New ToastImage(uif, IImage.Modes.Inline)}
                                                uifKey = $"{Key}_{Images.Keys.Count + 1}_{KeyBttPhoto}"
                                                If Not Images.ContainsKey(uifKey) Then Images.Add(uifKey, uif_orig)
                                            End If
                                        End If
                                    End If
                                    Notify.Buttons = {
                                        New ToastButton(KeyFolder, "Folder"),
                                        New ToastButton(KeySite, "Site")
                                    }
                                    If Not uifKey.IsEmptyString Then Notify.Buttons = {New ToastButton(uifKey, "Photo")}
                                    Notify.Buttons = {New ToastButton(KeyDismiss, "Dismiss")}
                                    Notify.Show()
                                End Using
                            End If
                        End If
                    End If
                Catch ex As Exception
                    ErrorsDescriber.Execute(EDP.SendInLog, ex, "[AutoDownloader.NotifiedUser.ShowNotification]")
                    If User IsNot Nothing Then
                        MainFrameObj.ShowNotification(SettingsCLS.NotificationObjects.AutoDownloader,
                                                      User.ToString & vbNewLine &
                                                      $"Downloaded: {User.DownloadedPictures(False)} images, {User.DownloadedVideos(False)} videos" &
                                                      If(User.HasError, vbNewLine & "With errors", String.Empty))
                    End If
                End Try
            End Sub
            ''' <returns>True to activate</returns>
            Friend Function Open(_Key As String) As Boolean
                If User IsNot Nothing Then
                    If KeyDismiss = _Key Then
                    ElseIf Key = _Key Then
                        Return True
                    ElseIf KeyFolder = _Key Then
                        User.OpenFolder()
                    ElseIf KeySite = _Key Then
                        User.OpenSite()
                    ElseIf Images.ContainsKey(_Key) Then
                        Images(_Key).Open(, EDP.None)
                    End If
                Else
                    Return True
                End If
                Return False
            End Function
            Public Overrides Function Equals(Obj As Object) As Boolean
                With CType(Obj, NotifiedUser)
                    Return .Key = Key Or .Key = KeyFolder Or .Key = KeySite Or .Key = KeyDismiss Or Images.ContainsKey(.Key)
                End With
            End Function
#Region "IDisposable Support"
            Private disposedValue As Boolean = False
            Protected Overridable Overloads Sub Dispose(disposing As Boolean)
                If Not disposedValue Then
                    If disposing Then Images.Clear()
                    disposedValue = True
                End If
            End Sub
            Protected Overrides Sub Finalize()
                Dispose(False)
                MyBase.Finalize()
            End Sub
            Friend Overloads Sub Dispose() Implements IDisposable.Dispose
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region
        End Class
#End Region
#Region "XML Names"
        Private Const Name_Mode As String = "Mode"
        Private Const Name_Groups As String = "Groups"
        Private Const Name_Timer As String = "Timer"
        Private Const Name_StartupDelay As String = "StartupDelay"
        Private Const Name_LastDownloadDate As String = "LastDownloadDate"
        Private Const Name_ShowNotifications As String = "Notify"
        Private Const Name_ShowPictureDown As String = "ShowDownloadedPicture"
        Private Const Name_ShowPictureUser As String = "ShowUserPicture"
        Private Const Name_ShowSimpleNotification As String = "ShowSimpleNotification"
#End Region
#Region "Declarations"
        Friend Property Source As Scheduler
        Private _Mode As Modes = Modes.None
        Friend Property Mode As Modes
            Get
                Return _Mode
            End Get
            Set(m As Modes)
                _Mode = m
                If _Mode = Modes.None Then [Stop]()
            End Set
        End Property
        Friend ReadOnly Property Groups As List(Of String)
        Friend Property Timer As Integer = DefaultTimer
        Friend Property StartupDelay As Integer = 1
        Friend Property ShowNotifications As Boolean = True
        Friend Property ShowPictureDownloaded As Boolean = True
        Friend Property ShowPictureUser As Boolean = True
        Friend Property ShowSimpleNotification As Boolean = False
        Private Property Index As Integer = -1 Implements IIndexable.Index
        Private Function SetIndex(Obj As Object, Index As Integer) As Object Implements IIndexable.SetIndex
            DirectCast(Obj, AutoDownloader).Index = Index
            Return Obj
        End Function
#Region "Date"
        Private ReadOnly LastDownloadDateXML As Date? = Nothing
        Private _LastDownloadDate As Date = Now.AddYears(-1)
        Private _LastDownloadDateChanged As Boolean = False
        Friend Property LastDownloadDate As Date
            Get
                Return _LastDownloadDate
            End Get
            Set(d As Date)
                _LastDownloadDate = d
                If Not Initialization Then _LastDownloadDateChanged = True
            End Set
        End Property
        Private ReadOnly Property NextExecutionDate As Date
            Get
                If _PauseValue.HasValue Then
                    Return {LastDownloadDate.AddMinutes(Timer), _StartTime.AddMinutes(StartupDelay), _PauseValue.Value}.Max
                Else
                    Return {LastDownloadDate.AddMinutes(Timer), _StartTime.AddMinutes(StartupDelay)}.Max
                End If
            End Get
        End Property
        Private ReadOnly DateProvider As New ADateTime(ADateTime.Formats.BaseDateTime)
        Private Function GetLastDateString() As String
            If LastDownloadDateXML.HasValue Or _LastDownloadDateChanged Then
                Return LastDownloadDate.ToStringDate(DateProvider)
            Else
                Return "never"
            End If
        End Function
        Private Function GetNextDateString() As String
            Return NextExecutionDate.ToStringDate(DateProvider)
        End Function
#End Region
#Region "Information"
        Friend ReadOnly Property Information As String
            Get
                Return $"Last download date: {GetLastDateString()} ({GetWorkingState()})"
            End Get
        End Property
        Private Function GetWorkingState() As String
            Dim OutStr$
            If Working Then
                If StartupDelay > 0 And _StartTime.AddMinutes(StartupDelay) > Now Then
                    OutStr = $"delayed until {_StartTime.AddMinutes(StartupDelay).ToStringDate(ADateTime.Formats.BaseDateTime)}"
                ElseIf _StopRequested Then
                    OutStr = "stopping"
                Else
                    OutStr = "working"
                End If
                If IsPaused Then
                    Select Case Pause
                        Case PauseModes.Unlimited : OutStr &= ", paused"
                        Case PauseModes.Until : OutStr &= $", paused (until {AConvert(Of String)(_PauseValue, ADateTime.Formats.BaseDateTime, "?")})"
                        Case Else : OutStr &= $", paused ([{Pause}] until {AConvert(Of String)(_PauseValue, ADateTime.Formats.BaseDateTime, "?")})"
                    End Select
                End If
            Else
                OutStr = "stopped"
            End If
            Return OutStr
        End Function
        Public Overrides Function ToString() As String
            Return $"{Name} ({GetWorkingState()}): last download date: {GetLastDateString()}; next run: {GetNextDateString()}"
        End Function
#End Region
#End Region
#Region "Initializer"
        Private ReadOnly Initialization As Boolean = True
        Private _IsNewPlan As Boolean = False
        Friend ReadOnly Property IsNewPlan As Boolean
            Get
                Return _IsNewPlan
            End Get
        End Property
        Friend Sub New(Optional IsNewPlan As Boolean = False)
            Groups = New List(Of String)
            UserKeys = New List(Of NotifiedUser)
            _IsNewPlan = IsNewPlan
        End Sub
        Friend Sub New(x As EContainer)
            Me.New
            Mode = x.Value(Name_Mode).FromXML(Of Integer)(Modes.None)
            Import(x)
            If Name.IsEmptyString Then Name = "Default"
            Groups.ListAddList(x.Value(Name_Groups).StringToList(Of String)("|"), LAP.NotContainsOnly)

            Timer = x.Value(Name_Timer).FromXML(Of Integer)(DefaultTimer)
            If Timer <= 0 Then Timer = DefaultTimer
            StartupDelay = x.Value(Name_StartupDelay).FromXML(Of Integer)(0)
            If StartupDelay < 0 Then StartupDelay = 0
            ShowNotifications = x.Value(Name_ShowNotifications).FromXML(Of Boolean)(True)
            ShowPictureDownloaded = x.Value(Name_ShowPictureDown).FromXML(Of Boolean)(True)
            ShowPictureUser = x.Value(Name_ShowPictureUser).FromXML(Of Boolean)(True)
            ShowSimpleNotification = x.Value(Name_ShowSimpleNotification).FromXML(Of Boolean)(False)
            LastDownloadDateXML = AConvert(Of Date)(x.Value(Name_LastDownloadDate), DateProvider, Nothing)
            If LastDownloadDateXML.HasValue Then
                LastDownloadDate = LastDownloadDateXML.Value
            Else
                LastDownloadDate = Now.AddYears(-1)
            End If
            Initialization = False
        End Sub
#End Region
#Region "Groups Support"
        Friend Sub GROUPS_Updated(Sender As DownloadGroup)
            If Groups.Count > 0 Then
                Dim i% = Groups.IndexOf(Sender.NameBefore)
                If i >= 0 Then Groups(i) = Sender.Name : Update()
            End If
        End Sub
        Friend Sub GROUPS_Deleted(Sender As DownloadGroup)
            If Groups.Count > 0 Then
                Dim i% = Groups.IndexOf(Sender.Name)
                If i >= 0 Then Groups.RemoveAt(i) : Update()
            End If
        End Sub
#End Region
#Region "Update"
        Friend Sub Update()
            If Source IsNot Nothing Then Source.Update()
        End Sub
        Private Function ToEContainer(Optional e As ErrorsDescriber = Nothing) As EContainer Implements IEContainerProvider.ToEContainer
            Return Export(New EContainer(Scheduler.Name_Plan, String.Empty) From {
                                         New EContainer(Name_Mode, CInt(Mode)),
                                         New EContainer(Name_Groups, Groups.ListToString("|")),
                                         New EContainer(Name_Timer, Timer),
                                         New EContainer(Name_StartupDelay, StartupDelay),
                                         New EContainer(Name_ShowNotifications, ShowNotifications.BoolToInteger),
                                         New EContainer(Name_ShowPictureDown, ShowPictureDownloaded.BoolToInteger),
                                         New EContainer(Name_ShowPictureUser, ShowPictureUser.BoolToInteger),
                                         New EContainer(Name_ShowSimpleNotification, ShowSimpleNotification.BoolToInteger),
                                         New EContainer(Name_LastDownloadDate, CStr(AConvert(Of String)(If(LastDownloadDateXML.HasValue Or _LastDownloadDateChanged,
                                                                                                           CObj(LastDownloadDate), Nothing), DateProvider, String.Empty)))
                          })
        End Function
#End Region
#Region "Execution"
        Private AThread As Thread
        Friend ReadOnly Property Working As Boolean
            Get
                Return If(AThread?.IsAlive, False)
            End Get
        End Property
        Private _StartTime As Date = Now
        Friend Sub Start(Init As Boolean)
            If Init Then _StartTime = Now
            _IsNewPlan = False
            If Not Working And Not Mode = Modes.None Then
                AThread = New Thread(New ThreadStart(AddressOf Checker))
                AThread.SetApartmentState(ApartmentState.MTA)
                AThread.Start()
            End If
        End Sub
        Private _StopRequested As Boolean = False
        Private _Pause As PauseModes = PauseModes.Disabled
        Private _PauseValue As Date? = Nothing
        Friend Property Pause(Optional DateLimit As Date? = Nothing) As PauseModes
            Get
                Return _Pause
            End Get
            Set(p As PauseModes)
                _Pause = p
                Select Case p
                    Case PauseModes.Disabled, PauseModes.Unlimited : _PauseValue = Nothing
                    Case PauseModes.H1 : _PauseValue = Now.AddHours(1)
                    Case PauseModes.H2 : _PauseValue = Now.AddHours(2)
                    Case PauseModes.H3 : _PauseValue = Now.AddHours(3)
                    Case PauseModes.H4 : _PauseValue = Now.AddHours(4)
                    Case PauseModes.H6 : _PauseValue = Now.AddHours(6)
                    Case PauseModes.H12 : _PauseValue = Now.AddHours(12)
                    Case PauseModes.Until : _PauseValue = DateLimit
                    Case Else : _PauseValue = Nothing
                End Select
            End Set
        End Property
        Private ReadOnly Property IsPaused As Boolean
            Get
                If Not _Pause = PauseModes.Disabled Then
                    If _PauseValue.HasValue Then
                        If _PauseValue.Value > Now Then
                            Return True
                        Else
                            _Pause = PauseModes.Disabled
                            _PauseValue = Nothing
                            RaiseEvent PauseDisabled()
                            Return False
                        End If
                    Else
                        Return True
                    End If
                Else
                    Return False
                End If
            End Get
        End Property
        Friend Sub [Stop]()
            If Working Then _StopRequested = True
        End Sub
        Friend Sub Skip()
            If LastDownloadDate.AddMinutes(Timer) <= Now Then
                LastDownloadDate = Now.AddMinutes(Timer)
            Else
                LastDownloadDate = LastDownloadDate.AddMinutes(Timer)
            End If
        End Sub
        Private _SpecialDelayUse As Boolean = False
        Private _SpecialDelayTime As Date? = Nothing
        Private Sub Checker()
            Try
                Dim _StartDownload As Boolean
                While (Not _StopRequested Or Downloader.Working) And Not Mode = Modes.None
                    If LastDownloadDate.AddMinutes(Timer) < Now And _StartTime.AddMinutes(StartupDelay) < Now And
                       Not IsPaused And Not _StopRequested And Not Mode = Modes.None Then
                        If Downloader.Working Then
                            _SpecialDelayUse = True
                        Else
                            If _SpecialDelayUse And Not _SpecialDelayTime.HasValue Then _SpecialDelayTime = Now.AddSeconds(10)
                            If Not _SpecialDelayUse OrElse (_SpecialDelayTime.HasValue AndAlso _SpecialDelayTime.Value < Now) Then
                                _SpecialDelayUse = False
                                _SpecialDelayTime = Nothing
                                _StartDownload = False
                                If Settings.Automation.Count = 1 Then
                                    _StartDownload = True
                                ElseIf Index = -1 Then
                                    _StartDownload = True
                                Else
                                    _StartDownload = NextExecutionDate.AddMilliseconds(1000 * (Index + 1)).Ticks <= Now.Ticks
                                End If
                                If _StartDownload Then Download()
                            End If
                        End If
                    End If
                    Thread.Sleep(500)
                End While
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.SendInLog, ex, "[AutoDownloader.Checker]")
            Finally
                _StopRequested = False
            End Try
        End Sub
        Private _Downloading As Boolean = False
        Friend ReadOnly Property Downloading As Boolean
            Get
                Return _Downloading
            End Get
        End Property
        Private Sub Download()
            _Downloading = True
            Dim Keys As New List(Of String)
            Try
                Dim users As New List(Of IUserData)
                Dim GName$
                Dim i%
                Dim DownloadedUsersCount% = 0
                Dim simple As Boolean = ShowSimpleNotification And ShowNotifications
                Dim notify As Action = Sub()
                                           Try
                                               With Downloader.Downloaded
                                                   If ShowNotifications And .Count > 0 Then
                                                       For indx% = 0 To .Count - 1
                                                           With .Item(indx)
                                                               If Keys.Contains(.Key) Then
                                                                   If simple Then
                                                                       DownloadedUsersCount += 1
                                                                   Else
                                                                       ShowNotification(.Self)
                                                                   End If
                                                                   Keys.Remove(.Key)
                                                               End If
                                                           End With
                                                       Next
                                                   End If
                                               End With
                                           Catch n_ex As Exception
                                           End Try
                                       End Sub
                Select Case Mode
                    Case Modes.All
                        Dim CheckLabels As Predicate(Of IUserData) = Function(u As IUserData) As Boolean
                                                                         If LabelsExcluded.Count = 0 Then
                                                                             Return True
                                                                         ElseIf u.Labels.Count = 0 Then
                                                                             Return True
                                                                         Else
                                                                             Return Not u.Labels.ListContains(LabelsExcluded)
                                                                         End If
                                                                     End Function
                        Dim CheckSites As Predicate(Of IUserData) = Function(u) SitesExcluded.Count = 0 OrElse Not SitesExcluded.Contains(u.Site)
                        users.ListAddList(Settings.GetUsers(Function(u) UserExistsPredicate(u) And CheckLabels.Invoke(u) And CheckSites.Invoke(u)))
                    Case Modes.Default
                        Using g As New GroupParameters : users.ListAddList(DownloadGroup.GetUsers(g, True)) : End Using
                    Case Modes.Specified : users.ListAddList(DownloadGroup.GetUsers(Me, True))
                    Case Modes.Groups
                        If Groups.Count > 0 And Settings.Groups.Count > 0 Then
                            For Each GName In Groups
                                i = Settings.Groups.IndexOf(GName)
                                If i >= 0 Then users.ListAddList(Settings.Groups(i).GetUsers, LAP.IgnoreICopier, LAP.NotContainsOnly)
                            Next
                        End If
                End Select
                If users.Count > 0 Then
                    Keys.ListAddList(users.Select(Function(u) u.Key))
                    With Downloader
                        .AutoDownloaderWorking = True
                        If .Downloaded.Count > 0 Then .Downloaded.RemoveAll(Function(u) Keys.Contains(u.Key)) : .InvokeDownloadsChangeEvent()
                        .AddRange(users, True)
                        While .Working Or .Count > 0 : notify.Invoke() : Thread.Sleep(200) : End While
                        .AutoDownloaderWorking = False
                        notify.Invoke
                        If simple And DownloadedUsersCount > 0 Then _
                           MainFrameObj.ShowNotification(SettingsCLS.NotificationObjects.AutoDownloader,
                                                         $"{DownloadedUsersCount} user(s) downloaded with scheduler plan '{Name}'")
                    End With
                End If
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.SendInLog, ex, "[AutoDownloader.Download]")
            Finally
                Keys.Clear()
                LastDownloadDate = Now
                Update()
                _Downloading = False
            End Try
        End Sub
        Private Sub ShowNotification(u As IUserData)
            Dim k$ = $"{Name}_{u.Key}"
            Dim i% = UserKeys.IndexOf(k)
            If i >= 0 Then
                UserKeys(i).ShowNotification()
            Else
                UserKeys.Add(New NotifiedUser(k, Settings.GetUser(u), Me))
                UserKeys.Last.ShowNotification()
            End If
        End Sub
        Friend Function NotificationClicked(Key As String, ByRef Found As Boolean, ByRef ActivateForm As Boolean) As Boolean
            Dim i% = UserKeys.IndexOf(Key)
            If i >= 0 Then
                Found = True
                ActivateForm = UserKeys(i).Open(Key)
                MainFrameObj.FocusUser(UserKeys(i).IUserDataKey, ActivateForm)
                Return True
            Else
                Return False
            End If
        End Function
#End Region
#Region "IDisposable Support"
        Protected Overrides Sub Dispose(disposing As Boolean)
            If Not disposedValue And disposing Then
                [Stop]()
                UserKeys.ListClearDispose()
                Groups.Clear()
            End If
            MyBase.Dispose(disposing)
        End Sub
#End Region
    End Class
End Namespace