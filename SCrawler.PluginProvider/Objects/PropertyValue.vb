' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Namespace Plugin

    Public Interface IPropertyValue

        ''' <summary>Event for internal exchange</summary>
        ''' <param name="Value">New value</param>
        Event ValueChanged(Value As Object)

        ''' <summary>Value type</summary>
        Property [Type] As Type

        ''' <summary>Property value</summary>
        Property Value As Object

    End Interface

    Public NotInheritable Class PropertyValue : Implements IPropertyValue
        Private _Value As Object

        ''' <inheritdoc cref="PropertyValue.New(Object, Type, ByRef IPropertyValue.ValueChangedEventHandler)"/>
        ''' <exception cref="ArgumentNullException"></exception>
        Public Sub New(InitValue As Object)
            _Value = InitValue
            If IsNothing(InitValue) Then
                Throw New ArgumentNullException(NameOf(InitValue), "InitValue cannot be null")
            Else
                [Type] = _Value.GetType
            End If
        End Sub

        ''' <inheritdoc cref="PropertyValue.New(Object, Type, ByRef IPropertyValue.ValueChangedEventHandler)"/>
        Public Sub New(InitValue As Object, T As Type)
            _Value = InitValue
            [Type] = T
        End Sub

        ''' <summary>New property value instance</summary>
        ''' <param name="InitValue">Initialization value</param>
        ''' <param name="T">Value type</param>
        ''' <param name="RFunction">CallBack function on value change</param>
        Public Sub New(InitValue As Object, T As Type, ByRef RFunction As IPropertyValue.ValueChangedEventHandler)
            Me.New(InitValue, T)
            OnChangeFunction = RFunction
        End Sub

        Public Event ValueChanged As IPropertyValue.ValueChangedEventHandler Implements IPropertyValue.ValueChanged

        Public Property [Type] As Type Implements IPropertyValue.Type
        Public Property OnChangeFunction As IPropertyValue.ValueChangedEventHandler

        Public Property Value As Object Implements IPropertyValue.Value
            Get
                Return _Value
            End Get
            Set(Value As Object)
                _Value = Value
                If OnChangeFunction IsNot Nothing Then OnChangeFunction.Invoke(Me.Value)
                RaiseEvent ValueChanged(_Value)
            End Set
        End Property

    End Class

End Namespace
