﻿Public Class frmEditor_Item

#Region "Form Code"

    Private Sub BtnSave_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSave.Click
        ItemEditorOk()
    End Sub

    Private Sub BtnCancel_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnCancel.Click
        ItemEditorCancel()
    End Sub

    Private Sub BtnDelete_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnDelete.Click
        Dim tmpIndex As Integer

        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        ClearItem(EditorIndex + 1)

        tmpIndex = lstIndex.SelectedIndex
        lstIndex.Items.RemoveAt(EditorIndex - 1)
        lstIndex.Items.Insert(EditorIndex - 1, EditorIndex & ": " & Item(EditorIndex).Name)
        lstIndex.SelectedIndex = tmpIndex

        ItemEditorInit()
    End Sub

    Private Sub LstIndex_Click(ByVal sender As Object, ByVal e As EventArgs) Handles lstIndex.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        ItemEditorInit()
    End Sub

    Private Sub PicItem_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picItem.Paint
        'Dont let it auto paint ;)
    End Sub

    Private Sub PicPaperdoll_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picPaperdoll.Paint
        'Dont let it auto paint :0
    End Sub

    Private Sub PicFurniture_Paint(ByVal sender As Object, ByVal e As PaintEventArgs) Handles picFurniture.Paint
        'Dont let it auto paint ;)
    End Sub

    Private Sub FrmEditor_Item_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
        nudPic.Maximum = NumItems
        nudPaperdoll.Maximum = NumPaperdolls
        nudFurniture.Maximum = NumFurniture
        cmbFurnitureType.SelectedIndex = 0
    End Sub

    Private Sub BtnBasics_Click(sender As Object, e As EventArgs) Handles btnBasics.Click
        fraBasics.Visible = True
        fraRequirements.Visible = False
    End Sub

    Private Sub BtnRequirements_Click(sender As Object, e As EventArgs) Handles btnRequirements.Click
        fraBasics.Visible = False
        fraRequirements.Visible = True
    End Sub
#End Region

#Region "Basics"
    Private Sub NudPic_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudPic.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Pic = nudPic.Value
        EditorItem_DrawItem()
    End Sub

    Private Sub CmbBind_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbBind.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).BindType = cmbBind.SelectedIndex
    End Sub

    Private Sub NudRarity_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudRarity.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Rarity = nudRarity.Value
    End Sub

    Private Sub CmbAnimation_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbAnimation.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Animation = cmbAnimation.SelectedIndex
    End Sub

    Private Sub CmbType_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbType.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        cmbSubType.Enabled = False

        If (cmbType.SelectedIndex = ItemType.Equipment) Then
            fraEquipment.Visible = True

            ' Build subtype cmb
            cmbSubType.Items.Clear()
            cmbSubType.Items.Add("None")

            cmbSubType.Items.Add("Weapon")
            cmbSubType.Items.Add("Armor")
            cmbSubType.Items.Add("Helmet")
            cmbSubType.Items.Add("Shield")
            cmbSubType.Items.Add("Shoes")
            cmbSubType.Items.Add("Gloves")

            cmbSubType.Enabled = True
            cmbSubType.SelectedIndex = Item(EditorIndex).SubType
        Else
            fraEquipment.Visible = False
        End If

        If (cmbType.SelectedIndex = ItemType.Consumable) Then
            fraVitals.Visible = True

            ' Build subtype cmb
            cmbSubType.Items.Clear()
            cmbSubType.Items.Add("None")

            cmbSubType.Items.Add("Hp")
            cmbSubType.Items.Add("Mp")
            cmbSubType.Items.Add("Sp")
            cmbSubType.Items.Add("Exp")

            cmbSubType.Enabled = True
            cmbSubType.SelectedIndex = Item(EditorIndex).SubType
        Else
            fraVitals.Visible = False
        End If

        If (cmbType.SelectedIndex = ItemType.Skill) Then
            fraSkill.Visible = True
        Else
            fraSkill.Visible = False
        End If

        If cmbType.SelectedIndex = ItemType.Furniture Then
            fraFurniture.Visible = True
        Else
            fraFurniture.Visible = False
        End If

        If cmbType.SelectedIndex = ItemType.Recipe Then
            fraRecipe.Visible = True
        Else
            fraRecipe.Visible = False
        End If

        Item(EditorIndex).Type = cmbType.SelectedIndex
    End Sub

    Private Sub NudVitalMod_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudVitalMod.Click
        If EditorIndex <= 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data1 = nudVitalMod.Value
    End Sub

    Private Sub CmbSkills_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbSkills.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data1 = cmbSkills.SelectedIndex
    End Sub

    Private Sub TxtName_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles txtName.TextChanged
        Dim tmpIndex As Integer
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        tmpIndex = lstIndex.SelectedIndex
        Item(EditorIndex).Name = Trim$(txtName.Text)
        lstIndex.Items.RemoveAt(EditorIndex - 1)
        lstIndex.Items.Insert(EditorIndex - 1, EditorIndex & ": " & Item(EditorIndex).Name)
        lstIndex.SelectedIndex = tmpIndex
    End Sub

    Private Sub NudPrice_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudPrice.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Price = nudPrice.Value
    End Sub

    Private Sub ChkStackable_CheckedChanged(sender As Object, e As EventArgs) Handles chkStackable.CheckedChanged
        If chkStackable.Checked = True Then
            Item(EditorIndex).Stackable = 1
        Else
            Item(EditorIndex).Stackable = 0
        End If
    End Sub

    Private Sub CmbRecipe_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbRecipe.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data1 = cmbRecipe.SelectedIndex
    End Sub

    Private Sub TxtDescription_TextChanged(sender As Object, e As EventArgs) Handles txtDescription.TextChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Description = Trim$(txtDescription.Text)
    End Sub

    Private Sub CmbSubType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSubType.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).SubType = cmbSubType.SelectedIndex
    End Sub

    Private Sub NudItemLvl_ValueChanged(sender As Object, e As EventArgs) Handles nudItemLvl.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).ItemLevel = nudItemLvl.Value
    End Sub

    Private Sub CmbPet_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbPet.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data1 = cmbPet.SelectedIndex
    End Sub
#End Region

#Region "Requirements"
    Private Sub CmbClassReq_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbClassReq.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).ClassReq = cmbClassReq.SelectedIndex
    End Sub

    Private Sub CmbAccessReq_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbAccessReq.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).AccessReq = cmbAccessReq.SelectedIndex
    End Sub

    Private Sub NudLevelReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudLevelReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).LevelReq = nudLevelReq.Value
    End Sub

    Private Sub NudStrReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudStrReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Strength) = nudStrReq.Value
    End Sub

    Private Sub NudEndReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudEndReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Endurance) = nudEndReq.Value
    End Sub

    Private Sub NudVitReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudVitReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Vitality) = nudVitReq.Value
    End Sub

    Private Sub NudLuckReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudLuckReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Luck) = nudLuckReq.Value
    End Sub

    Private Sub NudIntReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudIntReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Intelligence) = nudIntReq.Value
    End Sub

    Private Sub NudSprReq_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudSprReq.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Stat_Req(Stats.Spirit) = nudSprReq.Value
    End Sub
#End Region

#Region "Equipment"
    Private Sub CmbTool_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbTool.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        Item(EditorIndex).Data3 = cmbTool.SelectedIndex
    End Sub

    Private Sub NudDamage_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudDamage.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data2 = nudDamage.Value
    End Sub

    Private Sub NudSpeed_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudSpeed.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        lblSpeed.Text = "Speed: " & nudSpeed.Value / 1000 & " sec"
        Item(EditorIndex).Speed = nudSpeed.Value
    End Sub

    Private Sub NudPaperdoll_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudPaperdoll.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Paperdoll = nudPaperdoll.Value
        EditorItem_DrawPaperdoll()
    End Sub

    Private Sub NudStrength_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudStrength.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Strength) = nudStrength.Value
    End Sub

    Private Sub NudLuck_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudLuck.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Luck) = nudLuck.Value
    End Sub

    Private Sub NudEndurance_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudEndurance.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Endurance) = nudEndurance.Value
    End Sub

    Private Sub NudIntelligence_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudIntelligence.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Intelligence) = nudIntelligence.Value
    End Sub

    Private Sub NudVitality_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudVitality.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Vitality) = nudVitality.Value
    End Sub

    Private Sub NudSpirit_ValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles nudSpirit.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Add_Stat(Stats.Spirit) = nudSpirit.Value
    End Sub

    Private Sub ChkKnockBack_CheckedChanged(sender As Object, e As EventArgs) Handles chkKnockBack.CheckedChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        If chkKnockBack.Checked = True Then
            Item(EditorIndex).KnockBack = 1
        Else
            Item(EditorIndex).KnockBack = 0
        End If
    End Sub

    Private Sub CmbKnockBackTiles_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbKnockBackTiles.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).KnockBackTiles = cmbKnockBackTiles.SelectedIndex
    End Sub

    Private Sub ChkRandomize_CheckedChanged(sender As Object, e As EventArgs) Handles chkRandomize.CheckedChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        If chkRandomize.Checked = True Then
            Item(EditorIndex).Randomize = 1
        Else
            Item(EditorIndex).Randomize = 0
        End If
    End Sub

    Private Sub CmbProjectile_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cmbProjectile.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Projectile = cmbProjectile.SelectedIndex
    End Sub

    Private Sub CmbAmmo_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbAmmo.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Ammo = cmbAmmo.SelectedIndex
    End Sub
#End Region

#Region "Furniture"
    Private Sub CmbFurnitureType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbFurnitureType.SelectedIndexChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        Item(EditorIndex).Data1 = cmbFurnitureType.SelectedIndex
    End Sub

    Private Sub OptNoFurnitureEditing_CheckedChanged(sender As Object, e As EventArgs) Handles optNoFurnitureEditing.CheckedChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        If optNoFurnitureEditing.Checked = True Then
            lblSetOption.Text = "No Editing: Lets you look at the image without setting any options (blocks/fringes)."
        End If
        EditorItem_DrawFurniture()
    End Sub

    Private Sub OptSetBlocks_CheckedChanged(sender As Object, e As EventArgs) Handles optSetBlocks.CheckedChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        If optSetBlocks.Checked = True Then
            lblSetOption.Text = "Set Blocks: Os are passable and Xs are not. Simply place Xs where you do not want the player to walk."
        End If
        EditorItem_DrawFurniture()
    End Sub

    Private Sub OptSetFringe_CheckedChanged(sender As Object, e As EventArgs) Handles optSetFringe.CheckedChanged
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        If optSetFringe.Checked = True Then
            lblSetOption.Text = "Set Fringe: Os are draw on the fringe layer (the player can walk behind)."
        End If
        EditorItem_DrawFurniture()
    End Sub

    Private Sub NudFurniture_ValueChanged(sender As Object, e As EventArgs) Handles nudFurniture.Click
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub

        Item(EditorIndex).Data2 = nudFurniture.Value

        If nudFurniture.Value > 0 And nudFurniture.Value <= NumFurniture Then
            Item(EditorIndex).FurnitureWidth = FurnitureGFXInfo(nudFurniture.Value).width / 32
            Item(EditorIndex).FurnitureHeight = FurnitureGFXInfo(nudFurniture.Value).height / 32
            If Item(EditorIndex).FurnitureHeight > 1 Then Item(EditorIndex).FurnitureHeight = Item(EditorIndex).FurnitureHeight - 1
        Else
            Item(EditorIndex).FurnitureWidth = 1
            Item(EditorIndex).FurnitureHeight = 1
        End If

        EditorItem_DrawFurniture()
    End Sub

    Private Sub PicFurniture_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles picFurniture.MouseDown
        If EditorIndex = 0 Or EditorIndex > MAX_ITEMS Then Exit Sub
        Dim X As Long, Y As Long
        X = e.X / 32
        Y = e.Y / 32

        If X > 3 Then Exit Sub
        If Y > 3 Then Exit Sub

        If optSetBlocks.Checked = True Then
            If Item(EditorIndex).FurnitureBlocks(X, Y) = 1 Then
                Item(EditorIndex).FurnitureBlocks(X, Y) = 0
            Else
                Item(EditorIndex).FurnitureBlocks(X, Y) = 1
            End If
        ElseIf optSetFringe.Checked = True Then
            If Item(EditorIndex).FurnitureFringe(X, Y) = 1 Then
                Item(EditorIndex).FurnitureFringe(X, Y) = 0
            Else
                Item(EditorIndex).FurnitureFringe(X, Y) = 1
            End If
        End If
        EditorItem_DrawFurniture()
    End Sub
#End Region

End Class