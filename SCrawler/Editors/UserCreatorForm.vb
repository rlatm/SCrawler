﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports PersonalUtilities.Forms
Imports PersonalUtilities.Forms.Controls
Imports PersonalUtilities.Forms.Controls.Base
Imports PersonalUtilities.Functions.RegularExpressions
Imports PersonalUtilities.Tools
Imports SCrawler.API
Imports SCrawler.API.Base
Imports SCrawler.Plugin
Imports SCrawler.Plugin.Hosts
Imports ADB = PersonalUtilities.Forms.Controls.Base.ActionButton.DefaultButtons
Namespace Editors
    Friend Class UserCreatorForm
        Private WithEvents MyDef As DefaultFormOptions
        Friend Property User As UserInfo
        Private Property UserInstance As IUserData
        Private ReadOnly UserIsCollection As Boolean = False
#Region "User options"
        ''' <summary>COLLECTION EDITING ONLY</summary>
        Friend Property CollectionName As String = String.Empty
        Friend Property StartIndex As Integer = -1
        Friend ReadOnly Property UserTemporary As Boolean
            Get
                If CH_FAV.Checked Then
                    Return False
                Else
                    Return CH_TEMP.Checked
                End If
            End Get
        End Property
        Friend ReadOnly Property UserFavorite As Boolean
            Get
                Return CH_FAV.Checked
            End Get
        End Property
        Friend ReadOnly Property UserMediaOnly As Boolean
            Get
                Return CH_PARSE_USER_MEDIA.Checked
            End Get
        End Property
        Friend ReadOnly Property UserReady As Boolean
            Get
                Return CH_READY_FOR_DOWN.Checked
            End Get
        End Property
        Friend ReadOnly Property DownloadImages As Boolean
            Get
                Return CH_DOWN_IMAGES.Checked
            End Get
        End Property
        Friend ReadOnly Property DownloadVideos As Boolean
            Get
                Return CH_DOWN_VIDEOS.Checked
            End Get
        End Property
        Friend ReadOnly Property UserDescr As String
            Get
                Return TXT_DESCR.Text
            End Get
        End Property
        Friend ReadOnly Property UserFriendly As String
            Get
                Return TXT_USER_FRIENDLY.Text
            End Get
        End Property
        Friend ReadOnly Property ScriptUse As Boolean
            Get
                Return TXT_SCRIPT.Checked
            End Get
        End Property
        Friend ReadOnly Property ScriptData As String
            Get
                Return TXT_SCRIPT.Text
            End Get
        End Property
#End Region
#Region "Exchange, Path, Labels"
        Friend Property MyExchangeOptions As Object = Nothing
        Private ReadOnly _SpecPathPattern As RParams = RParams.DM("\w:\\.*", 0, EDP.ReturnValue)
        Private ReadOnly Property SpecialPath(s As SettingsHost) As SFile
            Get
                If TXT_SPEC_FOLDER.IsEmptyString Then
                    Return Nothing
                Else
                    If Not CStr(RegexReplace(TXT_SPEC_FOLDER.Text, _SpecPathPattern)).IsEmptyString Then
                        Return $"{TXT_SPEC_FOLDER.Text}\"
                    Else
                        Return $"{s.Path.PathWithSeparator}{TXT_SPEC_FOLDER.Text}\"
                    End If
                End If
            End Get
        End Property
        Friend ReadOnly Property UserLabels As List(Of String)
#End Region
#Region "Initializers"
        ''' <summary>Create new user</summary>
        Friend Sub New()
            InitializeComponent()
            UserLabels = New List(Of String)
            MyDef = New DefaultFormOptions(Me, Settings.Design)
        End Sub
        ''' <summary>Edit exist user</summary>
        Friend Sub New(_Instance As IUserData)
            Me.New
            If _Instance IsNot Nothing Then
                UserInstance = _Instance
                User = DirectCast(UserInstance, UserDataBase).User
                UserIsCollection = TypeOf UserInstance Is UserDataBind
                If UserIsCollection Then
                    With DirectCast(UserInstance, UserDataBind) : .CurrentlyEdited = True : CollectionName = .CollectionName : End With
                End If
            End If
        End Sub
#End Region
#Region "Form handlers"
        Private Class CollectionNameFieldProvider : Implements IFieldsCheckerProvider
            Private Property ErrorMessage As String Implements IFieldsCheckerProvider.ErrorMessage
            Private Property Name As String Implements IFieldsCheckerProvider.Name
            Private Property TypeError As Boolean Implements IFieldsCheckerProvider.TypeError
            Private Function Convert(Value As Object, DestinationType As Type, Provider As IFormatProvider,
                                     Optional NothingArg As Object = Nothing, Optional e As ErrorsDescriber = Nothing) As Object Implements ICustomProvider.Convert
                If ACheck(Value) Then
                    If Settings.Users.Exists(Function(u) u.IsCollection AndAlso u.CollectionName = CStr(Value) AndAlso
                                                         Not DirectCast(u, UserDataBind).CurrentlyEdited) Then
                        ErrorMessage = $"A collection named [{Value}] already exist"
                        Return Nothing
                    Else
                        Return Value
                    End If
                Else
                    Return Nothing
                End If
            End Function
            Private Function GetFormat(FormatType As Type) As Object Implements IFormatProvider.GetFormat
                Throw New NotImplementedException("[GetFormat] is not available in the 'CollectionNameFieldProvider'")
            End Function
        End Class
        Private Sub UserCreatorForm_Load(sender As Object, e As EventArgs) Handles Me.Load
            Try
                With MyDef
                    .MyViewInitialize(True)
                    .AddOkCancelToolbar()
                    CH_AUTO_DETECT_SITE.Enabled = False
                    With CMB_SITE
                        .BeginUpdate()
                        .Items.AddRange(Settings.Plugins.Select(Function(p) New ListItem({p.Key, p.Name})))
                        .EndUpdate(True)
                    End With

                    Dim NameFieldProvider As IFormatProvider = Nothing

                    If UserIsCollection Then
                        Icon = If(ImageRenderer.GetIcon(My.Resources.DBPic_32, EDP.ReturnValue), Icon)
                        Text = $"Collection: {UserInstance.CollectionName}"

                        TXT_USER.CaptionText = "Collection name"
                        TXT_USER.Text = UserInstance.CollectionName
                        TXT_USER.Buttons.AddRange({ADB.Refresh, ADB.Clear})
                        TXT_USER.Buttons.UpdateButtonsPositions()
                        TXT_SPEC_FOLDER.Buttons.Clear()
                        TXT_SPEC_FOLDER.TextBoxReadOnly = True
                        TXT_SPEC_FOLDER.Buttons.UpdateButtonsPositions()

                        With TP_MAIN
                            .Controls.Clear()
                            .RowStyles.Clear()
                            .RowCount = 0
                            With .RowStyles
                                .Add(New RowStyle(SizeType.Absolute, 28))
                                .Add(New RowStyle(SizeType.Absolute, 28))
                                .Add(New RowStyle(SizeType.Absolute, 28))
                                .Add(New RowStyle(SizeType.Absolute, 28))
                                .Add(New RowStyle(SizeType.Absolute, 28))
                                .Add(New RowStyle(SizeType.Absolute, 26))
                                .Add(New RowStyle(SizeType.Percent, 100))
                            End With
                            .RowCount = .RowStyles.Count
                            With .Controls
                                .Add(TXT_USER, 0, 0)
                                .Add(TXT_SPEC_FOLDER, 0, 1)
                                .Add(TP_TEMP_FAV, 0, 2)
                                .Add(TP_DOWN_IMG_VID, 0, 3)
                                .Add(TP_READY_USERMEDIA, 0, 4)
                                .Add(TXT_LABELS, 0, 5)
                                .Add(TXT_DESCR, 0, 6)
                            End With
                            .Refresh()
                            .Update()
                        End With

                        TXT_DESCR.TextBoxReadOnly = True
                        TXT_DESCR.Buttons.Clear()
                        TXT_DESCR.Buttons.UpdateButtonsPositions()

                        CH_TEMP.ThreeState = True
                        CH_FAV.ThreeState = True
                        CH_DOWN_IMAGES.ThreeState = True
                        CH_DOWN_VIDEOS.ThreeState = True
                        CH_READY_FOR_DOWN.ThreeState = True
                        CH_PARSE_USER_MEDIA.ThreeState = True

                        With DirectCast(UserInstance, UserDataBind)
                            Dim state As Func(Of Boolean, Func(Of IUserData, Boolean, Boolean), CheckState) =
                                Function(v, p) If(.All(Function(pp) p.Invoke(pp, v)), If(v, CheckState.Checked, CheckState.Unchecked), CheckState.Indeterminate)
                            TXT_SPEC_FOLDER.Text = DirectCast(.Item(0), UserDataBase).User.SpecialCollectionPath.ToString
                            CH_TEMP.CheckState = state(.Item(0).Temporary, Function(p, v) p.Temporary = v)
                            CH_FAV.CheckState = state(.Item(0).Favorite, Function(p, v) p.Favorite = v)
                            CH_DOWN_IMAGES.CheckState = state(.Item(0).DownloadImages, Function(p, v) p.DownloadImages = v)
                            CH_DOWN_VIDEOS.CheckState = state(.Item(0).DownloadVideos, Function(p, v) p.DownloadVideos = v)
                            CH_READY_FOR_DOWN.CheckState = state(.Item(0).ReadyForDownload, Function(p, v) p.ReadyForDownload = v)
                            CH_PARSE_USER_MEDIA.CheckState = state(.Item(0).ParseUserMediaOnly, Function(p, v) p.ParseUserMediaOnly = v)
                            TXT_DESCR.Text = .GetUserInformation.StringFormatLines
                            UserLabels.ListAddList(.Labels)
                            If UserLabels.ListExists Then TXT_LABELS.Text = UserLabels.ListToString
                        End With

                        NameFieldProvider = New CollectionNameFieldProvider
                    Else
                        If User.Name.IsEmptyString Then
                            CH_READY_FOR_DOWN.Checked = True
                            CH_TEMP.Checked = Settings.DefaultTemporary
                            CH_DOWN_IMAGES.Checked = Settings.DefaultDownloadImages
                            CH_DOWN_VIDEOS.Checked = Settings.DefaultDownloadVideos
                            TXT_SCRIPT.Checked = Settings.ScriptData.Attribute
                            SetParamsBySite()
                        Else
                            TP_ADD_BY_LIST.Enabled = False
                            TXT_USER.Text = User.Name
                            TXT_SPEC_FOLDER.Text = User.SpecialPath
                            Dim i% = Settings.Plugins.FindIndex(Function(p) p.Key = User.Plugin)
                            If i >= 0 Then CMB_SITE.SelectedIndex = i
                            SetParamsBySite()
                            CH_IS_CHANNEL.Enabled = False
                            CMB_SITE.Enabled = False
                            CH_IS_CHANNEL.Checked = User.IsChannel
                            If UserInstance IsNot Nothing Then
                                Text = $"User: {UserInstance.Name}"
                                If Not UserInstance.FriendlyName.IsEmptyString Then Text &= $" ({UserInstance.FriendlyName})"
                                TXT_USER.Enabled = False
                                TXT_SPEC_FOLDER.TextBoxReadOnly = True
                                TXT_SPEC_FOLDER.Buttons.Clear()
                                TXT_SPEC_FOLDER.Buttons.UpdateButtonsPositions()
                                With UserInstance
                                    TXT_USER_FRIENDLY.Text = .FriendlyName
                                    CH_FAV.Checked = .Favorite
                                    CH_TEMP.Checked = .Temporary
                                    CH_PARSE_USER_MEDIA.Checked = .ParseUserMediaOnly
                                    CH_READY_FOR_DOWN.Checked = .ReadyForDownload
                                    CH_DOWN_IMAGES.Checked = .DownloadImages
                                    CH_DOWN_VIDEOS.Checked = .DownloadVideos
                                    TXT_SCRIPT.Checked = .ScriptUse
                                    TXT_SCRIPT.Text = .ScriptData
                                    TXT_DESCR.Text = .Description.StringFormatLines
                                    UserLabels.ListAddList(.Labels)
                                    If UserLabels.ListExists Then TXT_LABELS.Text = UserLabels.ListToString
                                End With
                                CH_ADD_BY_LIST.Enabled = False
                            Else
                                CH_TEMP.Checked = Settings.DefaultTemporary
                                CH_READY_FOR_DOWN.Checked = Not Settings.DefaultTemporary
                                CH_DOWN_IMAGES.Checked = Settings.DefaultDownloadImages
                                CH_DOWN_VIDEOS.Checked = Settings.DefaultDownloadVideos
                            End If
                        End If
                    End If
                    .MyFieldsChecker = New FieldsChecker
                    .MyFieldsCheckerE.AddControl(Of String)(TXT_USER, TXT_USER.CaptionText,, NameFieldProvider)
                    .MyFieldsChecker.EndLoaderOperations()
                    .EndLoaderOperations()
                End With
            Catch ex As Exception
                MyDef.InvokeLoaderError(ex)
            End Try
        End Sub
        Private Sub UserCreatorForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            Dim b As Boolean = True
            Select Case e.KeyCode
                Case Keys.F4 : ChangeLabels()
                Case Keys.F2 : If BTT_OTHER_SETTINGS.Enabled Then BTT_OTHER_SETTINGS.PerformClick()
                Case Else : b = False
            End Select
            If b Then e.Handled = True
        End Sub
        Private Sub UserCreatorForm_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
            UserLabels.Clear()
            If UserIsCollection And UserInstance IsNot Nothing Then DirectCast(UserInstance, UserDataBind).CurrentlyEdited = False
        End Sub
#End Region
#Region "Ok, Cancel"
        Private Sub MyDef_ButtonOkClick(Sender As Object, e As KeyHandleEventArgs) Handles MyDef.ButtonOkClick
            If UserIsCollection Then
                If MyDef.MyFieldsChecker.AllParamsOK Then
                    With UserInstance
                        If Not CH_TEMP.CheckState = CheckState.Indeterminate Then .Temporary = CH_TEMP.Checked
                        If Not CH_FAV.CheckState = CheckState.Indeterminate Then .Favorite = CH_FAV.Checked
                        If Not CH_DOWN_IMAGES.CheckState = CheckState.Indeterminate Then .DownloadImages = CH_DOWN_IMAGES.Checked
                        If Not CH_DOWN_VIDEOS.CheckState = CheckState.Indeterminate Then .DownloadVideos = CH_DOWN_VIDEOS.Checked
                        If Not CH_READY_FOR_DOWN.CheckState = CheckState.Indeterminate Then .ReadyForDownload = CH_READY_FOR_DOWN.Checked
                        If Not CH_PARSE_USER_MEDIA.CheckState = CheckState.Indeterminate Then .ParseUserMediaOnly = CH_PARSE_USER_MEDIA.Checked
                        DirectCast(UserInstance, UserDataBind).Collections.ForEach(Sub(u) u.Labels.ListAddList(UserLabels, LAP.ClearBeforeAdd, LAP.NotContainsOnly))
                        CollectionName = TXT_USER.Text
                        .UpdateUserInformation()
                    End With
                    GoTo CloseForm
                End If
            Else
                If Not CH_ADD_BY_LIST.Checked Then
                    If MyDef.MyFieldsChecker.AllParamsOK Then
                        Dim s As SettingsHost = GetSiteByCheckers()
                        If s IsNot Nothing Then
                            Dim tmpUser As UserInfo = User.Clone
                            With tmpUser
                                .Name = TXT_USER.Text
                                .SpecialPath = SpecialPath(s)
                                .Site = s.Name
                                .Plugin = s.Key
                                .IsChannel = CH_IS_CHANNEL.Checked
                                .UpdateUserFile()
                            End With
                            User = tmpUser
                            Dim ScriptText$ = TXT_SCRIPT.Text
                            If Not ScriptText.IsEmptyString Then
                                Dim f As SFile = ScriptText
                                If Not SFile.IsDirectory(ScriptText) And UserInstance IsNot Nothing Then
                                    With DirectCast(UserInstance, UserDataBase) : f.Path = .MyFile.Path : End With
                                End If
                                TXT_SCRIPT.Text = f
                            End If
                            If UserInstance IsNot Nothing Then
                                With DirectCast(UserInstance, UserDataBase)
                                    .User = User
                                    .FriendlyName = TXT_USER_FRIENDLY.Text
                                    .Favorite = CH_FAV.Checked
                                    .Temporary = CH_TEMP.Checked
                                    .ReadyForDownload = CH_READY_FOR_DOWN.Checked
                                    .DownloadImages = CH_DOWN_IMAGES.Checked
                                    .DownloadVideos = CH_DOWN_VIDEOS.Checked
                                    .UserDescription = TXT_DESCR.Text
                                    If MyExchangeOptions IsNot Nothing Then .ExchangeOptionsSet(MyExchangeOptions)
                                    Dim l As New ListAddParams(LAP.NotContainsOnly + LAP.ClearBeforeAdd)
                                    If .IsCollection Then
                                        With DirectCast(UserInstance, API.UserDataBind)
                                            If .Count > 0 Then .Collections.ForEach(Sub(c) c.Labels.ListAddList(UserLabels, l))
                                        End With
                                    Else
                                        .Labels.ListAddList(UserLabels, LAP.NotContainsOnly, LAP.ClearBeforeAdd)
                                    End If
                                    .ParseUserMediaOnly = CH_PARSE_USER_MEDIA.Checked
                                    .ScriptUse = TXT_SCRIPT.Checked
                                    .ScriptData = TXT_SCRIPT.Text
                                    .UpdateUserInformation()
                                End With
                            End If
                            GoTo CloseForm
                        Else
                            MsgBoxE("User site not selected", MsgBoxStyle.Exclamation)
                        End If
                    End If
                Else
                    If CreateUsersByList() Then GoTo CloseForm
                End If
            End If
            Exit Sub
CloseForm:
            MyDef.CloseForm()
        End Sub
        Private Sub MyDef_ButtonCancelClick(Sender As Object, e As KeyHandleEventArgs) Handles MyDef.ButtonCancelClick
            MyDef.CloseForm(IIf(StartIndex >= 0, DialogResult.OK, DialogResult.Cancel))
        End Sub
#End Region
#Region "Controls handlers"
        Private _TextChangeInvoked As Boolean = False
        Private Sub TXT_USER_ActionOnTextChanged(Sender As Object, e As EventArgs) Handles TXT_USER.ActionOnTextChanged
            Try
                If Not _TextChangeInvoked And Not UserIsCollection Then
                    _TextChangeInvoked = True
                    If Not CH_ADD_BY_LIST.Checked Then
                        Dim s As ExchangeOptions = GetSiteByText(TXT_USER.Text)
                        Dim found As Boolean = False
                        If Not s.UserName.IsEmptyString Then
                            Dim i% = Settings.Plugins.FindIndex(Function(p) p.Key = s.HostKey)
                            If i >= 0 Then
                                CMB_SITE.SelectedIndex = i
                                CH_IS_CHANNEL.Checked = s.IsChannel
                                TXT_USER.Text = s.UserName
                                found = True
                            End If
                        End If
                        If Not found Then
                            CMB_SITE.SelectedIndex = -1
                            CMB_SITE.Clear(ComboBoxExtended.ClearMode.Text)
                            CH_IS_CHANNEL.Checked = False
                        End If
                    End If
                    _TextChangeInvoked = False
                End If
            Catch
            End Try
        End Sub
        Private Sub TXT_USER_ActionOnButtonClick(Sender As ActionButton, e As ActionButtonEventArgs) Handles TXT_USER.ActionOnButtonClick
            If UserIsCollection AndAlso Sender.DefaultButton = ADB.Refresh Then TXT_USER.Text = UserInstance.CollectionName
        End Sub
        Private Sub CMB_SITE_ActionSelectedItemChanged(Sender As Object, e As EventArgs, Item As ListViewItem) Handles CMB_SITE.ActionSelectedItemChanged
            CH_IS_CHANNEL.Checked = False
            MyExchangeOptions = Nothing
            SetParamsBySite()
        End Sub
        Private Sub BTT_OTHER_SETTINGS_Click(sender As Object, e As EventArgs) Handles BTT_OTHER_SETTINGS.Click
            Dim s As SettingsHost = GetSiteByCheckers()
            If s IsNot Nothing Then
                s.Source.UserOptions(MyExchangeOptions, True)
                MyDef.ChangesDetected = True
                MyDef.MyOkCancel.EnableOK = True
            End If
        End Sub
        Private Sub TXT_SPEC_FOLDER_ActionOnButtonClick(Sender As ActionButton, e As EventArgs) Handles TXT_SPEC_FOLDER.ActionOnButtonClick
            If Sender.DefaultButton = ADB.Open Then
                Dim f As SFile = Nothing
                If Not TXT_SPEC_FOLDER.Text.IsEmptyString Then f = $"{TXT_SPEC_FOLDER.Text}\"
                f = SFile.SelectPath(f)
                If Not f.IsEmptyString Then TXT_SPEC_FOLDER.Text = f.PathWithSeparator
            End If
        End Sub
        Private Sub CH_TEMP_CheckedChanged(sender As Object, e As EventArgs) Handles CH_TEMP.CheckedChanged
            If CH_TEMP.Checked Then CH_FAV.Checked = False : CH_READY_FOR_DOWN.Checked = False
        End Sub
        Private Sub CH_FAV_CheckedChanged(sender As Object, e As EventArgs) Handles CH_FAV.CheckedChanged
            If CH_FAV.Checked Then CH_TEMP.Checked = False
        End Sub
        Private Sub CH_ADD_BY_LIST_CheckedChanged(sender As Object, e As EventArgs) Handles CH_ADD_BY_LIST.CheckedChanged
            If CH_ADD_BY_LIST.Checked Then
                TXT_DESCR.GroupBoxText = "Users list"
                CH_AUTO_DETECT_SITE.Enabled = True
            Else
                TXT_DESCR.GroupBoxText = "Description"
                CH_AUTO_DETECT_SITE.Checked = False
                CH_AUTO_DETECT_SITE.Enabled = False
                SetParamsBySite()
            End If
            TXT_USER.Enabled = Not CH_ADD_BY_LIST.Checked
            TXT_USER_FRIENDLY.Enabled = Not CH_ADD_BY_LIST.Checked
        End Sub
        Private Sub CH_AUTO_DETECT_SITE_CheckedChanged(sender As Object, e As EventArgs) Handles CH_AUTO_DETECT_SITE.CheckedChanged
            CH_IS_CHANNEL.Enabled = Not CH_AUTO_DETECT_SITE.Checked
            CMB_SITE.Enabled = Not CH_AUTO_DETECT_SITE.Checked
            If CH_AUTO_DETECT_SITE.Checked Then
                BTT_OTHER_SETTINGS.Enabled = False
                MyExchangeOptions = Nothing
            Else
                BTT_OTHER_SETTINGS.Enabled = True
            End If
        End Sub
        Private Sub TXT_LABELS_ActionOnButtonClick(Sender As ActionButton, e As EventArgs) Handles TXT_LABELS.ActionOnButtonClick
            Select Case Sender.DefaultButton
                Case ADB.Open : ChangeLabels()
                Case ADB.Clear : UserLabels.Clear()
            End Select
        End Sub
        Private Sub TXT_SCRIPT_ActionOnButtonClick(Sender As ActionButton, e As EventArgs) Handles TXT_SCRIPT.ActionOnButtonClick
            SettingsCLS.ScriptTextBoxButtonClick(TXT_SCRIPT, Sender)
        End Sub
#End Region
#Region "Functions"
        Private Function GetSiteByCheckers() As SettingsHost
            Return If(CMB_SITE.SelectedIndex >= 0, Settings(CStr(CMB_SITE.Items(CMB_SITE.SelectedIndex).Value(0))), Nothing)
        End Function
        Private Function CreateUsersByList() As Boolean
            Try
                If CH_ADD_BY_LIST.Checked Then
                    If Not TXT_DESCR.IsEmptyString Then
                        Dim u As List(Of String) = TXT_DESCR.Text.StringToList(Of String, List(Of String))(vbNewLine).ListForEach(Function(s, ii) s.Trim,, False)
                        If u.ListExists Then
                            Dim NonIdentified As New List(Of String)
                            Dim UsersForCreate As New List(Of UserInfo)
                            Dim BannedUsers() As String = Nothing
                            Dim uu$
                            Dim ulabels As List(Of String) = ListAddList(Nothing, UserLabels).ListAddValue(LabelsKeeper.NoParsedUser, LAP.NotContainsOnly)
                            Dim tmpUser As UserInfo
                            Dim s As SettingsHost = GetSiteByCheckers()
                            Dim sObj As ExchangeOptions
                            Dim _IsChannel As Boolean = CH_IS_CHANNEL.Checked
                            Dim Added% = 0
                            Dim Skipped% = 0
                            Dim uid%
                            Dim sf As Func(Of SettingsHost, String) = Function(__s) SpecialPath(__s).PathWithSeparator
                            Dim __sf As Func(Of String, SettingsHost, SFile) = Function(Input, __s) IIf(sf(__s).IsEmptyString, Nothing, New SFile($"{sf(__s)}{Input}\"))

                            Settings.Labels.Add(LabelsKeeper.NoParsedUser)

                            For i% = 0 To u.Count - 1
                                uu = u(i)
                                If CH_AUTO_DETECT_SITE.Checked Then
                                    sObj = GetSiteByText(uu)
                                    If Not sObj.UserName.IsEmptyString Then
                                        s = Settings(sObj.HostKey)
                                        uu = sObj.UserName
                                    Else
                                        s = Nothing
                                    End If
                                End If

                                If s IsNot Nothing Then
                                    tmpUser = New UserInfo(uu, s) With {.SpecialPath = __sf(uu, s), .IsChannel = _IsChannel}
                                    tmpUser.UpdateUserFile()
                                    uid = -1
                                    If Settings.UsersList.Count > 0 Then uid = Settings.UsersList.IndexOf(tmpUser)
                                    If uid < 0 And Not UsersForCreate.Contains(tmpUser) Then
                                        UsersForCreate.Add(tmpUser)
                                    Else
                                        Skipped += 1
                                    End If
                                Else
                                    NonIdentified.Add(u(i))
                                End If
                            Next

                            If UsersForCreate.Count > 0 Then
                                BannedUsers = UserBanned(UsersForCreate.Select(Function(uuu) uuu.Name).ToArray)
                                If BannedUsers.ListExists Then UsersForCreate.RemoveAll(Function(uuu) BannedUsers.Contains(uuu.Name))
                                If UsersForCreate.Count > 0 Then
                                    For Each tmpUser In UsersForCreate
                                        Settings.UpdateUsersList(tmpUser)
                                        If StartIndex = -1 Then StartIndex = Settings.Users.Count
                                        Settings.Users.Add(UserDataBase.GetInstance(tmpUser, False))
                                        With Settings.Users.Last
                                            .FriendlyName = TXT_USER_FRIENDLY.Text
                                            .Favorite = CH_FAV.Checked
                                            .Temporary = CH_TEMP.Checked
                                            .ReadyForDownload = CH_READY_FOR_DOWN.Checked
                                            .DownloadImages = CH_DOWN_IMAGES.Checked
                                            .DownloadVideos = CH_DOWN_VIDEOS.Checked
                                            .ScriptUse = TXT_SCRIPT.Checked
                                            .Labels.ListAddList(ulabels)
                                            .ParseUserMediaOnly = CH_PARSE_USER_MEDIA.Checked
                                            If Not CH_AUTO_DETECT_SITE.Checked Then _
                                               DirectCast(.Self, UserDataBase).HOST.Source.UserOptions(MyExchangeOptions, False)
                                            DirectCast(.Self, UserDataBase).ExchangeOptionsSet(MyExchangeOptions)
                                            .UpdateUserInformation()
                                        End With
                                        Added += 1
                                    Next
                                End If
                            End If

                            Dim m As New MMessage($"Added {Added} users (skipped (already exists and/or duplicated) {Skipped})")
                            If BannedUsers.ListExists Or NonIdentified.Count > 0 Then
                                Dim t$ = String.Empty
                                If BannedUsers.ListExists Then t.StringAppendLine($"Banned users:{vbNewLine}{BannedUsers.ListToString(vbNewLine)}")
                                If NonIdentified.Count > 0 Then t.StringAppendLine($"Non-Identified users:{vbNewLine}{NonIdentified.ListToString(vbNewLine)}", vbNewLine.StringDup(2))
                                m.Style = MsgBoxStyle.Exclamation
                                m.Text.StringAppendLine("Some of users does not recognized and/or banned")
                                m.Text.StringAppendLine(t, vbNewLine.StringDup(2))
                                TXT_DESCR.Text = t
                            Else
                                TXT_DESCR.Clear()
                            End If

                            MsgBoxE(m)
                            If Added > 0 Then MyDef.ChangesDetected = False
                            Return Added > 0 And Not BannedUsers.ListExists And NonIdentified.Count = 0
                        Else
                            MsgBoxE("No user can be recognized", MsgBoxStyle.Exclamation)
                        End If
                    Else
                        MsgBoxE("[Users list] is empty", MsgBoxStyle.Critical)
                    End If
                End If
                Return False
            Catch ex As Exception
                Return ErrorsDescriber.Execute(EDP.LogMessageValue, ex, "Error when adding users by list", False)
            End Try
        End Function
        Private Function GetSiteByText(ByRef TXT As String) As ExchangeOptions
            Dim s As ExchangeOptions
            For Each p As PluginHost In Settings.Plugins
                s = p.Settings.IsMyUser(TXT)
                If Not s.UserName.IsEmptyString Then Return s
            Next
            Return Nothing
        End Function
        Private Sub SetParamsBySite()
            Dim s As SettingsHost = GetSiteByCheckers()
            If s IsNot Nothing Then
                With s
                    CH_TEMP.Checked = .Temporary
                    CH_DOWN_IMAGES.Checked = .DownloadImages
                    CH_DOWN_VIDEOS.Checked = .DownloadVideos
                    CH_PARSE_USER_MEDIA.Checked = .GetUserMediaOnly.Value
                    CH_READY_FOR_DOWN.Checked = Not CH_TEMP.Checked
                    If s.HasSpecialOptions Then
                        BTT_OTHER_SETTINGS.Enabled = True
                        If UserInstance Is Nothing Then
                            s.Source.UserOptions(MyExchangeOptions, False)
                        Else
                            MyExchangeOptions = DirectCast(UserInstance, UserDataBase).ExchangeOptionsGet
                        End If
                    Else
                        BTT_OTHER_SETTINGS.Enabled = False
                    End If
                End With
            Else
                BTT_OTHER_SETTINGS.Enabled = False
            End If
        End Sub
        Private Sub ChangeLabels()
            Using fl As New LabelsForm(UserLabels)
                fl.ShowDialog()
                If fl.DialogResult = DialogResult.OK Then
                    UserLabels.ListAddList(fl.LabelsList, LAP.NotContainsOnly, LAP.ClearBeforeAdd)
                    If UserLabels.ListExists Then
                        TXT_LABELS.Text = UserLabels.ListToString
                    Else
                        TXT_LABELS.Clear()
                    End If
                End If
            End Using
        End Sub
#End Region
    End Class
End Namespace