﻿Imports System.Linq
Imports System.Threading
Imports Orion

Module ServerLoop
    Sub ServerLoop()
        Dim Tick As Integer
        Dim tmr25 As Integer, tmr300 As Integer
        Dim tmr500 As Integer, tmr1000 As Integer
        Dim LastUpdateSavePlayers As Integer
        Dim LastUpdateMapSpawnItems As Integer
        Dim LastUpdatePlayerVitals As Integer

        Do
            ' Update our current tick value.
            Tick = GetTimeMs()

            ' Don't process anything else if we're going down.
            If ServerDestroyed Then End

            ' Get all our online players.
            Dim OnlinePlayers = TempPlayer.Where(Function(player) player.InGame).Select(Function(player, index) New With {Key .Index = index + 1, Key .Player = player}).ToArray()

            If Tick > tmr25 Then
                ' Check if any of our players has completed casting and get their skill going if they have.
                Dim _playerskills = (
                    From p In OnlinePlayers
                    Where p.Player.SkillBuffer > 0 AndAlso GetTimeMs() > (p.Player.SkillBufferTimer + Skill(p.Player.SkillBuffer).CastTime * 1000)
                    Select New With {.Index = p.Index, Key .Success = HandleCastSkill(p.Index)}
                ).ToArray()

                ' Check if we need to clear any of our players from being stunned.
                Dim _playerstuns = (
                    From p In OnlinePlayers
                    Where p.Player.StunDuration > 0 AndAlso p.Player.StunTimer + (p.Player.StunDuration * 1000)
                    Select New With {Key .Index = p.Index, Key .Success = HandleClearStun(p.Index)}
                ).ToArray()

                ' Check if any of our pets has completed casting and get their skill going if they have.
                Dim _petskills = (
                From p In OnlinePlayers
                Where Player(p.Index).Character(p.Player.CurChar).Pet.Alive = 1 AndAlso TempPlayer(p.Index).PetskillBuffer.Skill > 0 AndAlso GetTimeMs() > p.Player.PetskillBuffer.Timer + (Skill(Player(p.Index).Character(p.Player.CurChar).Pet.Skill(p.Player.PetskillBuffer.Skill)).CastTime * 1000)
                Select New With {Key .Index = p.Index, Key .Success = HandlePetSkill(p.Index)}
                ).ToArray()

                ' Check if we need to clear any of our pets from being stunned.
                Dim _petstuns = (
                    From p In OnlinePlayers
                    Where p.Player.PetStunDuration > 0 AndAlso p.Player.PetStunTimer + (p.Player.PetStunDuration * 1000)
                    Select New With {Key .Index = p.Index, Key .Success = HandleClearPetStun(p.Index)}
                ).ToArray()

                ' check pet regen timer
                Dim _petregen = (
                    From p In OnlinePlayers
                    Where p.Player.PetstopRegen = True AndAlso p.Player.PetstopRegenTimer + 5000 < GetTimeMs()
                    Select New With {Key .Index = p.Index, Key .Success = HandleStopPetRegen(p.Index)}
                ).ToArray()

                ' HoT and DoT logic
                'For x = 1 To MAX_DOTS
                '    HandleDoT_Pet i, x
                '        HandleHoT_Pet i, x
                '    Next

                ' Update all our available events.
                UpdateEventLogic()

                ' Move the timer up 25ms.
                tmr25 = GetTimeMs() + 25
            End If

            If Tick > tmr1000 Then
                ' Handle our player crafting
                Dim _playercrafts = (
                    From p In OnlinePlayers
                    Where GetTimeMs() > p.Player.CraftTimer + (p.Player.CraftTimeNeeded * 1000) AndAlso p.Player.CraftIt = 1
                    Select New With {Key .Index = p.Index, .Success = HandlePlayerCraft(p.Index)}
                ).ToArray()

                Time.Instance.Tick()

                ' Move the timer up 1000ms.
                tmr1000 = GetTimeMs() + 1000
            End If

            If Tick > tmr500 Then

                ' Handle player housing timers.
                Dim _playerhousing = (
                    From p In OnlinePlayers
                    Where Player(p.Index).Character(p.Player.CurChar).InHouse > 0 AndAlso
                          IsPlaying(Player(p.Index).Character(p.Player.CurChar).InHouse) AndAlso
                          Player(Player(p.Index).Character(p.Player.CurChar).InHouse).Character(p.Player.CurChar).InHouse <> Player(p.Index).Character(p.Player.CurChar).InHouse
                    Select New With {Key .Index = p.Index, Key .Success = HandlePlayerHouse(p.Index)}
                ).ToArray()

                ' Move the timer up 500ms.
                tmr500 = GetTimeMs() + 500

            End If

            If GetTimeMs() > tmr300 Then
                UpdateNpcAI()
                UpdatePetAI()
                tmr300 = GetTimeMs() + 300
            End If

            ' Checks to update player vitals every 5 seconds - Can be tweaked
            If Tick > LastUpdatePlayerVitals Then
                UpdatePlayerVitals()
                LastUpdatePlayerVitals = GetTimeMs() + 5000
            End If

            ' Checks to spawn map items every 5 minutes - Can be tweaked
            If Tick > LastUpdateMapSpawnItems Then
                UpdateMapSpawnItems()
                LastUpdateMapSpawnItems = GetTimeMs() + 300000
            End If

            ' Checks to save players every 10 minutes - Can be tweaked
            If Tick > LastUpdateSavePlayers Then
                UpdateSavePlayers()
                LastUpdateSavePlayers = GetTimeMs() + 600000
            End If

            Application.DoEvents()
            'Thread.Yield()
            Thread.Sleep(1)
        Loop
    End Sub

    'Function GetTotalPlayersOnline() As Integer
    '    GetTotalPlayersOnline = TempPlayer.Where(Function(x) x.InGame).ToArray().Length
    'End Function

    Sub UpdateSavePlayers()
        Dim i As Integer

        If GetPlayersOnline() > 0 Then
            Console.WriteLine("Saving all online players...")
            GlobalMsg("Saving all online players...")

            For i = 1 To GetPlayersOnline()
                If IsPlaying(i) Then
                    SavePlayer(i)
                    SaveBank(i)
                End If
                Application.DoEvents()
            Next

        End If

    End Sub

    Private Sub UpdateMapSpawnItems()
        Dim x As Integer
        Dim y As Integer

        ' ///////////////////////////////////////////
        ' // This is used for respawning map items //
        ' ///////////////////////////////////////////
        For y = 1 To MAX_CACHED_MAPS

            ' Make sure no one is on the map when it respawns
            If Not PlayersOnMap(y) Then

                ' Clear out unnecessary junk
                For x = 1 To MAX_MAP_ITEMS
                    ClearMapItem(x, y)
                Next

                ' Spawn the items
                SpawnMapItems(y)
                SendMapItemsToAll(y)
            End If

            Application.DoEvents()
        Next

    End Sub

    Private Sub UpdatePlayerVitals()
        Dim i As Integer

        For i = 1 To GetPlayersOnline()

            If IsPlaying(i) Then
                If GetPlayerVital(i, VitalType.HP) <> GetPlayerMaxVital(i, VitalType.HP) Then
                    SetPlayerVital(i, VitalType.HP, GetPlayerVital(i, VitalType.HP) + GetPlayerVitalRegen(i, VitalType.HP))
                    SendVital(i, VitalType.HP)
                End If

                If GetPlayerVital(i, VitalType.MP) <> GetPlayerMaxVital(i, VitalType.MP) Then
                    SetPlayerVital(i, VitalType.MP, GetPlayerVital(i, VitalType.MP) + GetPlayerVitalRegen(i, VitalType.MP))
                    SendVital(i, VitalType.MP)
                End If

                If GetPlayerVital(i, VitalType.SP) <> GetPlayerMaxVital(i, VitalType.SP) Then
                    SetPlayerVital(i, VitalType.SP, GetPlayerVital(i, VitalType.SP) + GetPlayerVitalRegen(i, VitalType.SP))
                    SendVital(i, VitalType.SP)
                End If
            End If
            ' send vitals to party if in one
            If TempPlayer(i).InParty > 0 Then SendPartyVitals(TempPlayer(i).InParty, i)
        Next

    End Sub

    Private Sub UpdateNpcAI()
        Dim i As Integer, x As Integer, n As Integer, x1 As Integer, y1 As Integer
        Dim MapNum As Integer, TickCount As Integer
        Dim Damage As Integer
        Dim DistanceX As Integer, DistanceY As Integer
        Dim NpcNum As Integer
        Dim Target As Integer, TargetTypes As Byte, TargetX As Integer, TargetY As Integer, target_verify As Boolean
        Dim Resource_index As Integer

        For MapNum = 1 To MAX_CACHED_MAPS

            If ServerDestroyed Then Exit Sub

            '  Close the doors
            If TickCount > TempTile(MapNum).DoorTimer + 5000 Then

                For x1 = 0 To Map(MapNum).MaxX
                    For y1 = 0 To Map(MapNum).MaxY
                        If Map(MapNum).Tile(x1, y1).Type = TileType.Key AndAlso TempTile(MapNum).DoorOpen(x1, y1) = True Then
                            TempTile(MapNum).DoorOpen(x1, y1) = False
                            SendMapKeyToMap(MapNum, x1, y1, 0)
                        End If
                    Next
                Next

            End If

            ' Respawning Resources
            If ResourceCache Is Nothing Then Exit Sub
            If ResourceCache(MapNum).Resource_Count > 0 Then
                For i = 1 To ResourceCache(MapNum).Resource_Count

                    Resource_index = Map(MapNum).Tile(ResourceCache(MapNum).ResourceData(i).X, ResourceCache(MapNum).ResourceData(i).Y).Data1

                    If Resource_index > 0 Then
                        If ResourceCache(MapNum).ResourceData(i).ResourceState = 1 OrElse ResourceCache(MapNum).ResourceData(i).Cur_Health < 1 Then  ' dead or fucked up
                            If ResourceCache(MapNum).ResourceData(i).ResourceTimer + (Resource(Resource_index).RespawnTime * 1000) < GetTimeMs() Then
                                ResourceCache(MapNum).ResourceData(i).ResourceTimer = GetTimeMs()
                                ResourceCache(MapNum).ResourceData(i).ResourceState = 0 ' normal
                                ' re-set health to resource root
                                ResourceCache(MapNum).ResourceData(i).Cur_Health = Resource(Resource_index).Health
                                SendResourceCacheToMap(MapNum, i)
                            End If
                        End If
                    End If
                Next
            End If

            If ServerDestroyed Then Exit Sub

            If PlayersOnMap(MapNum) = True Then
                TickCount = GetTimeMs()

                For x = 1 To MAX_MAP_NPCS
                    NpcNum = MapNpc(MapNum).Npc(x).Num

                    ' check if they've completed casting, and if so set the actual skill going
                    If MapNpc(MapNum).Npc(x).SkillBuffer > 0 AndAlso Map(MapNum).Npc(x) > 0 AndAlso MapNpc(MapNum).Npc(x).Num > 0 Then
                        If GetTimeMs() > MapNpc(MapNum).Npc(x).SkillBufferTimer + (Skill(Npc(NpcNum).Skill(MapNpc(MapNum).Npc(x).SkillBuffer)).CastTime * 1000) Then
                            CastNpcSkill(x, MapNum, MapNpc(MapNum).Npc(x).SkillBuffer)
                            MapNpc(MapNum).Npc(x).SkillBuffer = 0
                            MapNpc(MapNum).Npc(x).SkillBufferTimer = 0
                        End If

                    Else
                        ' /////////////////////////////////////////
                        ' // This is used for ATTACKING ON SIGHT //
                        ' /////////////////////////////////////////
                        ' Make sure theres a npc with the map
                        If Map(MapNum).Npc(x) > 0 AndAlso MapNpc(MapNum).Npc(x).Num > 0 Then

                            ' If the npc is a attack on sight, search for a player on the map
                            If Npc(NpcNum).Behaviour = NpcBehavior.AttackOnSight OrElse Npc(NpcNum).Behaviour = NpcBehavior.Guard Then

                                ' make sure it's not stunned
                                If Not MapNpc(MapNum).Npc(x).StunDuration > 0 Then

                                    For i = 1 To GetPlayersOnline()
                                        If IsPlaying(i) Then
                                            If GetPlayerMap(i) = MapNum AndAlso MapNpc(MapNum).Npc(x).Target = 0 AndAlso GetPlayerAccess(i) <= AdminType.Monitor Then
                                                If PetAlive(i) Then
                                                    n = Npc(NpcNum).Range
                                                    DistanceX = MapNpc(MapNum).Npc(x).X - Player(i).Character(TempPlayer(i).CurChar).Pet.X
                                                    DistanceY = MapNpc(MapNum).Npc(x).Y - Player(i).Character(TempPlayer(i).CurChar).Pet.Y

                                                    ' Make sure we get a positive value
                                                    If DistanceX < 0 Then DistanceX = DistanceX * -1
                                                    If DistanceY < 0 Then DistanceY = DistanceY * -1

                                                    ' Are they in range?  if so GET'M!
                                                    If DistanceX <= n AndAlso DistanceY <= n Then
                                                        If Npc(NpcNum).Behaviour = NpcBehavior.AttackOnSight OrElse GetPlayerPK(i) = i Then
                                                            If Len(Trim$(Npc(NpcNum).AttackSay)) > 0 Then
                                                                PlayerMsg(i, Trim$(Npc(NpcNum).Name) & " says: " & Trim$(Npc(NpcNum).AttackSay), QColorType.SayColor)
                                                            End If
                                                            MapNpc(MapNum).Npc(x).TargetType = TargetType.Pet
                                                            MapNpc(MapNum).Npc(x).Target = i
                                                        End If
                                                    End If
                                                Else
                                                    n = Npc(NpcNum).Range
                                                    DistanceX = MapNpc(MapNum).Npc(x).X - GetPlayerX(i)
                                                    DistanceY = MapNpc(MapNum).Npc(x).Y - GetPlayerY(i)

                                                    ' Make sure we get a positive value
                                                    If DistanceX < 0 Then DistanceX = DistanceX * -1
                                                    If DistanceY < 0 Then DistanceY = DistanceY * -1

                                                    ' Are they in range?  if so GET'M!
                                                    If DistanceX <= n AndAlso DistanceY <= n Then
                                                        If Npc(NpcNum).Behaviour = NpcBehavior.AttackOnSight OrElse GetPlayerPK(i) = True Then
                                                            If Len(Trim$(Npc(NpcNum).AttackSay)) > 0 Then
                                                                PlayerMsg(i, CheckGrammar(Trim$(Npc(NpcNum).Name), 1) & " says, '" & Trim$(Npc(NpcNum).AttackSay) & "' to you.", ColorType.Yellow)
                                                            End If
                                                            MapNpc(MapNum).Npc(x).TargetType = TargetType.Player
                                                            MapNpc(MapNum).Npc(x).Target = i
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        End If
                                    Next

                                    ' Check if target was found for NPC targetting
                                    If MapNpc(MapNum).Npc(x).Target = 0 AndAlso Npc(NpcNum).Faction > 0 Then
                                        ' search for npc of another faction to target
                                        For i = 1 To MAX_MAP_NPCS
                                            ' exist?
                                            If MapNpc(MapNum).Npc(i).Num > 0 Then
                                                ' different faction?
                                                If Npc(MapNpc(MapNum).Npc(i).Num).Faction > 0 AndAlso Npc(MapNpc(MapNum).Npc(i).Num).Faction <> Npc(NpcNum).Faction Then
                                                    n = Npc(NpcNum).Range
                                                    DistanceX = MapNpc(MapNum).Npc(x).X - CLng(MapNpc(MapNum).Npc(i).X)
                                                    DistanceY = MapNpc(MapNum).Npc(x).Y - CLng(MapNpc(MapNum).Npc(i).Y)

                                                    ' Make sure we get a positive value
                                                    If DistanceX < 0 Then DistanceX = DistanceX * -1
                                                    If DistanceY < 0 Then DistanceY = DistanceY * -1

                                                    ' Are they in range?  if so GET'M!
                                                    If DistanceX <= n AndAlso DistanceY <= n AndAlso Npc(NpcNum).Behaviour = NpcBehavior.AttackOnSight Then
                                                        MapNpc(MapNum).Npc(x).TargetType = 2 ' npc
                                                        MapNpc(MapNum).Npc(x).Target = i
                                                    End If
                                                End If
                                            End If
                                        Next
                                    End If
                                End If
                            End If
                        End If

                        target_verify = False

                        ' /////////////////////////////////////////////
                        ' // This is used for NPC walking/targetting //
                        ' /////////////////////////////////////////////
                        ' Make sure theres a npc with the map
                        If Map(MapNum).Npc(x) > 0 AndAlso MapNpc(MapNum).Npc(x).Num > 0 Then
                            If MapNpc(MapNum).Npc(x).StunDuration > 0 Then
                                ' check if we can unstun them
                                If GetTimeMs() > MapNpc(MapNum).Npc(x).StunTimer + (MapNpc(MapNum).Npc(x).StunDuration * 1000) Then
                                    MapNpc(MapNum).Npc(x).StunDuration = 0
                                    MapNpc(MapNum).Npc(x).StunTimer = 0
                                End If
                            Else

                                Target = MapNpc(MapNum).Npc(x).Target
                                TargetTypes = MapNpc(MapNum).Npc(x).TargetType

                                ' Check to see if its time for the npc to walk
                                If Npc(NpcNum).Behaviour <> NpcBehavior.ShopKeeper AndAlso Npc(NpcNum).Behaviour <> NpcBehavior.Quest Then
                                    If TargetTypes = TargetType.Player Then ' player
                                        ' Check to see if we are following a player or not
                                        If Target > 0 Then
                                            ' Check if the player is even playing, if so follow'm
                                            If IsPlaying(Target) AndAlso GetPlayerMap(Target) = MapNum Then
                                                target_verify = True
                                                TargetY = GetPlayerY(Target)
                                                TargetX = GetPlayerX(Target)
                                            Else
                                                MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                                MapNpc(MapNum).Npc(x).Target = 0
                                            End If
                                        End If
                                    ElseIf TargetTypes = TargetType.Npc Then 'npc
                                        If Target > 0 Then
                                            If MapNpc(MapNum).Npc(Target).Num > 0 Then
                                                target_verify = True
                                                TargetY = MapNpc(MapNum).Npc(Target).Y
                                                TargetX = MapNpc(MapNum).Npc(Target).X
                                            Else
                                                MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                                MapNpc(MapNum).Npc(x).Target = 0
                                            End If
                                        End If
                                    ElseIf TargetTypes = TargetType.Pet Then
                                        If Target > 0 Then
                                            If IsPlaying(Target) = True AndAlso GetPlayerMap(Target) = MapNum AndAlso PetAlive(Target) Then
                                                target_verify = True
                                                TargetY = Player(Target).Character(TempPlayer(Target).CurChar).Pet.Y
                                                TargetX = Player(Target).Character(TempPlayer(Target).CurChar).Pet.X
                                            Else
                                                MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                                MapNpc(MapNum).Npc(x).Target = 0
                                            End If
                                        End If
                                    End If

                                    If target_verify Then
                                        'Gonna make the npcs smarter.. Implementing a pathfinding algorithm.. we shall see what happens.
                                        If IsOneBlockAway(TargetX, TargetY, CLng(MapNpc(MapNum).Npc(x).X), CLng(MapNpc(MapNum).Npc(x).Y)) = False Then

                                            i = FindNpcPath(MapNum, x, TargetX, TargetY)
                                            If i < 4 Then 'Returned an answer. Move the NPC
                                                If CanNpcMove(MapNum, x, i) Then
                                                    NpcMove(MapNum, x, i, MovementType.Walking)
                                                End If
                                            Else 'No good path found. Move randomly
                                                i = Int(Rnd() * 4)
                                                If i = 1 Then
                                                    i = Int(Rnd() * 4)

                                                    If CanNpcMove(MapNum, x, i) Then
                                                        NpcMove(MapNum, x, i, MovementType.Walking)
                                                    End If
                                                End If
                                            End If
                                        Else
                                            NpcDir(MapNum, x, GetNpcDir(TargetX, TargetY, CLng(MapNpc(MapNum).Npc(x).X), CLng(MapNpc(MapNum).Npc(x).Y)))
                                        End If
                                    Else
                                        i = Int(Rnd() * 4)

                                        If i = 1 Then
                                            i = Int(Rnd() * 4)

                                            If CanNpcMove(MapNum, x, i) Then
                                                NpcMove(MapNum, x, i, MovementType.Walking)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        End If

                    End If

                    ' /////////////////////////////////////////////
                    ' // This is used for npcs to attack targets //
                    ' /////////////////////////////////////////////
                    ' Make sure theres a npc with the map
                    If Map(MapNum).Npc(x) > 0 AndAlso MapNpc(MapNum).Npc(x).Num > 0 Then
                        Target = MapNpc(MapNum).Npc(x).Target
                        TargetTypes = MapNpc(MapNum).Npc(x).TargetType

                        ' Check if the npc can attack the targeted player player
                        If Target > 0 Then

                            If TargetTypes = TargetType.Player Then ' player

                                ' Is the target playing and on the same map?
                                If IsPlaying(Target) AndAlso GetPlayerMap(Target) = MapNum Then
                                    If IsPlaying(Target) AndAlso GetPlayerMap(Target) = MapNum Then
                                        If Random(1, 3) = 1 Then
                                            Dim skillnum As Byte = RandomNpcAttack(MapNum, x)
                                            If skillnum > 0 Then
                                                BufferNpcSkill(MapNum, x, skillnum)
                                            Else
                                                TryNpcAttackPlayer(x, Target) ', Damage)
                                            End If
                                        Else
                                            TryNpcAttackPlayer(x, Target)
                                        End If
                                    Else
                                        ' Player left map or game, set target to 0
                                        MapNpc(MapNum).Npc(x).Target = 0
                                        MapNpc(MapNum).Npc(x).TargetType = 0 ' clear

                                    End If
                                Else
                                    ' Player left map or game, set target to 0
                                    MapNpc(MapNum).Npc(x).Target = 0
                                    MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                End If
                            ElseIf TargetTypes = TargetType.Npc Then
                                If MapNpc(MapNum).Npc(Target).Num > 0 Then ' npc exists
                                    'Can the npc attack the npc?
                                    If CanNpcAttackNpc(MapNum, x, Target) Then
                                        Damage = Npc(NpcNum).Stat(StatType.Strength) - CLng(Npc(Target).Stat(StatType.Endurance))
                                        If Damage < 1 Then Damage = 1
                                        NpcAttackNpc(MapNum, x, Target, Damage)
                                    End If
                                Else
                                    ' npc is dead or non-existant
                                    MapNpc(MapNum).Npc(x).Target = 0
                                    MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                End If
                            ElseIf TargetTypes = TargetType.Pet Then
                                If IsPlaying(Target) AndAlso GetPlayerMap(Target) = MapNum AndAlso PetAlive(Target) Then
                                    TryNpcAttackPet(x, Target)
                                Else
                                    ' Player left map or game, set target to 0
                                    MapNpc(MapNum).Npc(x).Target = 0
                                    MapNpc(MapNum).Npc(x).TargetType = 0 ' clear
                                End If
                            End If
                        End If
                    End If

                    ' ////////////////////////////////////////////
                    ' // This is used for regenerating NPC's HP //
                    ' ////////////////////////////////////////////
                    ' Check to see if we want to regen some of the npc's hp
                    If MapNpc(MapNum).Npc(x).Num > 0 AndAlso TickCount > GiveNPCHPTimer + 10000 Then
                        If MapNpc(MapNum).Npc(x).Vital(VitalType.HP) > 0 Then
                            MapNpc(MapNum).Npc(x).Vital(VitalType.HP) = MapNpc(MapNum).Npc(x).Vital(VitalType.HP) + GetNpcVitalRegen(NpcNum, VitalType.HP)

                            ' Check if they have more then they should and if so just set it to max
                            If MapNpc(MapNum).Npc(x).Vital(VitalType.HP) > GetNpcMaxVital(NpcNum, VitalType.HP) Then
                                MapNpc(MapNum).Npc(x).Vital(VitalType.HP) = GetNpcMaxVital(NpcNum, VitalType.HP)
                            End If
                        End If
                    End If

                    If MapNpc(MapNum).Npc(x).Num > 0 AndAlso TickCount > GiveNPCMPTimer + 10000 AndAlso MapNpc(MapNum).Npc(x).Vital(VitalType.MP) > 0 Then
                        MapNpc(MapNum).Npc(x).Vital(VitalType.MP) = MapNpc(MapNum).Npc(x).Vital(VitalType.MP) + GetNpcVitalRegen(NpcNum, VitalType.MP)

                        ' Check if they have more then they should and if so just set it to max
                        If MapNpc(MapNum).Npc(x).Vital(VitalType.MP) > GetNpcMaxVital(NpcNum, VitalType.MP) Then
                            MapNpc(MapNum).Npc(x).Vital(VitalType.MP) = GetNpcMaxVital(NpcNum, VitalType.MP)
                        End If
                    End If

                    ' ////////////////////////////////////////////////////////
                    ' // This is used for checking if an NPC is dead or not //
                    ' ////////////////////////////////////////////////////////
                    ' Check if the npc is dead or not
                    If MapNpc(MapNum).Npc(x).Num > 0 AndAlso MapNpc(MapNum).Npc(x).Vital(VitalType.HP) <= 0 Then
                        MapNpc(MapNum).Npc(x).Num = 0
                        MapNpc(MapNum).Npc(x).SpawnWait = GetTimeMs()
                        MapNpc(MapNum).Npc(x).Vital(VitalType.HP) = 0
                    End If

                    ' //////////////////////////////////////
                    ' // This is used for spawning an NPC //
                    ' //////////////////////////////////////
                    ' Check if we are supposed to spawn an npc or not
                    If MapNpc(MapNum).Npc(x).Num = 0 AndAlso Map(MapNum).Npc(x) > 0 Then
                        If TickCount > MapNpc(MapNum).Npc(x).SpawnWait + (Npc(Map(MapNum).Npc(x)).SpawnSecs * 1000) Then
                            SpawnNpc(x, MapNum)
                        End If
                    End If
                Next
            End If

        Next

        ' Make sure we reset the timer for npc hp regeneration
        If GetTimeMs() > GiveNPCHPTimer + 10000 Then GiveNPCHPTimer = GetTimeMs()

        If GetTimeMs() > GiveNPCMPTimer + 10000 Then GiveNPCMPTimer = GetTimeMs()

        ' Make sure we reset the timer for door closing
        If GetTimeMs() > KeyTimer + 15000 Then KeyTimer = GetTimeMs()

    End Sub

    Function GetNpcVitalRegen(NpcNum As Integer, Vital As VitalType) As Integer
        Dim i As Integer

        'Prevent subscript out of range
        If NpcNum <= 0 OrElse NpcNum > MAX_NPCS Then
            GetNpcVitalRegen = 0
            Exit Function
        End If

        Select Case Vital
            Case VitalType.HP
                i = Npc(NpcNum).Stat(StatType.Vitality) \ 3

                If i < 1 Then i = 1
                GetNpcVitalRegen = i
            Case VitalType.MP
                i = Npc(NpcNum).Stat(StatType.Intelligence) \ 3

                If i < 1 Then i = 1
                GetNpcVitalRegen = i
        End Select

    End Function

    Friend Function HandleCloseSocket(Index As Integer) As Boolean
        Socket.Disconnect(Index)
        HandleCloseSocket = True
    End Function

    Friend Function HandlePlayerHouse(Index As Integer) As Boolean
        Player(Index).Character(TempPlayer(Index).CurChar).InHouse = 0
        PlayerWarp(Index, Player(Index).Character(TempPlayer(Index).CurChar).LastMap, Player(Index).Character(TempPlayer(Index).CurChar).LastX, Player(Index).Character(TempPlayer(Index).CurChar).LastY)
        PlayerMsg(Index, "Your visitation has ended. Possibly due to a disconnection. You are being warped back to your previous location.", ColorType.Yellow)
        HandlePlayerHouse = True
    End Function

    Friend Function HandlePetSkill(Index As Integer) As Boolean
        PetCastSkill(Index, TempPlayer(Index).PetskillBuffer.Skill, TempPlayer(Index).PetskillBuffer.Target, TempPlayer(Index).PetskillBuffer.TargetTypes, True)
        TempPlayer(Index).PetskillBuffer.Skill = 0
        TempPlayer(Index).PetskillBuffer.Timer = 0
        TempPlayer(Index).PetskillBuffer.Target = 0
        TempPlayer(Index).PetskillBuffer.TargetTypes = 0
        HandlePetSkill = True
    End Function

    Friend Function HandlePlayerCraft(Index As Integer) As Boolean
        TempPlayer(Index).CraftIt = 0
        TempPlayer(Index).CraftTimer = 0
        TempPlayer(Index).CraftTimeNeeded = 0
        UpdateCraft(Index)
        HandlePlayerCraft = True
    End Function

    Friend Function HandleClearStun(Index As Integer) As Boolean
        TempPlayer(Index).StunDuration = 0
        TempPlayer(Index).StunTimer = 0
        SendStunned(Index)
        HandleClearStun = True
    End Function

    Friend Function HandleClearPetStun(Index As Integer) As Boolean
        TempPlayer(Index).PetStunDuration = 0
        TempPlayer(Index).PetStunTimer = 0
        HandleClearPetStun = True
    End Function

    Friend Function HandleStopPetRegen(Index As Integer) As Boolean
        TempPlayer(Index).PetstopRegen = False
        TempPlayer(Index).PetstopRegenTimer = 0
        HandleStopPetRegen = True
    End Function

    Friend Function HandleCastSkill(Index As Integer) As Boolean
        CastSkill(Index, TempPlayer(Index).SkillBuffer)
        TempPlayer(Index).SkillBuffer = 0
        TempPlayer(Index).SkillBufferTimer = 0
        HandleCastSkill = True
    End Function

    Friend Sub CastSkill(Index As Integer, SkillSlot As Integer)
        ' Set up some basic variables we'll be using.
        Dim SkillId = GetPlayerSkill(Index, SkillSlot)

        ' Preventative checks
        If Not IsPlaying(Index) OrElse SkillSlot <= 0 OrElse SkillSlot > MAX_PLAYER_SKILLS OrElse Not HasSkill(Index, SkillId) Then Exit Sub

        ' Check if the player is able to cast the spell.
        If GetPlayerVital(Index, VitalType.MP) < Skill(SkillId).MpCost Then
            PlayerMsg(Index, "Not enough mana!", ColorType.BrightRed)
            Exit Sub
        ElseIf GetPlayerLevel(Index) < Skill(SkillId).LevelReq Then
            PlayerMsg(Index, String.Format("You must be level {0} to use this skill.", Skill(SkillId).LevelReq), ColorType.BrightRed)
            Exit Sub
        ElseIf GetPlayerAccess(Index) < Skill(SkillId).AccessReq Then
            PlayerMsg(Index, "You must be an administrator to use this skill.", ColorType.BrightRed)
            Exit Sub
        ElseIf Not Skill(SkillId).ClassReq = 0 AndAlso GetPlayerClass(Index) <> Skill(SkillId).ClassReq Then
            PlayerMsg(Index, String.Format("Only {0} can use this skill.", CheckGrammar((Classes(Skill(SkillId).ClassReq).Name.Trim()))), ColorType.BrightRed)
            Exit Sub
        ElseIf Skill(SkillId).Range > 0 AndAlso Not IsTargetOnMap(Index) Then
            Exit Sub
        ElseIf Skill(SkillId).Range > 0 AndAlso Not IsInSkillRange(Index, SkillId) AndAlso Skill(SkillId).IsProjectile = 0 Then
            PlayerMsg(Index, "Target not in range.", ColorType.BrightRed)
            SendClearSkillBuffer(Index)
            Exit Sub
        End If

        ' Determine what kind of Skill Type we're dealing with and move on to the appropriate methods.
        If Skill(SkillId).IsProjectile = 1 Then
            PlayerFireProjectile(Index, SkillId)
        Else
            If Skill(SkillId).Range = 0 AndAlso Not Skill(SkillId).IsAoE Then HandleSelfCastSkill(Index, SkillId)
            If Skill(SkillId).Range = 0 AndAlso Skill(SkillId).IsAoE Then HandleSelfCastAoESkill(Index, SkillId)
            If Skill(SkillId).Range > 0 AndAlso Skill(SkillId).IsAoE Then HandleTargetedAoESkill(Index, SkillId)
            If Skill(SkillId).Range > 0 AndAlso Not Skill(SkillId).IsAoE Then HandleTargetedSkill(Index, SkillId)
        End If

        ' Do everything we need to do at the end of the cast.
        FinalizeCast(Index, GetPlayerSkillSlot(Index, SkillId), Skill(SkillId).MpCost)
    End Sub

    Private Sub HandleSelfCastAoESkill(Index As Integer, SkillId As Integer)

        ' Set up some variables we'll definitely be using.
        Dim CenterX = GetPlayerX(Index)
        Dim CenterY = GetPlayerY(Index)

        ' Determine what kind of spell we're dealing with and process it.
        Select Case Skill(SkillId).Type
            Case SkillType.DamageHp, SkillType.DamageMp, SkillType.HealHp, SkillType.HealMp
                HandleAoE(Index, SkillId, CenterX, CenterY)

            Case Else
                Throw New NotImplementedException()
        End Select

    End Sub

    Private Sub HandleTargetedAoESkill(Index As Integer, SkillId As Integer)

        ' Set up some variables we'll definitely be using.
        Dim CenterX As Integer
        Dim CenterY As Integer
        Dim TargetType As TargetType
        Select Case TempPlayer(Index).TargetType
            Case TargetType.Npc
                TargetType = TargetType.Npc
                CenterX = MapNpc(GetPlayerMap(Index)).Npc(TempPlayer(Index).Target).X
                CenterY = MapNpc(GetPlayerMap(Index)).Npc(TempPlayer(Index).Target).Y

            Case TargetType.Player
                TargetType = TargetType.Player
                CenterX = GetPlayerX(TempPlayer(Index).Target)
                CenterY = GetPlayerY(TempPlayer(Index).Target)

            Case Else
                Throw New NotImplementedException()

        End Select

        ' Determine what kind of spell we're dealing with and process it.
        Select Case Skill(SkillId).Type
            Case SkillType.HealMp, SkillType.DamageHp, SkillType.DamageMp, SkillType.HealHp
                HandleAoE(Index, SkillId, CenterX, CenterY)

            Case Else
                Throw New NotImplementedException()
        End Select
    End Sub

    Private Sub HandleSelfCastSkill(Index As Integer, SkillId As Integer)
        ' Determine what kind of spell we're dealing with and process it.
        Select Case Skill(SkillId).Type
            Case SkillType.HealHp
                SkillPlayer_Effect(VitalType.HP, True, Index, Skill(SkillId).Vital, SkillId)
            Case SkillType.HealMp
                SkillPlayer_Effect(VitalType.MP, True, Index, Skill(SkillId).Vital, SkillId)
            Case SkillType.Warp
                SendAnimation(GetPlayerMap(Index), Skill(SkillId).SkillAnim, 0, 0, Enums.TargetType.Player, Index)
                PlayerWarp(Index, Skill(SkillId).Map, Skill(SkillId).X, Skill(SkillId).Y)
            Case Else
                Throw New NotImplementedException()
        End Select

        ' Play our animation.
        SendAnimation(GetPlayerMap(Index), Skill(SkillId).SkillAnim, 0, 0, Enums.TargetType.Player, Index)
    End Sub

    Private Sub HandleTargetedSkill(Index As Integer, SkillId As Integer)
        ' Set up some variables we'll definitely be using.
        Dim Vital As VitalType
        Dim DealsDamage As Boolean
        Dim Amount = Skill(SkillId).Vital
        Dim Target = TempPlayer(Index).Target

        ' Determine what vital we need to adjust and how.
        Select Case Skill(SkillId).Type
            Case SkillType.DamageHp
                Vital = VitalType.HP
                DealsDamage = True

            Case SkillType.DamageMp
                Vital = VitalType.MP
                DealsDamage = True

            Case SkillType.HealHp
                Vital = VitalType.HP
                DealsDamage = False

            Case SkillType.HealMp
                Vital = VitalType.MP
                DealsDamage = False

            Case Else
                Throw New NotImplementedException
        End Select

        Select Case TempPlayer(Index).TargetType
            Case TargetType.Npc
                ' Deal with damaging abilities.
                If DealsDamage AndAlso CanPlayerAttackNpc(Index, Target, True) Then SkillNpc_Effect(Vital, False, Target, Amount, SkillId, GetPlayerMap(Index))

                ' Deal with healing abilities
                If Not DealsDamage Then SkillNpc_Effect(Vital, True, Target, Amount, SkillId, GetPlayerMap(Index))

                ' Handle our NPC death if it kills them
                If IsNpcDead(GetPlayerMap(Index), TempPlayer(Index).Target) Then
                    HandlePlayerKillNpc(GetPlayerMap(Index), Index, TempPlayer(Index).Target)
                End If

            Case TargetType.Player

                ' Deal with damaging abilities.
                If DealsDamage AndAlso CanPlayerAttackPlayer(Index, Target, True) Then SkillPlayer_Effect(Vital, False, Target, Amount, SkillId)

                ' Deal with healing abilities
                If Not DealsDamage Then SkillPlayer_Effect(Vital, True, Target, Amount, SkillId)

                If IsPlayerDead(Target) Then
                    ' Actually kill the player.
                    OnDeath(Target)

                    ' Handle PK stuff.
                    HandlePlayerKilledPK(Index, Target)

                    ' Handle our quest system stuff.
                    CheckTasks(Index, QuestType.Kill, 0)
                End If
            Case Else
                Throw New NotImplementedException()

        End Select

        ' Play our animation.
        SendAnimation(GetPlayerMap(Index), Skill(SkillId).SkillAnim, 0, 0, TempPlayer(Index).TargetType, Target)
    End Sub

    Private Sub HandleAoE(Index As Integer, SkillId As Integer, X As Integer, Y As Integer)
        ' Get some basic things set up.
        Dim Map = GetPlayerMap(Index)
        Dim Range = Skill(SkillId).Range
        Dim Amount = Skill(SkillId).Vital
        Dim Vital As Enums.VitalType
        Dim DealsDamage As Boolean

        ' Determine what vital we need to adjust and how.
        Select Case Skill(SkillId).Type
            Case SkillType.DamageHp
                Vital = VitalType.HP
                DealsDamage = True

            Case SkillType.DamageMp
                Vital = VitalType.MP
                DealsDamage = True

            Case SkillType.HealHp
                Vital = VitalType.HP
                DealsDamage = False

            Case SkillType.HealMp
                Vital = VitalType.MP
                DealsDamage = False

            Case Else
                Throw New NotImplementedException
        End Select

        ' Loop through all online players on the current map.
        For Each id In TempPlayer.Where(Function(p) p.InGame).Select(Function(p, i) i + 1).Where(Function(i) GetPlayerMap(i) = Map AndAlso i <> Index).ToArray()
            If IsInRange(Range, X, Y, GetPlayerX(id), GetPlayerY(id)) Then

                ' Deal with damaging abilities.
                If DealsDamage AndAlso CanPlayerAttackPlayer(Index, id, True) Then SkillPlayer_Effect(Vital, False, id, Amount, SkillId)

                ' Deal with healing abilities
                If Not DealsDamage Then SkillPlayer_Effect(Vital, True, id, Amount, SkillId)

                ' Send our animation to the map.
                SendAnimation(Map, Skill(SkillId).SkillAnim, 0, 0, Enums.TargetType.Player, id)

                If IsPlayerDead(id) Then
                    ' Actually kill the player.
                    OnDeath(id)

                    ' Handle PK stuff.
                    HandlePlayerKilledPK(Index, id)

                    ' Handle our quest system stuff.
                    CheckTasks(Index, QuestType.Kill, 0)
                End If
            End If
        Next

        ' Loop through all the NPCs on this map
        For Each id In MapNpc(Map).Npc.Where(Function(n) n.Num > 0 AndAlso n.Vital(VitalType.HP) > 0).Select(Function(n, i) i + 1).ToArray()
            If IsInRange(Range, X, Y, MapNpc(Map).Npc(id).X, MapNpc(Map).Npc(id).Y) Then

                ' Deal with damaging abilities.
                If DealsDamage AndAlso CanPlayerAttackNpc(Index, id, True) Then SkillNpc_Effect(Vital, False, id, Amount, SkillId, Map)

                ' Deal with healing abilities
                If Not DealsDamage Then SkillNpc_Effect(Vital, True, id, Amount, SkillId, Map)

                ' Send our animation to the map.
                SendAnimation(Map, Skill(SkillId).SkillAnim, 0, 0, Enums.TargetType.Npc, id)

                ' Handle our NPC death if it kills them
                If IsNpcDead(Map, id) Then
                    HandlePlayerKillNpc(Map, Index, id)
                End If
            End If
        Next
    End Sub

    Private Sub FinalizeCast(Index As Integer, SkillSlot As Integer, SkillCost As Integer)
        SetPlayerVital(Index, VitalType.MP, GetPlayerVital(Index, VitalType.MP) - SkillCost)
        SendVital(Index, VitalType.MP)
        TempPlayer(Index).SkillCD(SkillSlot) = GetTimeMs() + (Skill(SkillSlot).CdTime * 1000)
        SendCooldown(Index, SkillSlot)
    End Sub

    Private Function IsTargetOnMap(Index As Integer) As Boolean
        If TempPlayer(Index).TargetType = TargetType.Player Then
            If GetPlayerMap(TempPlayer(Index).Target) = GetPlayerMap(Index) Then IsTargetOnMap = True
        ElseIf TempPlayer(Index).TargetType = TargetType.Npc Then
            If TempPlayer(Index).Target > 0 AndAlso TempPlayer(Index).Target <= MAX_MAP_NPCS AndAlso MapNpc(GetPlayerMap(Index)).Npc(TempPlayer(Index).Target).Vital(VitalType.HP) > 0 Then IsTargetOnMap = True
        End If
    End Function

    Private Function IsInSkillRange(Index As Integer, SkillId As Integer) As Boolean
        Dim TargetX As Integer
        Dim TargetY As Integer

        If TempPlayer(Index).TargetType = TargetType.Player Then
            TargetX = GetPlayerX(TempPlayer(Index).Target)
            TargetY = GetPlayerY(TempPlayer(Index).Target)
        ElseIf TempPlayer(Index).TargetType = TargetType.Npc Then
            TargetX = MapNpc(GetPlayerMap(Index)).Npc(TempPlayer(Index).Target).X
            TargetY = MapNpc(GetPlayerMap(Index)).Npc(TempPlayer(Index).Target).Y
        End If

        IsInSkillRange = IsInRange(Skill(SkillId).Range, GetPlayerX(Index), GetPlayerY(Index), TargetX, TargetY)
    End Function

    Friend Sub CastNpcSkill(NpcNum As Integer, MapNum As Integer, skillslot As Integer)
        Dim skillnum As Integer, MPCost As Integer
        Dim Vital As Integer, DidCast As Boolean
        Dim i As Integer
        Dim AoE As Integer, range As Integer, VitalType As Byte
        Dim increment As Boolean, x As Integer, y As Integer

        Dim TargetType As Byte
        Dim Target As Integer
        Dim SkillCastType As Integer

        DidCast = False

        ' Prevent subscript out of range
        If skillslot <= 0 OrElse skillslot > MAX_NPC_SKILLS Then Exit Sub

        skillnum = GetNpcSkill(MapNpc(MapNum).Npc(NpcNum).Num, skillslot)

        MPCost = Skill(skillnum).MpCost

        ' Check if they have enough MP
        If MapNpc(MapNum).Npc(NpcNum).Vital(Enums.VitalType.MP) < MPCost Then Exit Sub

        ' find out what kind of skill it is! self cast, target or AOE
        If Skill(skillnum).IsProjectile = 1 Then
            SkillCastType = 4 ' Projectile
        ElseIf Skill(skillnum).Range > 0 Then
            ' ranged attack, single target or aoe?
            If Not Skill(skillnum).IsAoE Then
                SkillCastType = 2 ' targetted
            Else
                SkillCastType = 3 ' targetted aoe
            End If
        Else
            If Not Skill(skillnum).IsAoE Then
                SkillCastType = 0 ' self-cast
            Else
                SkillCastType = 1 ' self-cast AoE
            End If
        End If

        ' set the vital
        Vital = Skill(skillnum).Vital
        AoE = Skill(skillnum).AoE
        range = Skill(skillnum).Range

        Select Case SkillCastType
            Case 0 ' self-cast target
                'Select Case Skill(skillnum).Type
                '    Case SkillType.HEALHP
                '        SkillPlayer_Effect(VitalType.HP, True, NpcNum, Vital, skillnum)
                '        DidCast = True
                '    Case SkillType.HEALMP
                '        SkillPlayer_Effect(VitalType.MP, True, NpcNum, Vital, skillnum)
                '        DidCast = True
                '    Case SkillType.WARP
                '        SendAnimation(MapNum, Skill(skillnum).SkillAnim, 0, 0, TargetType.PLAYER, NpcNum)
                '        PlayerWarp(NpcNum, Skill(skillnum).Map, Skill(skillnum).x, Skill(skillnum).y)
                '        SendAnimation(GetPlayerMap(NpcNum), Skill(skillnum).SkillAnim, 0, 0, TargetType.PLAYER, NpcNum)
                '        DidCast = True
                'End Select

            Case 1, 3 ' self-cast AOE & targetted AOE
                If SkillCastType = 1 Then
                    x = MapNpc(MapNum).Npc(NpcNum).X
                    y = MapNpc(MapNum).Npc(NpcNum).Y
                ElseIf SkillCastType = 3 Then
                    TargetType = MapNpc(MapNum).Npc(NpcNum).TargetType
                    Target = MapNpc(MapNum).Npc(NpcNum).Target

                    If TargetType = 0 Then Exit Sub
                    If Target = 0 Then Exit Sub

                    If TargetType = Enums.TargetType.Player Then
                        x = GetPlayerX(Target)
                        y = GetPlayerY(Target)
                    Else
                        x = MapNpc(MapNum).Npc(Target).X
                        y = MapNpc(MapNum).Npc(Target).Y
                    End If

                    If Not IsInRange(range, x, y, GetPlayerX(NpcNum), GetPlayerY(NpcNum)) Then
                        Exit Sub
                    End If
                End If
                Select Case Skill(skillnum).Type
                    Case SkillType.DamageHp
                        DidCast = True
                        For i = 1 To GetPlayersOnline()
                            If IsPlaying(i) Then
                                If GetPlayerMap(i) = MapNum Then
                                    If IsInRange(AoE, x, y, GetPlayerX(i), GetPlayerY(i)) Then
                                        If CanNpcAttackPlayer(NpcNum, i) Then
                                            SendAnimation(MapNum, Skill(skillnum).SkillAnim, 0, 0, Enums.TargetType.Player, i)
                                            PlayerMsg(i, Trim(Npc(MapNpc(MapNum).Npc(NpcNum).Num).Name) & " uses " & Trim(Skill(skillnum).Name) & "!", ColorType.Yellow)
                                            SkillPlayer_Effect(Enums.VitalType.HP, False, i, Vital, skillnum)
                                        End If
                                    End If
                                End If
                            End If
                        Next
                        For i = 1 To MAX_MAP_NPCS
                            If MapNpc(MapNum).Npc(i).Num > 0 Then
                                If MapNpc(MapNum).Npc(i).Vital(Enums.VitalType.HP) > 0 Then
                                    If IsInRange(AoE, x, y, MapNpc(MapNum).Npc(i).X, MapNpc(MapNum).Npc(i).Y) Then
                                        If CanPlayerAttackNpc(NpcNum, i, True) Then
                                            SendAnimation(MapNum, Skill(skillnum).SkillAnim, 0, 0, Enums.TargetType.Npc, i)
                                            SkillNpc_Effect(Enums.VitalType.HP, False, i, Vital, skillnum, MapNum)
                                            If Global.OrionServer.Types.Skill(skillnum).KnockBack = 1 Then
                                                KnockBackNpc(NpcNum, Target, skillnum)
                                            End If
                                        End If
                                    End If
                                End If
                            End If
                        Next
                    Case SkillType.HealHp, SkillType.HealMp, SkillType.DamageMp
                        If Skill(skillnum).Type = SkillType.HealHp Then
                            VitalType = Enums.VitalType.HP
                            increment = True
                        ElseIf Skill(skillnum).Type = SkillType.HealMp Then
                            VitalType = Enums.VitalType.MP
                            increment = True
                        ElseIf Skill(skillnum).Type = SkillType.DamageMp Then
                            VitalType = Enums.VitalType.MP
                            increment = False
                        End If

                        DidCast = True
                        For i = 1 To GetPlayersOnline()
                            If IsPlaying(i) AndAlso GetPlayerMap(i) = GetPlayerMap(NpcNum) Then
                                If IsInRange(AoE, x, y, GetPlayerX(i), GetPlayerY(i)) Then
                                    SkillPlayer_Effect(VitalType, increment, i, Vital, skillnum)
                                End If
                            End If
                        Next
                        For i = 1 To MAX_MAP_NPCS
                            If MapNpc(MapNum).Npc(i).Num > 0 AndAlso MapNpc(MapNum).Npc(i).Vital(Enums.VitalType.HP) > 0 Then
                                If IsInRange(AoE, x, y, MapNpc(MapNum).Npc(i).X, MapNpc(MapNum).Npc(i).Y) Then
                                    SkillNpc_Effect(VitalType, increment, i, Vital, skillnum, MapNum)
                                End If
                            End If
                        Next
                End Select

            Case 2 ' targetted

                TargetType = MapNpc(MapNum).Npc(NpcNum).TargetType
                Target = MapNpc(MapNum).Npc(NpcNum).Target

                If TargetType = 0 OrElse Target = 0 Then Exit Sub

                If MapNpc(MapNum).Npc(NpcNum).TargetType = Enums.TargetType.Player Then
                    x = GetPlayerX(Target)
                    y = GetPlayerY(Target)
                Else
                    x = MapNpc(MapNum).Npc(Target).X
                    y = MapNpc(MapNum).Npc(Target).Y
                End If

                If Not IsInRange(range, MapNpc(MapNum).Npc(NpcNum).X, MapNpc(MapNum).Npc(NpcNum).Y, x, y) Then Exit Sub

                Select Case Skill(skillnum).Type
                    Case SkillType.DamageHp
                        If MapNpc(MapNum).Npc(NpcNum).TargetType = Enums.TargetType.Player Then
                            If CanNpcAttackPlayer(NpcNum, Target) AndAlso Vital > 0 Then
                                SendAnimation(MapNum, Skill(skillnum).SkillAnim, 0, 0, Enums.TargetType.Player, Target)
                                PlayerMsg(Target, Trim(Npc(MapNpc(MapNum).Npc(NpcNum).Num).Name) & " uses " & Trim(Skill(skillnum).Name) & "!", ColorType.Yellow)
                                SkillPlayer_Effect(Enums.VitalType.HP, False, Target, Vital, skillnum)
                                DidCast = True
                            End If
                        Else
                            If CanPlayerAttackNpc(NpcNum, Target, True) AndAlso Vital > 0 Then
                                SendAnimation(MapNum, Skill(skillnum).SkillAnim, 0, 0, Enums.TargetType.Npc, Target)
                                SkillNpc_Effect(Enums.VitalType.HP, False, i, Vital, skillnum, MapNum)

                                If Skill(skillnum).KnockBack = 1 Then
                                    KnockBackNpc(NpcNum, Target, skillnum)
                                End If
                                DidCast = True
                            End If
                        End If

                    Case SkillType.DamageMp, SkillType.HealMp, SkillType.HealHp
                        If Skill(skillnum).Type = SkillType.DamageMp Then
                            VitalType = Enums.VitalType.MP
                            increment = False
                        ElseIf Skill(skillnum).Type = SkillType.HealMp Then
                            VitalType = Enums.VitalType.MP
                            increment = True
                        ElseIf Skill(skillnum).Type = SkillType.HealHp Then
                            VitalType = Enums.VitalType.HP
                            increment = True
                        End If

                        If TempPlayer(NpcNum).TargetType = Enums.TargetType.Player Then
                            If Skill(skillnum).Type = SkillType.DamageMp Then
                                If CanPlayerAttackPlayer(NpcNum, Target, True) Then
                                    SkillPlayer_Effect(VitalType, increment, Target, Vital, skillnum)
                                End If
                            Else
                                SkillPlayer_Effect(VitalType, increment, Target, Vital, skillnum)
                            End If
                        Else
                            If Skill(skillnum).Type = SkillType.DamageMp Then
                                If CanPlayerAttackNpc(NpcNum, Target, True) Then
                                    SkillNpc_Effect(VitalType, increment, Target, Vital, skillnum, MapNum)
                                End If
                            Else
                                SkillNpc_Effect(VitalType, increment, Target, Vital, skillnum, MapNum)
                            End If
                        End If
                End Select
            Case 4 ' Projectile
                PlayerFireProjectile(NpcNum, skillnum)

                DidCast = True
        End Select

        If DidCast Then
            MapNpc(MapNum).Npc(NpcNum).Vital(Enums.VitalType.MP) = MapNpc(MapNum).Npc(NpcNum).Vital(Enums.VitalType.MP) - MPCost
            SendMapNpcVitals(MapNum, NpcNum)
            MapNpc(MapNum).Npc(NpcNum).SkillCD(skillslot) = GetTimeMs() + (Skill(skillnum).CdTime * 1000)
        End If
    End Sub

    Friend Sub SkillPlayer_Effect(Vital As Byte, increment As Boolean, Index As Integer, Damage As Integer, Skillnum As Integer)
        Dim sSymbol As String
        Dim Colour As Integer

        If Damage > 0 Then
            ' Calculate for Magic Resistance.
            Damage = Damage - ((GetPlayerStat(Index, StatType.Spirit) * 2) + (GetPlayerLevel(Index) * 3))

            If increment Then
                sSymbol = "+"
                If Vital = VitalType.HP Then Colour = ColorType.BrightGreen
                If Vital = VitalType.MP Then Colour = ColorType.BrightBlue
            Else
                sSymbol = "-"
                Colour = ColorType.BrightRed
            End If

            ' Deal with stun effects.
            If Skill(Skillnum).StunDuration > 0 Then StunPlayer(Index, Skillnum)

            SendActionMsg(GetPlayerMap(Index), sSymbol & Damage, Colour, ActionMsgType.Scroll, GetPlayerX(Index) * 32, GetPlayerY(Index) * 32)
            If increment Then SetPlayerVital(Index, Vital, GetPlayerVital(Index, Vital) + Damage)
            If Not increment Then SetPlayerVital(Index, Vital, GetPlayerVital(Index, Vital) - Damage)
            SendVital(Index, Vital)
        End If
    End Sub

    Friend Sub SkillNpc_Effect(Vital As Byte, increment As Boolean, Index As Integer, Damage As Integer, skillnum As Integer, MapNum As Integer)
        Dim sSymbol As String
        Dim Color As Integer

        If Index <= 0 OrElse Index > MAX_MAP_NPCS OrElse Damage < 0 OrElse MapNpc(MapNum).Npc(Index).Vital(Vital) <= 0 Then Exit Sub

        If Damage > 0 Then
            If increment Then
                sSymbol = "+"
                If Vital = VitalType.HP Then Color = ColorType.BrightGreen
                If Vital = VitalType.MP Then Color = ColorType.BrightBlue
            Else
                sSymbol = "-"
                Color = ColorType.BrightRed
            End If

            ' Deal with Stun and Knockback effects.
            If Skill(skillnum).KnockBack = 1 Then KnockBackNpc(Index, Index, skillnum)
            If Skill(skillnum).StunDuration > 0 Then StunNPC(Index, MapNum, skillnum)

            SendActionMsg(MapNum, sSymbol & Damage, Color, ActionMsgType.Scroll, MapNpc(MapNum).Npc(Index).X * 32, MapNpc(MapNum).Npc(Index).Y * 32)
            If increment Then MapNpc(MapNum).Npc(Index).Vital(Vital) = MapNpc(MapNum).Npc(Index).Vital(Vital) + Damage
            If Not increment Then MapNpc(MapNum).Npc(Index).Vital(Vital) = MapNpc(MapNum).Npc(Index).Vital(Vital) - Damage
            SendMapNpcVitals(MapNum, Index)
        End If
    End Sub
End Module