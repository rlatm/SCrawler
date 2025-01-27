﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports PersonalUtilities.Functions.XML
Imports PersonalUtilities.Functions.XML.Base
Imports PersonalUtilities.Tools
Friend Class LabelsKeeper : Implements ICollection(Of String), IMyEnumerator(Of String), IDisposable
    Friend Event NewLabelAdded()
    Friend Const NoLabeledName As String = "No Label"
    Friend Const NoParsedUser As String = "No Parsed"
    Friend ReadOnly NoLabel As New ListViewGroup(NoLabeledName, NoLabeledName)
    Private ReadOnly LabelsList As List(Of String)
    Private ReadOnly LabelsFile As SFile = "Settings\Labels.txt"
    Friend ReadOnly NewLabels As List(Of String)
    Friend ReadOnly Property NewLabelsExists As Boolean
        Get
            Return NewLabels.Count > 0
        End Get
    End Property
    Friend ReadOnly Property Current As XMLValuesCollection(Of String)
    Friend ReadOnly Property Excluded As XMLValuesCollection(Of String)
    Friend ReadOnly Property ExcludedIgnore As XMLValue(Of Boolean)
    Friend Sub New(ByRef x As XmlFile)
        LabelsList = New List(Of String)
        NewLabels = New List(Of String)
        If LabelsFile.Exists Then LabelsList.ListAddList(IO.File.ReadAllLines(LabelsFile), LAP.NotContainsOnly)
        Current = New XMLValuesCollection(Of String)(XMLValueBase.ListModes.String, "LatestSelectedLabels", x) With {.ListAddParameters = LAP.NotContainsOnly}
        Excluded = New XMLValuesCollection(Of String)(XMLValueBase.ListModes.String, "LatestExcludedLabels", x) With {.ListAddParameters = LAP.NotContainsOnly}
        ExcludedIgnore = New XMLValue(Of Boolean)("LatestExcludedLabelsIgnore", False, x)
        Dim lp As New ListAddParams(LAP.NotContainsOnly + LAP.IgnoreICopier)
        If Current.Count > 0 Then LabelsList.ListAddList(Current, lp)
        If Excluded.Count > 0 Then LabelsList.ListAddList(Excluded, lp)
    End Sub
    Friend ReadOnly Property ToList As List(Of String)
        Get
            Return LabelsList
        End Get
    End Property
    Friend ReadOnly Property Count As Integer Implements ICollection(Of String).Count, IMyEnumerator(Of String).MyEnumeratorCount
        Get
            Return LabelsList.Count
        End Get
    End Property
    Default Friend ReadOnly Property Item(Index As Integer) As String Implements IMyEnumerator(Of String).MyEnumeratorObject
        Get
            Return LabelsList(Index)
        End Get
    End Property
    Friend Sub Clear() Implements ICollection(Of String).Clear
        LabelsList.Clear()
        NewLabels.Clear()
    End Sub
    Friend Sub Update(Optional Force As Boolean = False)
        If LabelsList.Count > 0 Then
            If NewLabelsExists Or Force Then
                If LabelsList.Contains(NoParsedUser) Then LabelsList.Remove(NoParsedUser)
                LabelsList.Sort()
                TextSaver.SaveTextToFile(LabelsList.ListToString(vbNewLine), LabelsFile, True, False, EDP.SendInLog)
                If NewLabels.Count > 0 Then NewLabels.Clear()
            End If
        Else
            LabelsFile.Delete(, Settings.DeleteMode, EDP.SendInLog)
        End If
    End Sub
    Friend Overloads Sub Add(_Item As String) Implements ICollection(Of String).Add
        Add(_Item, True)
    End Sub
    Friend Overloads Sub Add(_Item As String, UpdateMainFrame As Boolean)
        If Not _Item.IsEmptyString And Not LabelsList.Contains(_Item) Then
            LabelsList.Add(_Item)
            If Not NewLabels.Contains(_Item) Then NewLabels.Add(_Item)
            If UpdateMainFrame Then RaiseEvent NewLabelAdded()
        End If
    End Sub
    Friend Sub AddRange(_Items As IEnumerable(Of String), UpdateMainFrame As Boolean)
        If _Items.ListExists Then
            For Each i$ In _Items : Add(i, False) : Next
            If UpdateMainFrame Then RaiseEvent NewLabelAdded()
        End If
    End Sub
    Friend Function Contains(_Item As String) As Boolean Implements ICollection(Of String).Contains
        Return LabelsList.Contains(_Item)
    End Function
    Friend Function Remove(_Item As String) As Boolean Implements ICollection(Of String).Remove
        Return LabelsList.Remove(_Item)
    End Function
    Private ReadOnly Property IsReadOnly As Boolean = False Implements ICollection(Of String).IsReadOnly
    Private Sub CopyTo(_Array() As String, ArrayIndex As Integer) Implements ICollection(Of String).CopyTo
        LabelsList.CopyTo(_Array, ArrayIndex)
    End Sub
#Region "IEnumerable Support"
    Private Function GetEnumerator() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        Return New MyEnumerator(Of String)(Me)
    End Function
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function
#End Region
#Region "IDisposable Support"
    Private disposedValue As Boolean = False
    Protected Overridable Overloads Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then Clear() : Current.Dispose() : Excluded.Dispose()
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