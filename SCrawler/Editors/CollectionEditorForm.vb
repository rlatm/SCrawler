﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports PersonalUtilities.Forms
Imports PersonalUtilities.Forms.Controls.Base
Namespace Editors
    Friend Class CollectionEditorForm
        Private WithEvents MyDefs As DefaultFormOptions
        Private ReadOnly Collections As List(Of String)
        Friend Property [Collection] As String = String.Empty
        Friend Sub New()
            InitializeComponent()
            MyDefs = New DefaultFormOptions(Me, Settings.Design)
            Collections = New List(Of String)
        End Sub
        Private Sub CollectionEditorForm_Load(sender As Object, e As EventArgs) Handles Me.Load
            Try
                With MyDefs
                    .MyViewInitialize()
                    .AddOkCancelToolbar()
                    Collections.ListAddList(Settings.LastCollections)
                    Dim ecol As List(Of String) = ListAddList(Nothing, (From c In Settings.Users Where c.IsCollection Select c.CollectionName), LAP.NotContainsOnly)
                    If ecol.ListExists Then ecol.Sort() : Collections.ListAddList(ecol, LAP.NotContainsOnly) : ecol.Clear()
                    If Collections.Count > 0 Then CMB_COLLECTIONS.Items.AddRange(Collections.Select(Function(c) New ListItem(c)))
                    If Not Collection.IsEmptyString And Collections.Contains(Collection) Then CMB_COLLECTIONS.SelectedIndex = Collections.IndexOf(Collection)
                    .DelegateClosingChecker = False
                    .EndLoaderOperations()
                End With
            Catch ex As Exception
                MyDefs.InvokeLoaderError(ex)
            End Try
        End Sub
        Private Sub CollectionEditorForm_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
            Collections.Clear()
        End Sub
        Private Sub CollectionEditorForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            If e.KeyCode = Keys.Insert Then AddNewCollection() : e.Handled = True Else e.Handled = False
        End Sub
        Private Sub MyDefs_ButtonOkClick() Handles MyDefs.ButtonOkClick
            If CMB_COLLECTIONS.SelectedIndex >= 0 Then
                Collection = CMB_COLLECTIONS.Value.ToString
                With Settings.LastCollections
                    If .Contains(Collection) Then .Remove(Collection)
                    If .Count = 0 Then .Add(Collection) Else .Insert(0, Collection)
                End With
                MyDefs.CloseForm()
            Else
                MsgBoxE("Collection not selected", MsgBoxStyle.Exclamation)
            End If
        End Sub
        Private Sub CMB_COLLECTIONS_ActionOnButtonClick(Sender As ActionButton, e As EventArgs) Handles CMB_COLLECTIONS.ActionOnButtonClick
            If Sender.DefaultButton = ActionButton.DefaultButtons.Add Then AddNewCollection()
        End Sub
        Private Sub CMB_COLLECTIONS_ActionOnListDoubleClick(Sender As Object, e As EventArgs, Item As ListViewItem) Handles CMB_COLLECTIONS.ActionOnListDoubleClick
            Item.Selected = True
            MyDefs_ButtonOkClick()
        End Sub
        Private Sub AddNewCollection()
            Dim c$ = InputBoxE("Enter new collection name:", "Collection name")
            If Not c.IsEmptyString Then
                If Not Collections.Contains(c) Then
                    Collections.Add(c)
                    CMB_COLLECTIONS.Items.Add(c)
                    CMB_COLLECTIONS.SelectedIndex = CMB_COLLECTIONS.Count - 1
                Else
                    Dim i% = Collections.IndexOf(c)
                    If i >= 0 Then CMB_COLLECTIONS.SelectedIndex = i
                End If
            End If
        End Sub
    End Class
End Namespace