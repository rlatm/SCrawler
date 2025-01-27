﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.ComponentModel
Imports SCrawler.API.Base
Imports PersonalUtilities.Forms
Namespace DownloadObjects
    Friend Class DownloadedInfoForm
#Region "Events"
        Friend Event UserFind(Key As String)
#End Region
#Region "Declarations"
        Private MyView As FormView
        Private ReadOnly LParams As New ListAddParams(LAP.IgnoreICopier) With {.Error = EDP.None}
        Private Opened As Boolean = False
        Friend ReadOnly Property ReadyToOpen As Boolean
            Get
                Return Settings.DownloadOpenInfo And (Not Opened Or Settings.DownloadOpenInfo.Attribute) And Not Visible
            End Get
        End Property
        Friend Enum ViewModes As Integer
            Session = 0
            All = 1
        End Enum
        Friend Property ViewMode As ViewModes
            Get
                Return IIf(MENU_VIEW_ALL.Checked, ViewModes.All, ViewModes.Session)
            End Get
            Set(SMode As ViewModes)
                Settings.InfoViewMode.Value = CInt(SMode)
            End Set
        End Property
        Private ReadOnly _TempUsersList As List(Of IUserData)
#End Region
#Region "Initializer"
        Public Sub New()
            InitializeComponent()
            _TempUsersList = New List(Of IUserData)
            If Settings.InfoViewMode.Value = CInt(ViewModes.All) Then
                MENU_VIEW_SESSION.Checked = False
                MENU_VIEW_ALL.Checked = True
            Else
                MENU_VIEW_SESSION.Checked = True
                MENU_VIEW_ALL.Checked = False
            End If
            Settings.InfoViewMode.Value = ViewMode
            RefillList()
        End Sub
#End Region
#Region "Form handlers"
        Private Sub DownloadedInfoForm_Load(sender As Object, e As EventArgs) Handles Me.Load
            Try
                If MyView Is Nothing Then
                    MyView = New FormView(Me)
                    MyView.Import(Settings.Design)
                    MyView.SetFormSize()
                End If
                BTT_CLEAR.Visible = ViewMode = ViewModes.Session
                RefillList()
            Catch
            Finally
                Opened = True
            End Try
        End Sub
        Private Sub DownloadedInfoForm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
            e.Cancel = True
            Hide()
        End Sub
        Private Sub DownloadedInfoForm_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
            If MyView IsNot Nothing Then MyView.Dispose(Settings.Design)
            _TempUsersList.Clear()
        End Sub
        Private Sub DownloadedInfoForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            Dim b As Boolean = True
            Select Case e.KeyCode
                Case Keys.F5 : RefillList()
                Case Keys.F2 : UpdateNavigationButtons(1)
                Case Keys.F3 : UpdateNavigationButtons(-1)
                Case Keys.F4 : BTT_FIND.PerformClick()
                Case Keys.Enter : LIST_DOWN_MouseDoubleClick(Nothing, Nothing)
                Case Else : b = False
            End Select
            If b Then e.Handled = True
        End Sub
#End Region
#Region "Refill"
        Private Class UsersDateOrder : Implements IComparer(Of IUserData)
            Friend Function Compare(x As IUserData, y As IUserData) As Integer Implements IComparer(Of IUserData).Compare
                Dim xv& = If(DirectCast(x, UserDataBase).LastUpdated.HasValue, DirectCast(x, UserDataBase).LastUpdated.Value.Ticks, 0)
                Dim yv& = If(DirectCast(y, UserDataBase).LastUpdated.HasValue, DirectCast(y, UserDataBase).LastUpdated.Value.Ticks, 0)
                Return xv.CompareTo(yv) * -1
            End Function
        End Class
        Private Sub RefillList() Handles BTT_REFRESH.Click
            Try
                _TempUsersList.Clear()
                Dim lClear As Action = Sub() LIST_DOWN.Items.Clear()
                If LIST_DOWN.InvokeRequired Then LIST_DOWN.Invoke(lClear) Else lClear.Invoke
                If ViewMode = ViewModes.Session Then
                    _TempUsersList.ListAddList(Downloader.Downloaded, LParams)
                Else
                    _TempUsersList.ListAddList(Settings.Users.SelectMany(Of IUserData) _
                                              (Function(u) If(u.IsCollection, DirectCast(u, API.UserDataBind).Collections, {u})), LParams)
                End If
                If _TempUsersList.Count > 0 Then
                    _TempUsersList.Sort(New UsersDateOrder)
                    For Each user As IUserData In _TempUsersList
                        If LIST_DOWN.InvokeRequired Then
                            LIST_DOWN.Invoke(Sub() LIST_DOWN.Items.Add(user.DownloadedInformation))
                        Else
                            LIST_DOWN.Items.Add(user.DownloadedInformation)
                        End If
                    Next
                    If _LatestSelected >= 0 AndAlso _LatestSelected <= LIST_DOWN.Items.Count - 1 Then
                        Dim aSel As Action = Sub() LIST_DOWN.SelectedIndex = _LatestSelected
                        If LIST_DOWN.InvokeRequired Then LIST_DOWN.Invoke(aSel) Else aSel.Invoke
                    Else
                        _LatestSelected = -1
                    End If
                Else
                    _LatestSelected = -1
                End If
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.SendInLog, ex, "[DownloadedInfoForm.RefillList]")
            Finally
                UpdateNavigationButtons(Nothing)
            End Try
        End Sub
#End Region
#Region "Toolbar controls"
        Private Sub MENU_VIEW_SESSION_Click(sender As Object, e As EventArgs) Handles MENU_VIEW_SESSION.Click
            MENU_VIEW_SESSION.Checked = True
            MENU_VIEW_ALL.Checked = False
            ViewMode = ViewModes.Session
            BTT_CLEAR.Visible = True
            RefillList()
        End Sub
        Private Sub MENU_VIEW_ALL_Click(sender As Object, e As EventArgs) Handles MENU_VIEW_ALL.Click
            MENU_VIEW_SESSION.Checked = False
            MENU_VIEW_ALL.Checked = True
            ViewMode = ViewModes.All
            BTT_CLEAR.Visible = False
            RefillList()
        End Sub
        Private Sub BTT_FIND_Click(sender As Object, e As EventArgs) Handles BTT_FIND.Click
            Try
                If _LatestSelected.ValueBetween(0, LIST_DOWN.Items.Count - 1) AndAlso _LatestSelected.ValueBetween(0, Downloader.Downloaded.Count - 1) Then
                    Dim u As IUserData = Settings.GetUser(_TempUsersList(_LatestSelected), True)
                    If u IsNot Nothing Then RaiseEvent UserFind(u.Key)
                End If
            Catch ex As Exception
            End Try
        End Sub
        Private Sub BTT_CLEAR_Click(sender As Object, e As EventArgs) Handles BTT_CLEAR.Click
            If LIST_DOWN.Items.Count > 0 Then
                Downloader.Downloaded.Clear()
                RefillList()
            End If
        End Sub
#End Region
#Region "List handlers"
        Private _LatestSelected As Integer = -1
        Private Sub LIST_DOWN_SelectedIndexChanged(sender As Object, e As EventArgs) Handles LIST_DOWN.SelectedIndexChanged
            _LatestSelected = LIST_DOWN.SelectedIndex
            UpdateNavigationButtons(Nothing)
        End Sub
        Private Sub LIST_DOWN_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles LIST_DOWN.MouseDoubleClick
            Try
                If _LatestSelected.ValueBetween(0, _TempUsersList.Count - 1) AndAlso
                   Not DirectCast(_TempUsersList(_LatestSelected), UserDataBase).Disposed Then _TempUsersList(_LatestSelected).OpenFolder()
            Catch
            End Try
        End Sub
#End Region
#Region "Navigation"
        Private Sub BTT_UP_Click(sender As Object, e As EventArgs) Handles BTT_UP.Click
            UpdateNavigationButtons(-1)
        End Sub
        Private Sub BTT_DOWN_Click(sender As Object, e As EventArgs) Handles BTT_DOWN.Click
            UpdateNavigationButtons(1)
        End Sub
        Private Sub UpdateNavigationButtons(Offset As Integer?)
            Dim u As Boolean = False, d As Boolean = False
            With LIST_DOWN
                If .Items.Count > 0 Then
                    u = _LatestSelected > 0
                    d = _LatestSelected < .Items.Count - 1
                End If
                Dim a As Action = Sub()
                                      BTT_UP.Enabled = u
                                      BTT_DOWN.Enabled = d
                                  End Sub
                If ToolbarTOP.InvokeRequired Then ToolbarTOP.Invoke(a) Else a.Invoke
                a = Nothing
                If Offset.HasValue AndAlso .Items.Count > 0 AndAlso
                   (_LatestSelected + Offset.Value).ValueBetween(0, .Items.Count - 1) Then a = Sub() .SelectedIndex = _LatestSelected + Offset.Value
                If a IsNot Nothing Then
                    If LIST_DOWN.InvokeRequired Then LIST_DOWN.Invoke(a) Else a.Invoke
                End If
            End With
        End Sub
#End Region
#Region "Downloader handlers"
        Friend Sub Downloader_DownloadCountChange()
            If ViewMode = ViewModes.Session Then RefillList()
        End Sub
#End Region
    End Class
End Namespace