﻿Imports System.IO

Friend Class frmClasses

#Region "Frm Controls"

    Private Sub FrmEditor_Classes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        nudMaleSprite.Maximum = NumCharacters
        nudFemaleSprite.Maximum = NumCharacters

        DrawPreview()
    End Sub

    Private Sub LstIndex_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstIndex.SelectedIndexChanged
        If lstIndex.SelectedIndex < 0 Then Exit Sub

        Editorindex = lstIndex.SelectedIndex + 1

        LoadClassInfo = True
    End Sub

    Private Sub BtnAddClass_Click(sender As Object, e As EventArgs) Handles btnAddClass.Click
        Max_Classes = Max_Classes + 1

        ReDim Preserve Classes(Max_Classes)

        Classes(Max_Classes).Name = "New Class"

        ReDim Classes(Max_Classes).Stat(StatType.Count - 1)

        ReDim Classes(Max_Classes).Vital(VitalType.Count - 1)

        ReDim Classes(Max_Classes).MaleSprite(1)
        ReDim Classes(Max_Classes).FemaleSprite(1)

        For i = 1 To StatType.Count - 1
            Classes(Max_Classes).Stat(i) = 1
        Next

        ReDim Classes(Max_Classes).StartItem(5)
        ReDim Classes(Max_Classes).StartValue(5)

        Classes(Max_Classes).StartMap = 1
        Classes(Max_Classes).StartX = 1
        Classes(Max_Classes).StartY = 1

        ClassEditorInit()
    End Sub

    Private Sub BtnRemoveClass_Click(sender As Object, e As EventArgs) Handles btnRemoveClass.Click
        Dim i As Integer

        'If its The Last class, its simple, just remove and redim
        If Editorindex = Max_Classes Then
            Max_Classes = Max_Classes - 1
            ReDim Preserve Classes(Max_Classes)
        Else
            'but if its somewhere in the middle, or beginning, it gets harder xD
            For i = 1 To Max_Classes
                'we shift everything thats beneath the selected class, up 1 slot
                Classes(Editorindex) = Classes(Editorindex + 1)
            Next

            'and then we remove it, and redim
            Max_Classes = Max_Classes - 1
            ReDim Preserve Classes(Max_Classes)
        End If

        ClassEditorInit()
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ClassesEditorOk()
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        ClassesEditorCancel()
    End Sub

    Private Sub TxtDescription_TextChanged(sender As Object, e As EventArgs) Handles txtDescription.TextChanged
        Classes(Editorindex).Desc = txtDescription.Text
    End Sub

    Private Sub TxtName_TextChanged(sender As Object, e As EventArgs) Handles txtName.TextChanged
        Dim tmpindex As Integer
        If Editorindex = 0 OrElse Editorindex > Max_Classes Then Exit Sub

        tmpindex = lstIndex.SelectedIndex
        Classes(Editorindex).Name = Trim$(txtName.Text)
        lstIndex.Items.RemoveAt(Editorindex - 1)
        lstIndex.Items.Insert(Editorindex - 1, Trim(Classes(Editorindex).Name))
        lstIndex.SelectedIndex = tmpindex
    End Sub

#End Region

#Region "Sprites"

    Private Sub BtnAddMaleSprite_Click(sender As Object, e As EventArgs) Handles btnAddMaleSprite.Click
        Dim tmpamount As Byte
        If Editorindex = 0 OrElse Editorindex > Max_Classes Then Exit Sub

        tmpamount = UBound(Classes(Editorindex).MaleSprite)

        ReDim Preserve Classes(Editorindex).MaleSprite(tmpamount + 1)

        Classes(Editorindex).MaleSprite(tmpamount + 1) = 1

        LoadClassInfo = True
    End Sub

    Private Sub BtnDeleteMaleSprite_Click(sender As Object, e As EventArgs) Handles btnDeleteMaleSprite.Click
        Dim tmpamount As Byte
        If Editorindex = 0 OrElse Editorindex > Max_Classes Then Exit Sub

        tmpamount = UBound(Classes(Editorindex).MaleSprite)

        ReDim Preserve Classes(Editorindex).MaleSprite(tmpamount - 1)

        LoadClassInfo = True
    End Sub

    Private Sub BtnAddFemaleSprite_Click(sender As Object, e As EventArgs) Handles btnAddFemaleSprite.Click
        Dim tmpamount As Byte
        If Editorindex = 0 OrElse Editorindex > Max_Classes Then Exit Sub

        tmpamount = UBound(Classes(Editorindex).FemaleSprite)

        ReDim Preserve Classes(Editorindex).FemaleSprite(tmpamount + 1)

        Classes(Editorindex).FemaleSprite(tmpamount + 1) = 1

        LoadClassInfo = True
    End Sub

    Private Sub BtnDeleteFemaleSprite_Click(sender As Object, e As EventArgs) Handles btnDeleteFemaleSprite.Click
        Dim tmpamount As Byte
        If Editorindex = 0 OrElse Editorindex > Max_Classes Then Exit Sub

        tmpamount = UBound(Classes(Editorindex).FemaleSprite)

        ReDim Preserve Classes(Editorindex).FemaleSprite(tmpamount - 1)

        LoadClassInfo = True
    End Sub

    Private Sub NudMaleSprite_ValueChanged(sender As Object, e As EventArgs) Handles nudMaleSprite.Click
        If cmbMaleSprite.SelectedIndex < 0 Then Exit Sub

        Classes(Editorindex).MaleSprite(cmbMaleSprite.SelectedIndex) = nudMaleSprite.Value

        DrawPreview()
    End Sub

    Private Sub NudFemaleSprite_ValueChanged(sender As Object, e As EventArgs) Handles nudFemaleSprite.Click
        If cmbFemaleSprite.SelectedIndex < 0 Then Exit Sub

        Classes(Editorindex).FemaleSprite(cmbFemaleSprite.SelectedIndex) = nudFemaleSprite.Value

        DrawPreview()
    End Sub

    Private Sub CmbMaleSprite_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMaleSprite.SelectedIndexChanged
        nudMaleSprite.Value = Classes(Editorindex).MaleSprite(cmbMaleSprite.SelectedIndex)
        DrawPreview()
    End Sub

    Private Sub CmbFemaleSprite_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFemaleSprite.SelectedIndexChanged
        nudFemaleSprite.Value = Classes(Editorindex).FemaleSprite(cmbFemaleSprite.SelectedIndex)
        DrawPreview()
    End Sub

    Sub DrawPreview()

        If File.Exists(Application.StartupPath & GFX_PATH & "Characters\" & nudMaleSprite.Value & GFX_EXT) Then
            picMale.Width = Image.FromFile(Application.StartupPath & GFX_PATH & "characters\" & nudMaleSprite.Value & GFX_EXT).Width \ 4
            picMale.Height = Image.FromFile(Application.StartupPath & GFX_PATH & "characters\" & nudMaleSprite.Value & GFX_EXT).Height \ 4
            picMale.BackgroundImage = Image.FromFile(Application.StartupPath & GFX_PATH & "Characters\" & nudMaleSprite.Value & GFX_EXT)
        End If

        If File.Exists(Application.StartupPath & GFX_PATH & "Characters\" & nudFemaleSprite.Value & GFX_EXT) Then
            picFemale.Width = Image.FromFile(Application.StartupPath & GFX_PATH & "characters\" & nudFemaleSprite.Value & GFX_EXT).Width \ 4
            picFemale.Height = Image.FromFile(Application.StartupPath & GFX_PATH & "characters\" & nudFemaleSprite.Value & GFX_EXT).Height \ 4
            picFemale.BackgroundImage = Image.FromFile(Application.StartupPath & GFX_PATH & "Characters\" & nudFemaleSprite.Value & GFX_EXT)
        End If

    End Sub

    Private Sub PicMale_Paint(sender As Object, e As EventArgs) Handles picMale.Paint
        'nope
    End Sub

    Private Sub PicFemale_Paint(sender As Object, e As EventArgs) Handles picFemale.Paint
        'nope
    End Sub

#End Region

#Region "Stats"

    Private Sub NumStrength_ValueChanged(sender As Object, e As EventArgs) Handles nudStrength.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Strength) = nudStrength.Value
    End Sub

    Private Sub NumLuck_ValueChanged(sender As Object, e As EventArgs) Handles nudLuck.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Luck) = nudLuck.Value
    End Sub

    Private Sub NumEndurance_ValueChanged(sender As Object, e As EventArgs) Handles nudEndurance.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Endurance) = nudEndurance.Value
    End Sub

    Private Sub NumIntelligence_ValueChanged(sender As Object, e As EventArgs) Handles nudIntelligence.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Intelligence) = nudIntelligence.Value
    End Sub

    Private Sub NumVitality_ValueChanged(sender As Object, e As EventArgs) Handles nudVitality.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Vitality) = nudVitality.Value
    End Sub

    Private Sub NumSpirit_ValueChanged(sender As Object, e As EventArgs) Handles nudSpirit.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).Stat(StatType.Spirit) = nudSpirit.Value
    End Sub

    Private Sub NumBaseExp_ValueChanged(sender As Object, e As EventArgs) Handles nudBaseExp.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).BaseExp = nudBaseExp.Value
    End Sub

#End Region

#Region "Start Items"

    Private Sub BtnItemAdd_Click(sender As Object, e As EventArgs) Handles btnItemAdd.Click
        If lstStartItems.SelectedIndex < 0 OrElse cmbItems.SelectedIndex < 0 Then Exit Sub

        Classes(Editorindex).StartItem(lstStartItems.SelectedIndex + 1) = cmbItems.SelectedIndex
        Classes(Editorindex).StartValue(lstStartItems.SelectedIndex + 1) = nudItemAmount.Value

        LoadClassInfo = True
    End Sub

#End Region

#Region "Starting Point"

    Private Sub NumStartMap_ValueChanged(sender As Object, e As EventArgs) Handles nudStartMap.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).StartMap = nudStartMap.Value
    End Sub

    Private Sub NumStartX_ValueChanged(sender As Object, e As EventArgs) Handles nudStartX.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).StartX = nudStartX.Value
    End Sub

    Private Sub NumStartY_ValueChanged(sender As Object, e As EventArgs) Handles nudStartY.Click
        If Editorindex <= 0 OrElse Editorindex > Max_Classes Then Exit Sub

        Classes(Editorindex).StartY = nudStartY.Value
    End Sub

#End Region

End Class