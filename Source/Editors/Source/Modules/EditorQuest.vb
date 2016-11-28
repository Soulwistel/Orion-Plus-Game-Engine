﻿Public Module EditorQuest
#Region "Global Info"
    'Constants
    Public Const MAX_QUESTS As Byte = 250
    'Public Const MAX_REQUIREMENTS As Byte = 10
    'Public Const MAX_TASKS As Byte = 10
    Public Const EDITOR_TASKS As Byte = 7

    Public Const QUEST_TYPE_GOSLAY As Byte = 1
    Public Const QUEST_TYPE_GOGATHER As Byte = 2
    Public Const QUEST_TYPE_GOTALK As Byte = 3
    Public Const QUEST_TYPE_GOREACH As Byte = 4
    Public Const QUEST_TYPE_GOGIVE As Byte = 5
    Public Const QUEST_TYPE_GOKILL As Byte = 6
    Public Const QUEST_TYPE_GOTRAIN As Byte = 7
    Public Const QUEST_TYPE_GOGET As Byte = 8

    Public Const QUEST_NOT_STARTED As Byte = 0
    Public Const QUEST_STARTED As Byte = 1
    Public Const QUEST_COMPLETED As Byte = 2
    Public Const QUEST_COMPLETED_BUT As Byte = 3

    Public QuestLogPage As Integer
    Public QuestNames(MAX_ACTIVEQUESTS) As String

    Public Quest_Changed(MAX_QUESTS) As Boolean

    Public QuestEditorShow As Boolean

    'questlog variables

    Public Const MAX_ACTIVEQUESTS = 10

    Public QuestLogX As Integer = 150
    Public QuestLogY As Integer = 100

    Public pnlQuestLogVisible As Boolean
    Public SelectedQuest As String
    Public QuestTaskLogText As String = ""
    Public ActualTaskText As String = ""
    Public QuestDialogText As String = ""
    Public QuestStatus2Text As String = ""
    Public AbandonQuestText As String = ""
    Public AbandonQuestVisible As Boolean
    Public QuestRequirementsText As String = ""
    Public QuestRewardsText As String = ""

    'here we store temp info because off UpdateUI >.<
    Public UpdateQuestWindow As Boolean
    Public UpdateQuestChat As Boolean
    Public QuestNum As Integer
    Public QuestNumForStart As Integer
    Public QuestMessage As String
    Public QuestAcceptTag As Integer

    'Types
    Public Quest(0 To MAX_QUESTS) As QuestRec

    Public Structure PlayerQuestRec
        Dim Status As Integer '0=not started, 1=started, 2=completed, 3=completed but repeatable
        Dim ActualTask As Integer
        Dim CurrentCount As Integer 'Used to handle the Amount property
    End Structure

    Public Structure TaskRec
        Dim Order As Integer
        Dim Npc As Integer
        Dim Item As Integer
        Dim Map As Integer
        Dim Resource As Integer
        Dim Amount As Integer
        Dim Speech As String
        Dim TaskLog As String
        Dim QuestEnd As Byte
        Dim TaskType As Integer
    End Structure

    Public Structure QuestRec
        Dim Name As String
        Dim QuestLog As String
        Dim Repeat As Byte
        Dim Cancelable As Byte

        Dim ReqCount As Integer
        Dim Requirement() As Integer '1=item, 2=quest, 3=class
        Dim RequirementIndex() As Integer

        Dim QuestGiveItem As Integer 'Todo: make this dynamic
        Dim QuestGiveItemValue As Integer
        Dim QuestRemoveItem As Integer
        Dim QuestRemoveItemValue As Integer

        Dim Chat() As String

        Dim RewardCount As Integer
        Dim RewardItem() As Integer
        Dim RewardItemAmount() As Integer
        Dim RewardExp As Integer

        Dim TaskCount As Integer
        Dim Task() As TaskRec

    End Structure
#End Region

#Region "Quest Editor"
    Public Sub QuestEditorInit()

        If frmEditor_Quest.Visible = False Then Exit Sub
        EditorIndex = frmEditor_Quest.lstIndex.SelectedIndex + 1

        With frmEditor_Quest
            .txtName.Text = Trim$(Quest(EditorIndex).Name)

            If Quest(EditorIndex).Repeat = 1 Then
                .chkRepeat.Checked = True
            Else
                .chkRepeat.Checked = False
            End If

            .txtStartText.Text = Trim$(Quest(EditorIndex).Chat(1))
            .txtProgressText.Text = Trim$(Quest(EditorIndex).Chat(2))
            .txtEndText.Text = Trim$(Quest(EditorIndex).Chat(3))

            .scrlStartItemName.Value = Quest(EditorIndex).QuestGiveItem
            .scrlEndItemName.Value = Quest(EditorIndex).QuestRemoveItem

            If Not Quest(EditorIndex).QuestGiveItemValue = 0 Then
                .scrlStartItemAmount.Value = Quest(EditorIndex).QuestGiveItemValue
            Else
                .scrlStartItemAmount.Value = 0
            End If

            If Not Quest(EditorIndex).QuestRemoveItemValue = 0 Then
                .scrlEndItemAmount.Value = Quest(EditorIndex).QuestRemoveItemValue
            Else
                .scrlEndItemAmount.Value = 0
            End If

            frmEditor_Quest.lstRewards.Items.Clear()
            For i = 1 To Quest(EditorIndex).RewardCount
                frmEditor_Quest.lstRewards.Items.Add(i & ":" & Quest(EditorIndex).RewardItemAmount(i) & " X " & Trim(Item(Quest(EditorIndex).RewardItem(i)).Name))
            Next

            If Not Quest(EditorIndex).RewardExp = 0 Then
                .scrlExpReward.Value = Quest(EditorIndex).RewardExp
            Else
                .scrlExpReward.Value = 0
            End If

            frmEditor_Quest.lstRequirements.Items.Clear()

            For i = 1 To Quest(EditorIndex).ReqCount

                Select Case Quest(EditorIndex).Requirement(i)
                    Case 1
                        frmEditor_Quest.lstRequirements.Items.Add(i & ":" & "Item Requirement: " & Trim(Item(Quest(EditorIndex).RequirementIndex(i)).Name))
                    Case 2
                        frmEditor_Quest.lstRequirements.Items.Add(i & ":" & "Quest Requirement: " & Trim(Quest(Quest(EditorIndex).RequirementIndex(i)).Name))
                    Case 3
                        frmEditor_Quest.lstRequirements.Items.Add(i & ":" & "Class Requirement: " & Trim(Classes(Quest(EditorIndex).RequirementIndex(i)).Name))
                    Case Else
                        frmEditor_Quest.lstRequirements.Items.Add(i & ":")
                End Select
            Next

            frmEditor_Quest.lstTasks.Items.Clear()
            For i = 1 To Quest(EditorIndex).TaskCount
                frmEditor_Quest.lstTasks.Items.Add(i & ":" & Quest(EditorIndex).Task(i).TaskLog)
            Next

        End With

        Quest_Changed(EditorIndex) = True

    End Sub

    Public Sub QuestEditorOk()
        Dim I As Integer

        For I = 1 To MAX_QUESTS
            If Quest_Changed(I) Then
                SendSaveQuest(I)
            End If
        Next

        frmEditor_Quest.Dispose()
        Editor = 0
        ClearChanged_Quest()

    End Sub

    Public Sub QuestEditorCancel()
        Editor = 0
        frmEditor_Quest.Dispose()
        ClearChanged_Quest()
        ClearQuests()
        SendRequestQuests()
    End Sub

    Public Sub ClearChanged_Quest()
        Dim I As Integer

        For I = 0 To MAX_QUESTS
            Quest_Changed(I) = False
        Next
    End Sub
#End Region

#Region "DataBase"
    Sub ClearQuest(ByVal QuestNum As Integer)
        Dim I As Integer

        ' clear the Quest
        Quest(QuestNum).Name = ""
        Quest(QuestNum).QuestLog = ""
        Quest(QuestNum).Repeat = 0
        Quest(QuestNum).Cancelable = 0

        Quest(QuestNum).ReqCount = 0
        ReDim Quest(QuestNum).Requirement(Quest(QuestNum).ReqCount)
        ReDim Quest(QuestNum).RequirementIndex(Quest(QuestNum).ReqCount)
        For I = 1 To Quest(QuestNum).ReqCount
            Quest(QuestNum).Requirement(I) = 0
            Quest(QuestNum).RequirementIndex(I) = 0
        Next

        Quest(QuestNum).QuestGiveItem = 0
        Quest(QuestNum).QuestGiveItemValue = 0
        Quest(QuestNum).QuestRemoveItem = 0
        Quest(QuestNum).QuestRemoveItemValue = 0

        ReDim Quest(QuestNum).Chat(3)
        For I = 1 To 3
            Quest(QuestNum).Chat(I) = ""
        Next

        Quest(QuestNum).RewardCount = 0
        ReDim Quest(QuestNum).RewardItem(Quest(QuestNum).RewardCount)
        ReDim Quest(QuestNum).RewardItemAmount(Quest(QuestNum).RewardCount)
        For I = 1 To Quest(QuestNum).RewardCount
            Quest(QuestNum).RewardItem(I) = 0
            Quest(QuestNum).RewardItemAmount(I) = 0
        Next
        Quest(QuestNum).RewardExp = 0

        Quest(QuestNum).TaskCount = 0
        ReDim Quest(QuestNum).Task(Quest(QuestNum).TaskCount)
        For I = 1 To Quest(QuestNum).TaskCount
            Quest(QuestNum).Task(I).Order = 0
            Quest(QuestNum).Task(I).Npc = 0
            Quest(QuestNum).Task(I).Item = 0
            Quest(QuestNum).Task(I).Map = 0
            Quest(QuestNum).Task(I).Resource = 0
            Quest(QuestNum).Task(I).Amount = 0
            Quest(QuestNum).Task(I).Speech = ""
            Quest(QuestNum).Task(I).TaskLog = ""
            Quest(QuestNum).Task(I).QuestEnd = 0
            Quest(QuestNum).Task(I).TaskType = 0
        Next

    End Sub

    Sub ClearQuests()
        Dim I As Integer

        For I = 1 To MAX_QUESTS
            ClearQuest(I)
        Next
    End Sub
#End Region

#Region "Incoming Packets"
    Public Sub Packet_QuestEditor(ByVal data() As Byte)
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteBytes(data)

        If buffer.ReadInteger <> ServerPackets.SQuestEditor Then Exit Sub

        QuestEditorShow = True

        buffer = Nothing
    End Sub

    Public Sub Packet_UpdateQuest(ByVal data() As Byte)
        Dim QuestNum As Integer
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteBytes(data)

        If buffer.ReadInteger <> ServerPackets.SUpdateQuest Then Exit Sub

        QuestNum = buffer.ReadInteger

        ' Update the Quest
        Quest(QuestNum).Name = buffer.ReadString
        Quest(QuestNum).QuestLog = buffer.ReadString
        Quest(QuestNum).Repeat = buffer.ReadInteger
        Quest(QuestNum).Cancelable = buffer.ReadInteger

        Quest(QuestNum).ReqCount = buffer.ReadInteger
        ReDim Quest(QuestNum).Requirement(Quest(QuestNum).ReqCount)
        ReDim Quest(QuestNum).RequirementIndex(Quest(QuestNum).ReqCount)
        For I = 1 To Quest(QuestNum).ReqCount
            Quest(QuestNum).Requirement(I) = buffer.ReadInteger
            Quest(QuestNum).RequirementIndex(I) = buffer.ReadInteger
        Next

        Quest(QuestNum).QuestGiveItem = buffer.ReadInteger
        Quest(QuestNum).QuestGiveItemValue = buffer.ReadInteger
        Quest(QuestNum).QuestRemoveItem = buffer.ReadInteger
        Quest(QuestNum).QuestRemoveItemValue = buffer.ReadInteger

        For I = 1 To 3
            Quest(QuestNum).Chat(I) = buffer.ReadString
        Next

        Quest(QuestNum).RewardCount = buffer.ReadInteger
        ReDim Quest(QuestNum).RewardItem(Quest(QuestNum).RewardCount)
        ReDim Quest(QuestNum).RewardItemAmount(Quest(QuestNum).RewardCount)
        For i = 1 To Quest(QuestNum).RewardCount
            Quest(QuestNum).RewardItem(i) = buffer.ReadInteger
            Quest(QuestNum).RewardItemAmount(i) = buffer.ReadInteger
        Next

        Quest(QuestNum).RewardExp = buffer.ReadInteger

        Quest(QuestNum).TaskCount = buffer.ReadInteger
        ReDim Quest(QuestNum).Task(Quest(QuestNum).TaskCount)
        For I = 1 To Quest(QuestNum).TaskCount
            Quest(QuestNum).Task(I).Order = buffer.ReadInteger
            Quest(QuestNum).Task(I).Npc = buffer.ReadInteger
            Quest(QuestNum).Task(I).Item = buffer.ReadInteger
            Quest(QuestNum).Task(I).Map = buffer.ReadInteger
            Quest(QuestNum).Task(I).Resource = buffer.ReadInteger
            Quest(QuestNum).Task(I).Amount = buffer.ReadInteger
            Quest(QuestNum).Task(I).Speech = buffer.ReadString
            Quest(QuestNum).Task(I).TaskLog = buffer.ReadString
            Quest(QuestNum).Task(I).QuestEnd = buffer.ReadInteger
            Quest(QuestNum).Task(I).TaskType = buffer.ReadInteger
        Next

        buffer = Nothing
    End Sub

#End Region

#Region "Outgoing Packets"
    Public Sub SendRequestEditQuest()
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteInteger(ClientPackets.CRequestEditQuest)
        SendData(buffer.ToArray)
        buffer = Nothing

    End Sub

    Public Sub SendSaveQuest(ByVal QuestNum As Integer)
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer

        buffer.WriteInteger(ClientPackets.CSaveQuest)
        buffer.WriteInteger(QuestNum)

        buffer.WriteString(Trim(Quest(QuestNum).Name))
        buffer.WriteString(Trim(Quest(QuestNum).QuestLog))
        buffer.WriteInteger(Quest(QuestNum).Repeat)
        buffer.WriteInteger(Quest(QuestNum).Cancelable)

        buffer.WriteInteger(Quest(QuestNum).ReqCount)
        For I = 1 To Quest(QuestNum).ReqCount
            buffer.WriteInteger(Quest(QuestNum).Requirement(I))
            buffer.WriteInteger(Quest(QuestNum).RequirementIndex(I))
        Next

        buffer.WriteInteger(Quest(QuestNum).QuestGiveItem)
        buffer.WriteInteger(Quest(QuestNum).QuestGiveItemValue)
        buffer.WriteInteger(Quest(QuestNum).QuestRemoveItem)
        buffer.WriteInteger(Quest(QuestNum).QuestRemoveItemValue)

        For I = 1 To 3
            buffer.WriteString(Trim(Quest(QuestNum).Chat(I)))
        Next

        buffer.WriteInteger(Quest(QuestNum).RewardCount)
        For i = 1 To Quest(QuestNum).RewardCount
            buffer.WriteInteger(Quest(QuestNum).RewardItem(i))
            buffer.WriteInteger(Quest(QuestNum).RewardItemAmount(i))
        Next

        buffer.WriteInteger(Quest(QuestNum).RewardExp)

        buffer.WriteInteger(Quest(QuestNum).TaskCount)
        For I = 1 To Quest(QuestNum).TaskCount
            buffer.WriteInteger(Quest(QuestNum).Task(I).Order)
            buffer.WriteInteger(Quest(QuestNum).Task(I).Npc)
            buffer.WriteInteger(Quest(QuestNum).Task(I).Item)
            buffer.WriteInteger(Quest(QuestNum).Task(I).Map)
            buffer.WriteInteger(Quest(QuestNum).Task(I).Resource)
            buffer.WriteInteger(Quest(QuestNum).Task(I).Amount)
            buffer.WriteString(Trim(Quest(QuestNum).Task(I).Speech))
            buffer.WriteString(Trim(Quest(QuestNum).Task(I).TaskLog))
            buffer.WriteInteger(Quest(QuestNum).Task(I).QuestEnd)
            buffer.WriteInteger(Quest(QuestNum).Task(I).TaskType)
        Next

        SendData(buffer.ToArray)
        buffer = Nothing

    End Sub

    Sub SendRequestQuests()
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteInteger(ClientPackets.CRequestQuests)
        SendData(buffer.ToArray)
        buffer = Nothing

    End Sub

    Public Sub UpdateQuestLog()
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteInteger(ClientPackets.CQuestLogUpdate)
        SendData(buffer.ToArray)
        buffer = Nothing

    End Sub

    Public Sub PlayerHandleQuest(ByVal QuestNum As Integer, ByVal Order As Integer)
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer

        buffer.WriteInteger(ClientPackets.CPlayerHandleQuest)
        buffer.WriteInteger(QuestNum)
        buffer.WriteInteger(Order) '1=accept quest, 2=cancel quest
        SendData(buffer.ToArray)
        buffer = Nothing
    End Sub

    Public Sub QuestReset(ByVal QuestNum As Integer)
        Dim buffer As ByteBuffer

        buffer = New ByteBuffer
        buffer.WriteInteger(ClientPackets.CQuestReset)
        buffer.WriteInteger(QuestNum)
        SendData(buffer.ToArray)
        buffer = Nothing

    End Sub
#End Region

#Region "Support Functions"

    Public Function GetQuestNum(ByVal QuestName As String) As Integer
        Dim I As Integer
        GetQuestNum = 0

        For I = 1 To MAX_QUESTS
            If Trim$(Quest(I).Name) = Trim$(QuestName) Then
                GetQuestNum = I
                Exit For
            End If
        Next
    End Function

#End Region

#Region "Misc Functions"
    Public Sub LoadRequirement(ByVal QuestNum As Integer, ByVal ReqNum As Integer)

        With frmEditor_Quest
            'Set scrolls to 0 and disable them so they can be enabled when needed
            .scrlItemRec.Value = 0
            .scrlQuestRec.Value = 0
            .scrlClassRec.Value = 0

            .scrlItemRec.Enabled = False
            .scrlQuestRec.Enabled = False
            .scrlClassRec.Enabled = False

            Select Case Quest(QuestNum).Requirement(ReqNum)
                Case 0
                    .rdbNoneReq.Checked = True
                Case 1
                    .rdbItemReq.Checked = True
                    .scrlItemRec.Enabled = True
                    .scrlItemRec.Value = Quest(QuestNum).RequirementIndex(ReqNum)
                Case 2
                    .rdbQuestReq.Checked = True
                    .scrlQuestRec.Enabled = True
                    .scrlQuestRec.Value = Quest(QuestNum).RequirementIndex(ReqNum)
                Case 3
                    .rdbClassReq.Checked = True
                    .scrlClassRec.Enabled = True
                    .scrlClassRec.Value = Quest(QuestNum).RequirementIndex(ReqNum)
            End Select


        End With

    End Sub

    'Subroutine that load the desired task in the form
    Public Sub LoadTask(ByVal QuestNum As Integer, ByVal TaskNum As Integer)
        Dim TaskToLoad As TaskRec
        TaskToLoad = Quest(QuestNum).Task(TaskNum)

        With frmEditor_Quest
            'Load the task type
            Select Case TaskToLoad.Order
                Case 0
                    .optTask0.Checked = True
                Case 1
                    .optTask1.Checked = True
                Case 2
                    .optTask2.Checked = True
                Case 3
                    .optTask3.Checked = True
                Case 4
                    .optTask4.Checked = True
                Case 5
                    .optTask5.Checked = True
                Case 6
                    .optTask6.Checked = True
                Case 7
                    .optTask7.Checked = True
            End Select

            'Load textboxes
            .txtTaskLog.Text = "" & Trim$(TaskToLoad.TaskLog)

            'Set scrolls to 0 and disable them so they can be enabled when needed
            .scrlNPC.Value = 0
            .scrlItem.Value = 0
            .scrlMap.Value = 0
            .scrlResource.Value = 0
            .scrlAmount.Value = 0

            .scrlNPC.Enabled = False
            .scrlItem.Enabled = False
            .scrlMap.Enabled = False
            .scrlResource.Enabled = False
            .scrlAmount.Enabled = False

            If TaskToLoad.QuestEnd = 1 Then
                .chkEnd.Checked = True
            Else
                .chkEnd.Checked = False
            End If

            Select Case TaskToLoad.Order
                Case 0 'Nothing

                Case QUEST_TYPE_GOSLAY '1
                    .scrlNPC.Enabled = True
                    .scrlNPC.Value = TaskToLoad.Npc
                    .scrlAmount.Enabled = True
                    .scrlAmount.Value = TaskToLoad.Amount

                Case QUEST_TYPE_GOGATHER '2
                    .scrlItem.Enabled = True
                    .scrlItem.Value = TaskToLoad.Item
                    .scrlAmount.Enabled = True
                    .scrlAmount.Value = TaskToLoad.Amount

                Case QUEST_TYPE_GOTALK '3
                    .scrlNPC.Enabled = True
                    .scrlNPC.Value = TaskToLoad.Npc

                Case QUEST_TYPE_GOREACH '4
                    .scrlMap.Enabled = True
                    .scrlMap.Value = TaskToLoad.Map

                Case QUEST_TYPE_GOGIVE '5
                    .scrlItem.Enabled = True
                    .scrlItem.Value = TaskToLoad.Item
                    .scrlAmount.Enabled = True
                    .scrlAmount.Value = TaskToLoad.Amount
                    .scrlNPC.Enabled = True
                    .scrlNPC.Value = TaskToLoad.Npc
                    .txtTaskSpeech.Text = "" & Trim$(TaskToLoad.Speech)

                Case QUEST_TYPE_GOTRAIN '6
                    .scrlResource.Enabled = True
                    .scrlResource.Value = TaskToLoad.Resource
                    .scrlAmount.Enabled = True
                    .scrlAmount.Value = TaskToLoad.Amount

                Case QUEST_TYPE_GOGET '7
                    .scrlNPC.Enabled = True
                    .scrlNPC.Value = TaskToLoad.Npc
                    .scrlItem.Enabled = True
                    .scrlItem.Value = TaskToLoad.Item
                    .scrlAmount.Enabled = True
                    .scrlAmount.Value = TaskToLoad.Amount
                    .txtTaskSpeech.Text = "" & Trim$(TaskToLoad.Speech)
            End Select

            .lblTaskNum.Text = "Task Number: " & TaskNum
        End With
    End Sub
#End Region

End Module
