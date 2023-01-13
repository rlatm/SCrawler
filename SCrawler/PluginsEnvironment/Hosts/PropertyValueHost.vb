' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Imports System.Reflection
Imports SCrawler.Plugin.Attributes
Imports PersonalUtilities.Functions.XML.Base
Imports PersonalUtilities.Forms
Imports PersonalUtilities.Forms.Controls
Imports PersonalUtilities.Forms.Controls.Base
Imports ADB = PersonalUtilities.Forms.Controls.Base.ActionButton.DefaultButtons
Namespace Plugin.Hosts
    Friend Class PropertyValueHost : Implements IPropertyValue, IComparable(Of PropertyValueHost)
        Friend Const LeftOffsetDefault As Integer = 100
        Friend Event OnPropertyUpdateRequested(Sender As PropertyValueHost)
        Private Event ValueChanged As IPropertyValue.ValueChangedEventHandler Implements IPropertyValue.ValueChanged
        Private _Type As Type
        Friend Property [Type] As Type Implements IPropertyValue.Type
            Get
                Return If(ExternalValue?.Type, _Type)
            End Get
            Set(t As Type)
                _Type = t
            End Set
        End Property
#Region "Control"
        Friend Property Control As Control
        Private ReadOnly ControlNumber As Integer = -1
        Friend ReadOnly Property ControlHeight As Integer
            Get
                If Control IsNot Nothing Then
                    Return IIf(TypeOf Control Is CheckBox, 25, 28)
                Else
                    Return 0
                End If
            End Get
        End Property
        Friend Sub CreateControl(Optional TT As ToolTip = Nothing)
            With Options
                If .IsInformationLabel Then
                    Control = New Label
                    With DirectCast(Control, Label)
                        .Padding = New PaddingE(Control.Padding) With {.Left = LeftOffset}
                        .Text = CStr(AConvert(Of String)(Value, String.Empty))
                        .TextAlign = Options.LabelTextAlign
                    End With
                    If Not .ControlToolTip.IsEmptyString And TT IsNot Nothing Then TT.SetToolTip(Control, .ControlToolTip)
                Else
                    If Type Is GetType(Boolean) Or .ThreeStates Then
                        Control = New CheckBox
                        If Not .ControlToolTip.IsEmptyString And TT IsNot Nothing Then TT.SetToolTip(Control, .ControlToolTip)
                        DirectCast(Control, CheckBox).ThreeState = .ThreeStates
                        DirectCast(Control, CheckBox).Text = .ControlText
                        If .ThreeStates Then
                            DirectCast(Control, CheckBox).CheckState = CInt(AConvert(Of Integer)(Value, CInt(CheckState.Indeterminate)))
                        Else
                            DirectCast(Control, CheckBox).Checked = CBool(AConvert(Of Boolean)(Value, False))
                        End If
                        Control.Padding = New PaddingE(Control.Padding) With {.Left = LeftOffset}
                    Else
                        Control = New TextBoxExtended
                        With DirectCast(Control, TextBoxExtended)
                            .CaptionText = Options.ControlText
                            .CaptionToolTipEnabled = Not Options.ControlToolTip.IsEmptyString
                            .CaptionWidth = LeftOffset
                            If Not Options.ControlToolTip.IsEmptyString Then .CaptionToolTipText = Options.ControlToolTip : .CaptionToolTipEnabled = True
                            .Text = CStr(AConvert(Of String)(Value, String.Empty))
                            With .Buttons
                                .BeginInit()
                                If Source IsNot Nothing And UpdateMethod IsNot Nothing Then .Add(New ActionButton(ADB.Refresh))
                                .Add(ADB.Clear)
                                .EndInit(True)
                            End With
                            AddHandler .ActionOnButtonClick, AddressOf TextBoxClick
                        End With
                    End If
                End If
                Control.Tag = Name
                Control.Dock = DockStyle.Fill
            End With
        End Sub
        Friend Sub DisposeControl()
            If Control IsNot Nothing Then Control.Dispose() : Control = Nothing
        End Sub
        Private Sub TextBoxClick(Sender As ActionButton, e As EventArgs)
            Try
                If Sender.DefaultButton = ADB.Refresh AndAlso Source IsNot Nothing AndAlso UpdateMethod IsNot Nothing Then
                    If CBool(UpdateMethod.Invoke(Source, Nothing)) Then
                        RaiseEvent OnPropertyUpdateRequested(Me)
                        DirectCast(Control, TextBoxExtended).Text = CStr(AConvert(Of String)(Value, String.Empty))
                    End If
                End If
            Catch ex As Exception
                ErrorsDescriber.Execute(EDP.LogMessageValue, ex, $"Updating [{Name}] property")
            End Try
        End Sub
        Friend Sub UpdateValueByControl()
            If Control IsNot Nothing AndAlso TypeOf Control IsNot Label Then
                If TypeOf Control Is CheckBox Then
                    With DirectCast(Control, CheckBox)
                        If Options.ThreeStates Then Value = CInt(.CheckState) Else Value = .Checked
                    End With
                Else
                    Value = AConvert(DirectCast(Control, TextBoxExtended).Text, AModes.Var, [Type],,,, ProviderValue)
                End If
            End If
        End Sub
        Friend Function GetControlValue() As Object
            If Control IsNot Nothing Then
                If TypeOf Control Is CheckBox Then
                    With DirectCast(Control, CheckBox)
                        If Options.ThreeStates Then Return CInt(.CheckState) Else Return .Checked
                    End With
                Else
                    Return AConvert(DirectCast(Control, TextBoxExtended).Text, AModes.Var, [Type],,,, ProviderValue)
                End If
            Else
                Return Nothing
            End If
        End Function
#End Region
#Region "Compatibility"
        Private ReadOnly Source As Object
        Friend ReadOnly Name As String
        Private ReadOnly _XmlName As String
        Friend ReadOnly Options As PropertyOption
        Private _LeftOffset As Integer? = Nothing
        Friend Property LeftOffset As Integer
            Get
                If _LeftOffset.HasValue Then
                    Return _LeftOffset
                Else
                    Return If(Options?.LeftOffset, LeftOffsetDefault)
                End If
            End Get
            Set(NewOffset As Integer)
                _LeftOffset = NewOffset
            End Set
        End Property
#Region "Providers"
        Friend Property ProviderFieldsChecker As IFormatProvider
        Friend Property ProviderValue As IFormatProvider
        Friend Sub SetProvider(Provider As IFormatProvider, FC As Boolean)
            If FC Then ProviderFieldsChecker = Provider Else ProviderValue = Provider
        End Sub
#End Region
        Friend PropertiesChecking As String()
        Friend PropertiesCheckingMethod As MethodInfo
        Private UpdateMethod As MethodInfo
        Private _UpdateDependencies As String() = Nothing
        Friend ReadOnly Property UpdateDependencies As String()
            Get
                Return _UpdateDependencies
            End Get
        End Property
        Friend Sub SetUpdateMethod(m As MethodInfo, Dependencies As String())
            UpdateMethod = m
            _UpdateDependencies = Dependencies
        End Sub
        Friend ReadOnly IsTaskCounter As Boolean
#End Region
        Friend ReadOnly Exists As Boolean = False
#Region "Initializer"
        Friend Sub New(ByRef PropertySource As Object, Member As MemberInfo)
            Source = PropertySource
            Name = Member.Name

            ControlNumber = If(Member.GetCustomAttribute(Of ControlNumber)()?.PropertyNumber, -1)

            If DirectCast(Member, PropertyInfo).PropertyType Is GetType(PropertyValue) Then
                ExternalValue = DirectCast(DirectCast(Member, PropertyInfo).GetValue(Source), PropertyValue)
                _Value = ExternalValue.Value
                AddHandler ExternalValue.ValueChanged, AddressOf ExternalValueChanged
                Options = Member.GetCustomAttribute(Of PropertyOption)()
                IsTaskCounter = Member.GetCustomAttribute(Of TaskCounter)() IsNot Nothing
                _XmlName = If(Member.GetCustomAttribute(Of PXML)()?.ElementName, String.Empty)
                If Not _XmlName.IsEmptyString Then XValue = XMLValueBase.CreateInstance([Type])
                Exists = True
            End If
        End Sub
        Friend Sub SetXmlEnvironment(ByRef Container As Object, Optional _Nodes() As String = Nothing,
                                     Optional FormatProvider As IFormatProvider = Nothing)
            If Not _XmlName.IsEmptyString And XValue IsNot Nothing Then
                XValue.SetEnvironment(_XmlName, _Value, Container, _Nodes, If(ProviderValue, FormatProvider))
                Value(False) = XValue.Value
            End If
        End Sub
#End Region
#Region "Value"
        Private ReadOnly Property ExternalValue As PropertyValue
        Friend ReadOnly Property XValue As IXMLValue
        Private _Value As Object
        Friend Overloads Property Value As Object Implements IPropertyValue.Value
            Get
                Return _Value
            End Get
            Set(NewValue As Object)
                Value(True) = NewValue
            End Set
        End Property
        Private Overloads WriteOnly Property Value(UpdateXML As Boolean) As Object
            Set(NewValue As Object)
                _Value = NewValue
                If ExternalValue IsNot Nothing And Not _ExternalInvoked Then ExternalValue.Value = _Value
                If UpdateXML And XValue IsNot Nothing Then XValue.Value = NewValue
                RaiseEvent ValueChanged(_Value)
            End Set
        End Property
        Private _ExternalInvoked As Boolean = False
        Private Sub ExternalValueChanged(NewValue As Object)
            If Not _ExternalInvoked Then
                _ExternalInvoked = True
                Value = NewValue
                _ExternalInvoked = False
            End If
        End Sub
#End Region
#Region "IComparable Support"
        Private Function CompareTo(Other As PropertyValueHost) As Integer Implements IComparable(Of PropertyValueHost).CompareTo
            Return ControlNumber.CompareTo(Other.ControlNumber)
        End Function
#End Region
    End Class
End Namespace