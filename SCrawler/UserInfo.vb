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
Imports SCrawler.API
Imports SCrawler.API.Base
Imports SCrawler.Plugin.Hosts
Imports DownOptions = SCrawler.Plugin.ISiteSettings.Download
Partial Friend Module MainMod
    Friend Structure UserInfo : Implements IComparable(Of UserInfo), IEquatable(Of UserInfo), ICloneable, IEContainerProvider
#Region "XML Names"
        Friend Const Name_UserNode As String = "User"
        Friend Const Name_Site As String = "Site"
        Friend Const Name_Plugin As String = "Plugin"
        Friend Const Name_Collection As String = "Collection"
        Friend Const Name_Model_User As String = "ModelUser"
        Friend Const Name_Model_Collection As String = "ModelCollection"
        Friend Const Name_Merged As String = "Merged"
        Friend Const Name_IsChannel As String = "IsChannel"
        Friend Const Name_SpecialPath As String = "SpecialPath"
        Friend Const Name_SpecialCollectionPath As String = "SpecialCollectionPath"
#End Region
#Region "Declarations"
        Friend Name As String
        Friend Site As String
        Friend Plugin As String
        Friend File As SFile
        Friend SpecialPath As SFile
        Friend SpecialCollectionPath As SFile
        Friend Merged As Boolean
        Friend ReadOnly Property IncludedInCollection As Boolean
            Get
                Return Not CollectionName.IsEmptyString
            End Get
        End Property
        Friend ReadOnly Property IsVirtual As Boolean
            Get
                Return CollectionModel = UsageModel.Virtual Or UserModel = UsageModel.Virtual
            End Get
        End Property
        Friend UserModel As UsageModel
        Friend CollectionName As String
        Friend CollectionModel As UsageModel
        Friend IsChannel As Boolean
        Friend [Protected] As Boolean
        Friend ReadOnly Property DownloadOption As DownOptions
            Get
                If IsChannel Then
                    Return DownOptions.Channel
                Else
                    Return DownOptions.Main
                End If
            End Get
        End Property
#End Region
#Region "Initializers"
        Friend Sub New(_Name As String, Host As SettingsHost)
            Name = _Name
            Site = Host.Name
            Plugin = Host.Key
            UpdateUserFile()
        End Sub
        Private Sub New(x As EContainer)
            Name = x.Value
            Site = x.Attribute(Name_Site).Value
            Plugin = x.Attribute(Name_Plugin).Value
            CollectionName = x.Attribute(Name_Collection).Value
            CollectionModel = x.Attribute(Name_Model_Collection).Value.FromXML(Of Integer)(UsageModel.Default)
            UserModel = x.Attribute(Name_Model_User).Value.FromXML(Of Integer)(UsageModel.Default)
            Merged = x.Attribute(Name_Merged).Value.FromXML(Of Boolean)(False)
            SpecialPath = SFile.GetPath(x.Attribute(Name_SpecialPath).Value)
            SpecialCollectionPath = SFile.GetPath(x.Attribute(Name_SpecialCollectionPath).Value)
            IsChannel = x.Attribute(Name_IsChannel).Value.FromXML(Of Boolean)(False)
        End Sub
        Friend Sub New(c As Reddit.Channel)
            Name = c.Name
            Site = Reddit.RedditSite
            Plugin = Reddit.RedditSiteKey
            File = c.File
            IsChannel = True
        End Sub
        Public Shared Widening Operator CType(x As EContainer) As UserInfo
            Return New UserInfo(x)
        End Operator
        Public Shared Widening Operator CType(u As UserInfo) As String
            Return u.Name
        End Operator
#End Region
#Region "Operators"
        Public Shared Operator =(x As UserInfo, y As UserInfo)
            Return x.Equals(y)
        End Operator
        Public Shared Operator <>(x As UserInfo, y As UserInfo)
            Return Not x.Equals(y)
        End Operator
#End Region
#Region "ToString"
        Public Overrides Function ToString() As String
            Return Name
        End Function
#End Region
#Region "FilePath"
        Friend Sub UpdateUserFile()
            File = New SFile With {
                .Separator = "\",
                .Path = GetFilePathByParams(),
                .Extension = "xml",
                .Name = $"{UserDataBase.UserFileAppender}_{Site}_{Name}"
            }
        End Sub
        Private Function GetFilePathByParams() As String
            If [Protected] Then Return String.Empty
            Dim ColPath$ = If(SpecialCollectionPath.IsEmptyString, Settings.CollectionsPathF, SpecialCollectionPath).PathNoSeparator
            If SpecialCollectionPath.IsEmptyString Then ColPath &= $"\{CollectionName}"
            If Not SpecialPath.IsEmptyString Then
                Return $"{SpecialPath.PathWithSeparator}{SettingsFolderName}"
            ElseIf Merged And IncludedInCollection Then
                Return $"{ColPath}\{SettingsFolderName}"
            Else
                If IncludedInCollection And Not IsVirtual Then
                    Return $"{ColPath}\{Site}_{Name}\{SettingsFolderName}"
                ElseIf Settings(Plugin) IsNot Nothing Then
                    Return $"{Settings(Plugin).Path.PathNoSeparator}\{Name}\{SettingsFolderName}"
                Else
                    Dim s$ = Site.ToLower
                    Dim i% = Settings.Plugins.FindIndex(Function(p) p.Name.ToLower = s)
                    If i >= 0 Then Return $"{Settings.Plugins(i).Settings.Path.PathNoSeparator}\{Name}\{SettingsFolderName}" Else Return String.Empty
                End If
            End If
        End Function
#End Region
#Region "ToEContainer Support"
        Friend Function ToEContainer(Optional e As ErrorsDescriber = Nothing) As EContainer Implements IEContainerProvider.ToEContainer
            Return New EContainer(Name_UserNode, Name, {New EAttribute(Name_Site, Site),
                                                        New EAttribute(Name_Plugin, Plugin),
                                                        New EAttribute(Name_Collection, CollectionName),
                                                        New EAttribute(Name_Model_User, CInt(UserModel)),
                                                        New EAttribute(Name_Model_Collection, CInt(CollectionModel)),
                                                        New EAttribute(Name_Merged, Merged.BoolToInteger),
                                                        New EAttribute(Name_IsChannel, IsChannel.BoolToInteger),
                                                        New EAttribute(Name_SpecialPath, SpecialPath.PathWithSeparator),
                                                        New EAttribute(Name_SpecialCollectionPath, SpecialCollectionPath.PathWithSeparator)})
        End Function
#End Region
#Region "IComparable Support"
        Friend Function CompareTo(Other As UserInfo) As Integer Implements IComparable(Of UserInfo).CompareTo
            If Site = Other.Site Then
                Return Name.CompareTo(Other.Name)
            Else
                Return Site.CompareTo(Other.Site)
            End If
        End Function
#End Region
#Region "IEquatable Support"
        Friend Overloads Function Equals(Other As UserInfo) As Boolean Implements IEquatable(Of UserInfo).Equals
            Return Site.StringToLower = Other.Site.StringToLower And Name.StringToLower = Other.Name.StringToLower
        End Function
        Public Overloads Overrides Function Equals(Obj As Object) As Boolean
            Return Equals(DirectCast(Obj, UserInfo))
        End Function
#End Region
#Region "ICloneable Support"
        Friend Function Clone() As Object Implements ICloneable.Clone
            Return New UserInfo With {
                .Name = Name,
                .Site = Site,
                .Plugin = Plugin,
                .File = File,
                .SpecialPath = SpecialPath,
                .Merged = Merged,
                .CollectionName = CollectionName,
                .CollectionModel = CollectionModel,
                .UserModel = UserModel,
                .IsChannel = IsChannel,
                .[Protected] = [Protected]
            }
        End Function
#End Region
    End Structure
End Module