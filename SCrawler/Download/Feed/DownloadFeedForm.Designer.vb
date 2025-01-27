﻿' Copyright (C) 2023  Andy https://github.com/AAndyProgram
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY
Namespace DownloadObjects
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Friend Class DownloadFeedForm : Inherits System.Windows.Forms.Form
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub
        Private components As System.ComponentModel.IContainer
        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Dim SEP_1 As System.Windows.Forms.ToolStripSeparator
            Me.ToolbarTOP = New System.Windows.Forms.ToolStrip()
            Me.MENU_LOAD_SESSION = New System.Windows.Forms.ToolStripDropDownButton()
            Me.BTT_LOAD_SESSION_LAST = New System.Windows.Forms.ToolStripMenuItem()
            Me.BTT_LOAD_SESSION_CHOOSE = New System.Windows.Forms.ToolStripMenuItem()
            Me.SEP_0 = New System.Windows.Forms.ToolStripSeparator()
            Me.BTT_REFRESH = New System.Windows.Forms.ToolStripButton()
            Me.BTT_CLEAR = New System.Windows.Forms.ToolStripButton()
            Me.TP_DATA = New System.Windows.Forms.TableLayoutPanel()
            SEP_1 = New System.Windows.Forms.ToolStripSeparator()
            Me.ToolbarTOP.SuspendLayout()
            Me.SuspendLayout()
            '
            'SEP_1
            '
            SEP_1.Name = "SEP_1"
            SEP_1.Size = New System.Drawing.Size(6, 25)
            '
            'ToolbarTOP
            '
            Me.ToolbarTOP.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden
            Me.ToolbarTOP.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.MENU_LOAD_SESSION, Me.SEP_0, Me.BTT_REFRESH, Me.BTT_CLEAR, SEP_1})
            Me.ToolbarTOP.Location = New System.Drawing.Point(0, 0)
            Me.ToolbarTOP.Name = "ToolbarTOP"
            Me.ToolbarTOP.Size = New System.Drawing.Size(484, 25)
            Me.ToolbarTOP.TabIndex = 0
            '
            'MENU_LOAD_SESSION
            '
            Me.MENU_LOAD_SESSION.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image
            Me.MENU_LOAD_SESSION.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.BTT_LOAD_SESSION_LAST, Me.BTT_LOAD_SESSION_CHOOSE})
            Me.MENU_LOAD_SESSION.Image = Global.SCrawler.My.Resources.Resources.ArrowDownPic_Blue_24
            Me.MENU_LOAD_SESSION.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.MENU_LOAD_SESSION.Name = "MENU_LOAD_SESSION"
            Me.MENU_LOAD_SESSION.Size = New System.Drawing.Size(29, 22)
            Me.MENU_LOAD_SESSION.Text = "Load session"
            '
            'BTT_LOAD_SESSION_LAST
            '
            Me.BTT_LOAD_SESSION_LAST.Image = Global.SCrawler.My.Resources.Resources.ArrowDownPic_Blue_24
            Me.BTT_LOAD_SESSION_LAST.Name = "BTT_LOAD_SESSION_LAST"
            Me.BTT_LOAD_SESSION_LAST.Size = New System.Drawing.Size(189, 22)
            Me.BTT_LOAD_SESSION_LAST.Text = "Load last session"
            '
            'BTT_LOAD_SESSION_CHOOSE
            '
            Me.BTT_LOAD_SESSION_CHOOSE.Image = Global.SCrawler.My.Resources.Resources.ArrowDownPic_Blue_24
            Me.BTT_LOAD_SESSION_CHOOSE.Name = "BTT_LOAD_SESSION_CHOOSE"
            Me.BTT_LOAD_SESSION_CHOOSE.Size = New System.Drawing.Size(189, 22)
            Me.BTT_LOAD_SESSION_CHOOSE.Text = "Select loading session"
            '
            'SEP_0
            '
            Me.SEP_0.Name = "SEP_0"
            Me.SEP_0.Size = New System.Drawing.Size(6, 25)
            '
            'BTT_REFRESH
            '
            Me.BTT_REFRESH.Image = Global.SCrawler.My.Resources.Resources.RefreshPic_24
            Me.BTT_REFRESH.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.BTT_REFRESH.Name = "BTT_REFRESH"
            Me.BTT_REFRESH.Size = New System.Drawing.Size(66, 22)
            Me.BTT_REFRESH.Text = "Refresh"
            Me.BTT_REFRESH.ToolTipText = "Refresh data list"
            '
            'BTT_CLEAR
            '
            Me.BTT_CLEAR.Image = Global.SCrawler.My.Resources.Resources.DeletePic_24
            Me.BTT_CLEAR.ImageTransparentColor = System.Drawing.Color.Magenta
            Me.BTT_CLEAR.Name = "BTT_CLEAR"
            Me.BTT_CLEAR.Size = New System.Drawing.Size(54, 22)
            Me.BTT_CLEAR.Text = "Clear"
            Me.BTT_CLEAR.ToolTipText = "Clear data list"
            '
            'TP_DATA
            '
            Me.TP_DATA.AutoScroll = True
            Me.TP_DATA.ColumnCount = 1
            Me.TP_DATA.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.TP_DATA.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20.0!))
            Me.TP_DATA.Dock = System.Windows.Forms.DockStyle.Fill
            Me.TP_DATA.Location = New System.Drawing.Point(0, 25)
            Me.TP_DATA.Name = "TP_DATA"
            Me.TP_DATA.RowCount = 11
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.RowStyles.Add(New System.Windows.Forms.RowStyle())
            Me.TP_DATA.Size = New System.Drawing.Size(484, 436)
            Me.TP_DATA.TabIndex = 1
            '
            'DownloadFeedForm
            '
            Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.BackColor = System.Drawing.SystemColors.Window
            Me.ClientSize = New System.Drawing.Size(484, 461)
            Me.Controls.Add(Me.TP_DATA)
            Me.Controls.Add(Me.ToolbarTOP)
            Me.ForeColor = System.Drawing.SystemColors.WindowText
            Me.Icon = Global.SCrawler.My.Resources.Resources.RSSIcon_32
            Me.KeyPreview = True
            Me.MinimumSize = New System.Drawing.Size(300, 300)
            Me.Name = "DownloadFeedForm"
            Me.Text = "Download Feed"
            Me.ToolbarTOP.ResumeLayout(False)
            Me.ToolbarTOP.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Private WithEvents ToolbarTOP As ToolStrip
        Private WithEvents TP_DATA As TableLayoutPanel
        Private WithEvents BTT_REFRESH As ToolStripButton
        Private WithEvents BTT_CLEAR As ToolStripButton
        Private WithEvents MENU_LOAD_SESSION As ToolStripDropDownButton
        Private WithEvents BTT_LOAD_SESSION_LAST As ToolStripMenuItem
        Private WithEvents BTT_LOAD_SESSION_CHOOSE As ToolStripMenuItem
        Private WithEvents SEP_0 As ToolStripSeparator
    End Class
End Namespace