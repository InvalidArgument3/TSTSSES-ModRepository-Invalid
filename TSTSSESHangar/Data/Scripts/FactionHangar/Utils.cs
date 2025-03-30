using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Engine.Utils;
using System.Reflection.Metadata.Ecma335;
using VRage.Compiler;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using ProtoBuf.Meta;
using EmptyKeys.UserInterface.Generated;
using VRage.Scripting;

namespace CustomHangar
{
    public static class Utils
    {
        public static string GetGridsToStore(List<MyEntity> entities, HangarType hangarType, long playerId)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            bool isLeader = false;
            if (faction != null)
                isLeader = faction.IsLeader(playerId);

            string gridNames = "Potential Grids To Store:\n";
            foreach(var entity in entities)
            {
                VRage.Game.ModAPI.IMyCubeGrid grid = entity as VRage.Game.ModAPI.IMyCubeGrid;
                if (grid == null) continue;

                if (faction != null)
                {
                    if (Session.Instance.DoesFactionOwnGrid(grid, faction))
                    {
                        if (!isLeader)
                        {
                            if (Session.Instance.DoesPlayerOwnGrid(grid, playerId))
                                gridNames += $"{grid.CustomName},\n";
                        }
                        else
                            gridNames += $"{grid.CustomName},\n";
                    } 
                }
                else
                    if (Session.Instance.DoesPlayerOwnGrid(grid, playerId))
                        gridNames += $"{grid.CustomName},\n";
            }

            return gridNames;
        }

        public static void CheckOwnerValidFaction(IMyFaction faction, MyCubeGrid grid, long playerId)
        {
            if (faction == null) return;
            if (faction.IsMember(playerId)) return;

            grid.ChangeGridOwner(playerId, MyOwnershipShareModeEnum.Faction);
        }

        public static bool IsGridIntersecting(List<MyCubeGrid> grids)
        {
            if (grids == null || grids.Count == 0) return false;
            /*List<MyEntity> ents = new List<MyEntity>();
            var bb = grid.PositionComp.WorldVolume.GetBoundingBox();
            MyGamePruningStructure.GetAllEntitiesInBox(ref bb, ents);
            return ents.Count != 0;*/

            /*var aabb = grid.PositionComp.WorldAABB;
            bool result = grid.GetIntersectionWithAABB(ref aabb);
            return result;*/
            var settings = new MyGridPlacementSettings();
            settings.VoxelPlacement = new VoxelPlacementSettings()
            {
                PlacementMode = VoxelPlacementMode.OutsideVoxel
            };

            foreach (var grid in grids)
            {
                bool isStatic = grid.GridSizeEnum == MyCubeSize.Large ? true : Session.Instance.config.spawnSGStatic;
                var canPlace = MyCubeGrid.TestPlacementArea(grid, isStatic, ref settings, grid.PositionComp.LocalAABB, false, null, !isStatic, true);
                if (canPlace) continue;
                return true;
            }

            return false;
        }

        public static bool CheckForExcludedBlock(MyCubeGrid grid)
        {
            var blocks = grid.GetFatBlocks();
            foreach(var block in blocks)
            {
                VRage.Game.ModAPI.IMyCubeBlock cubeBlock = block as VRage.Game.ModAPI.IMyCubeBlock;
                long owner = block.OwnerId;
                if (owner == 0) continue;

                MyDefinitionId blockDef = cubeBlock.BlockDefinition;
                foreach(var def in Session.Instance.config.autoHangarConfig.exclusions.excludedBlockTypes)
                {
                    foreach(var subtype in def.blockSubtypes.subtype)
                    {
                        MyDefinitionId id;
                        MyDefinitionId.TryParse(def.blockType, subtype, out id);
                        if (id != null && id == blockDef) return true;
                    }
                }
            }

            return false;
        }

        public static void LoadHelpPopup()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("*** Faction leaders can execute commands on any faction grid while members can only execute commands on their own grids in the faction hangar. ***\n\n");
            sb.Append("/fh list - Displays a list of grids and their index currently in your faction hangar.\n");
            sb.Append("/fh store - Displays a list of possible grids you can store to your faction hangar in range.\n");
            sb.Append("/fh store GRIDNAME - Attempts to store the desired grid to your faction hangar.\n");
            sb.Append("/fh load [index#] - Attempts to load the desired grid at index from faction hangar.\n");
            sb.Append("/fh load [index#].true - Attempts to load the desired grid at index to its original location.\n");
            sb.Append("/fh load [index#].true.force - Attempts to load the desired grid at index but forces spawning at original location (no collision checks).\n");
            sb.Append("/fh transfer [index#] - Transfers the desired grid at index over to your private hangar.\n");
            sb.Append("/fh remove [index#] - Attempts to delete the desired grid at index from faction hangar.\n");
            sb.Append("/fh togglesphere - Toggles the visble debugging sphere that shows the 'spawnable' areas.\n");
            sb.Append("/fh togglegps - Toggles the GPS markers for the 'spawnable' areas.\n");
            sb.Append("/ph list - Displays a list of grids and their index currently in your private hangar.\n");
            sb.Append("/ph store - Displays a list of possible grids you can store to private hangar in range.\n");
            sb.Append("/ph store GRIDNAME - Attempts to store the desired grid to your private hangar.\n");
            sb.Append("/ph load [index#] - Attempts to load the desired grid at index from private hangar.\n");
            sb.Append("/ph load [index#].true - Attempts to load the desired grid at index to its original location.\n");
            sb.Append("/ph load [index#].true.force - Attempts to load the desired grid at index but forces spawning at original location (no collision checks).\n");
            sb.Append("/ph transfer [index#] - Transfers the desired grid at index over to your faction hangar.\n");
            sb.Append("/ph remove [index#] - Attempts to delete the desired grid at index from private hangar.\n");

            MyAPIGateway.Utilities.ShowMissionScreen("Faction Hangar Command List", "", null, sb.ToString(), null, "Ok");

        }

        public static void Storegrid(ObjectContainer packet)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
            if (packet.hangarType == HangarType.Faction)
            {
                if (faction == null) return;
                if (Session.Instance.cooldownTimers.ContainsKey(faction))
                {
                    foreach (var timer in Session.Instance.cooldownTimers[faction].timers)
                    {
                        if (timer.type == TimerType.StorageCooldown)
                        {
                            MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {timer.time} seconds before your faction can store.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                            return;
                        }
                    }
                }

                int slots = Session.Instance.allHangarData.GetFactionSlots(faction.FactionId);
                if (slots >= Session.Instance.config.factionHangarConfig.maxFactionSlots)
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Your faction has exceeded the amount of stored grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                if (packet.gridData.Count > 1)
                {
                    if (slots + packet.gridData.Count > Session.Instance.config.factionHangarConfig.maxFactionSlots)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Storing {packet.gridData.Count} connected grids will exceed your faction slots. Please remove connected grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                        return;
                    }
                }
            }

            if (packet.hangarType == HangarType.Private)
            {
                int unusedSlots = Session.Instance.allHangarData.GetPrivateSlots(packet.playerId);
                if (unusedSlots >= Session.Instance.config.privateHangarConfig.maxPrivateSlots)
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"You have exceeded the {Session.Instance.config.privateHangarConfig.maxPrivateSlots} max amount of stored grids in private hangar.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                if (packet.gridData.Count > 1)
                {
                    if (unusedSlots + packet.gridData.Count > Session.Instance.config.privateHangarConfig.maxPrivateSlots)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Storing {packet.gridData.Count} connected grids will exceed your private slots of {Session.Instance.config.privateHangarConfig.maxPrivateSlots}. Please remove connected grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                        return;
                    }
                }
            }

            int delaySeconds = packet.hangarType == HangarType.Faction ? Session.Instance.config.factionHangarConfig.factionStoreDelay : Session.Instance.config.privateHangarConfig.privateStoreDelay;
            HangarDelayData hangarDelayData = new HangarDelayData()
            {
                playerId = packet.playerId,
                gridData = packet.gridData,
                playerName = packet.stringData,
                hangarType = packet.hangarType,
                requesterId = packet.requesterId
            };

            Session.Instance.hangarDelay.Add(hangarDelayData);
            foreach (var grid in packet.gridData)
                MyVisualScriptLogicProvider.SendChatMessageColored($"Storing Grid {grid.gridName} in {delaySeconds} seconds", Color.Green, "[FactionHangar]", packet.playerId, "Green");

            if (faction != null)
                FactionTimers.AddTimer(faction, TimerType.StorageCooldown, Session.Instance.config.factionHangarConfig.factionHangarCooldown);

            IMyPlayer player = Session.Instance.GetPlayerfromID(packet.requesterId);
            bool privateStorage = packet.hangarType == HangarType.Private ? true : false;
            if (player != null)
                Comms.AddClientCooldown(player.SteamUserId, privateStorage, TimerType.StorageCooldown);

        }

        public static void GetFactionList(ObjectContainer packet)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
            if (faction != null)
            {
                bool isLeader = faction.IsLeader(packet.playerId);
                string gridNames = Session.Instance.allHangarData.GetFactionsGridNames(faction.FactionId, packet.playerId, isLeader);
                if (string.IsNullOrEmpty(gridNames))
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Your faction does not have any stored grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                if (!isLeader)
                    MyVisualScriptLogicProvider.SendChatMessageColored($"You are not a faction leader and can ONLY access grids owned by you.", Color.Orange, "[FactionHangar]", packet.playerId, "Green");

                MyVisualScriptLogicProvider.SendChatMessageColored($"{gridNames}", Color.Green, "[FactionHangar]", packet.playerId, "Green");
                return;
            }
        }

        public static void GetPrivateList(ObjectContainer packet)
        {
            string gridNames = Session.Instance.allHangarData.GetPrivateGridNames(packet.playerId);
            if (string.IsNullOrEmpty(gridNames))
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Your private hangar does not have any stored grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                return;
            }

            MyVisualScriptLogicProvider.SendChatMessageColored($"{gridNames}", Color.Green, "[FactionHangar]", packet.playerId, "Green");
            return;
        }

        public static bool TryHangarGridRemoval(ObjectContainer packet)
        {
            if (packet.hangarType == HangarType.Faction)
            {
                IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
                if (faction != null)
                {
                    bool isLeader = faction.IsLeader(packet.playerId);
                    var gridData = Session.Instance.allHangarData.GetFactionGridData(faction.FactionId, packet.intValue);
                    if (gridData == null)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Grid index {packet.intValue} is invalid", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                        return false;
                    }

                    if (!isLeader)
                    {
                        if (gridData.owner != packet.playerId)
                        {
                            MyVisualScriptLogicProvider.SendChatMessageColored($"You are not a faction leader and can ONLY remove grids owned by you.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                            return false;
                        }
                    }

                    Session.Instance.allHangarData.RemoveFactionData(faction.FactionId, packet.intValue, true);
                    return true;
                }
            }

            if (packet.hangarType == HangarType.Private)
            {
                Session.Instance.allHangarData.RemovePrivateData(packet.playerId, packet.intValue, true);
                return true;
            }

            return false;
        }

        public static void GetGridData(ObjectContainer packet)
        {
            GridData gridData = null;
            MyObjectBuilder_Definitions ob = null;
            if (packet.hangarType == HangarType.Faction)
            {
                IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
                if (faction == null) return;

                if (Session.Instance.cooldownTimers.ContainsKey(faction))
                {
                    foreach (var timer in Session.Instance.cooldownTimers[faction].timers)
                    {
                        if (timer.type == TimerType.RetrievalCooldown)
                        {
                            MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {timer.time} seconds before your faction can load another grid.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                            return;
                        }
                    }
                }

                bool isLeader = faction.IsLeader(packet.playerId);

                gridData = Session.Instance.allHangarData.GetFactionGridData(faction.FactionId, packet.intValue);
                if (gridData == null)
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Grid index {packet.intValue} is invalid", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                if (!isLeader)
                {
                    if (gridData.owner != packet.playerId)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"You are not a faction leader and can ONLY access grids owned by you.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                        return;
                    }
                }

                MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_Definitions>(gridData.gridPath, out ob);
                if (ob == null)
                    return;
            }

            if (packet.hangarType == HangarType.Private)
            {
                gridData = Session.Instance.allHangarData.GetPrivateGridData(packet.playerId, packet.intValue);
                if (gridData == null)
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Grid index {packet.intValue} is invalid", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_Definitions>(gridData.gridPath, out ob);
                if (ob == null)
                    return;
            }

            if (packet.originalLocation)
            {
                MyObjectBuilder_CubeGrid[] cubeGridObs = Session.Instance.GetGridFromGridData(ob, packet.intValue, packet.hangarType);
                if (cubeGridObs == null) return;


                Session.Instance.spawnType = SpawnType.Original;
                if (Session.Instance.IsEnemyNear(cubeGridObs[0], packet.playerId))
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Enemy nearby, failed to spawn from hangar.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                    return;
                }

                Session.Instance.SpawnGridsFromOb(cubeGridObs.ToList(), packet.intValue, packet.hangarType, packet.playerId, 0, SpawnType.Original, !packet.force);
                Session.Instance.spawnType = SpawnType.None;
                return;
            }

            Comms.SendOBToClient(ob, packet.steamId, packet.intValue, packet.hangarType);
        }

        public static void CreateNullShipBlueprint(string path)
        {
            //List<MyObjectBuilder_CubeGrid> list = GetGridGroupObs(myCubeGrid, groupType);
            MyObjectBuilder_ShipBlueprintDefinition myObjectBuilder_ShipBlueprintDefinition = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
            //myObjectBuilder_ShipBlueprintDefinition.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), MyUtils.StripInvalidChars(blueprintName));
            //myObjectBuilder_ShipBlueprintDefinition.CubeGrids = list.ToArray();
            //myObjectBuilder_ShipBlueprintDefinition.RespawnShip = false;
            //myObjectBuilder_ShipBlueprintDefinition.DisplayName = blueprintName;
            //myObjectBuilder_ShipBlueprintDefinition.CubeGrids[0].DisplayName = blueprintName;
            MyObjectBuilder_Definitions myObjectBuilder_Definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            myObjectBuilder_Definitions.ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1];
            myObjectBuilder_Definitions.ShipBlueprints[0] = myObjectBuilder_ShipBlueprintDefinition;
            MyObjectBuilderSerializer.SerializeXML(path, false, myObjectBuilder_Definitions);
        }

        public static void UpdateBalance(long playerId, long amount, int index, HangarType hangarType)
        {
            if (!Session.Instance.isServer) return;

            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            long current = 0;
            if (faction != null)
            {
                if (hangarType == HangarType.Faction)
                {
                    GridData data = Session.Instance.allHangarData.GetFactionGridData(faction.FactionId, index);
                    if (data != null)
                        if (data.autoHangared && Session.Instance.config.autoHangarConfig.autoBypassSpawnCost)
                            return;
                }
                else
                {
                    GridData data = Session.Instance.allHangarData.GetPrivateGridData(playerId, index);
                    if (data != null)
                        if (data.autoHangared && Session.Instance.config.autoHangarConfig.autoBypassSpawnCost)
                            return;
                }

                faction.TryGetBalanceInfo(out current);
                if (amount <= current)
                {
                    faction.RequestChangeBalance(-amount);
                    return;
                } 
            }

            IMyPlayer player = Session.Instance.GetPlayerfromID(playerId);
            if (player == null) return;

            if (hangarType == HangarType.Private)
            {
                GridData data = Session.Instance.allHangarData.GetPrivateGridData(playerId, index);
                if (data != null)
                    if (data.autoHangared && Session.Instance.config.autoHangarConfig.autoBypassSpawnCost)
                        return;
            }

            player.TryGetBalanceInfo(out current);
            if (amount <= current)
                player.RequestChangeBalance(-amount);
        }

        public static void AddSpawnLocationsClientGPS()
        {
            if (!Session.Instance.spawnClientGPS) return;
            foreach (var area in Session.Instance.config.spawnAreas)
            {
                if (!area.enableSpawnArea) continue;
                if (Session.Instance.useInverseSpawnArea)
                {
                    IMyGps gps = MyAPIGateway.Session.GPS.Create($"Non-{SpawnType.SpawnArea} - {area.areaRadius}m", "", area.areaCenter, true);
                    gps.GPSColor = Color.SeaGreen;
                    MyAPIGateway.Session.GPS.AddLocalGps(gps);
                    Session.Instance.clientSpawnLocations.Add(gps);
                }
                else
                {
                    IMyGps gps = MyAPIGateway.Session.GPS.Create($"{SpawnType.SpawnArea} - {area.areaRadius}m", "", area.areaCenter, true);
                    gps.GPSColor = Color.SeaGreen;
                    MyAPIGateway.Session.GPS.AddLocalGps(gps);
                    Session.Instance.clientSpawnLocations.Add(gps);
                }
            }

            if (Session.Instance.config.spawnNearbyConfig.allowSpawnNearby)
            {
                IMyGps gps = MyAPIGateway.Session.GPS.Create($"{SpawnType.Nearby} - {Session.Instance.config.spawnNearbyConfig.nearbyRadius}m", "", Session.Instance.original, true);
                gps.GPSColor = Color.Orange;
                MyAPIGateway.Session.GPS.AddLocalGps(gps);
                Session.Instance.clientSpawnLocations.Add(gps);
            }
        }

        public static void RemoveSpawnLocationsClientGPS()
        {
            foreach(var gps in Session.Instance.clientSpawnLocations)
                MyAPIGateway.Session.GPS.RemoveLocalGps(gps);

            Session.Instance.clientSpawnLocations.Clear();
        }

        public static void CheckGridSpawnLimits(VRage.Game.ModAPI.IMyCubeGrid grid, IMyFaction faction, HangarType hangarType, int index, SpawnType spawnType, long playerId)
        {
            if (hangarType == HangarType.Faction)
            {
                if (faction != null)
                {
                    if (Session.Instance.allHangarData.IsFactionGridAutoHangared(faction.FactionId, index))
                        if (Session.Instance.config.autoHangarConfig.autoBypassSpawnLimits) return;

                    if (BypassLimits(spawnType, grid.GetPosition())) return;
                }
            }

            if (hangarType == HangarType.Private)
            {
                if (Session.Instance.allHangarData.IsPrivateGridAutoHangared(playerId, index))
                    if (Session.Instance.config.autoHangarConfig.autoBypassSpawnLimits) return;

                if (BypassLimits(spawnType, grid.GetPosition())) return;
            }

            List<VRage.Game.ModAPI.IMyCubeGrid> connectedGrids = new List<VRage.Game.ModAPI.IMyCubeGrid>();
            Session.Instance.GetGroupByType(grid, connectedGrids, GridLinkTypeEnum.Physical);

            bool removeAmmo = Session.Instance.config.spawnConfig.removeAmmo;
            bool removeUranium = Session.Instance.config.spawnConfig.removeUranium;
            bool removeIce = Session.Instance.config.spawnConfig.removeIce;

            foreach(var connectedGrid in connectedGrids)
            {
                var blocks = connectedGrid.GetFatBlocks<VRage.Game.ModAPI.IMyCubeBlock>();
                foreach(var block in blocks)
                {
                    if (!block.HasInventory) continue;
                    if (removeAmmo || removeUranium || removeIce)
                    {
                        var invList = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                        VRage.Game.ModAPI.IMyInventory blockInv = block.GetInventory();
                        blockInv.GetItems(invList);

                        foreach (var item in invList)
                        {
                            bool removeItem = false;
                            if (item.Type.GetItemInfo().IsAmmo && removeAmmo)
                                removeItem = true;
                            if (item.Type.SubtypeId.Contains("Ice") && removeIce)
                                removeItem = true;
                            if(item.Type.SubtypeId.Contains("Uranium") && removeUranium)
                                removeItem = true;

                            if (!removeItem) continue;
                            MyFixedPoint amount = item.Amount;
                            uint itemId = item.ItemId;

                            blockInv.RemoveItems(itemId, amount);
                        }
                    }
                }
            }
        }

        public static bool BypassLimits(SpawnType spawntype, Vector3D pos)
        {
            if (spawntype == SpawnType.Dynamic)
                return Session.Instance.config.dynamicSpawningConfig.dynamicSpawnBypass;

            if (spawntype == SpawnType.SpawnArea)
            { 
                foreach(var area in Session.Instance.config.spawnAreas)
                {
                    if (!area.enableSpawnArea) continue;
                    if (Vector3D.Distance(pos, area.areaCenter) > area.areaRadius) continue;
                    return area.spawnAreaBypass;
                }

                return true;
            }

            if (spawntype == SpawnType.Nearby)
                return Session.Instance.config.spawnNearbyConfig.nearbySpawnBypass;

            if (spawntype == SpawnType.Original)
                return Session.Instance.config.spawnOriginalConfig.originalSpawnBypass;

            return true;
        }

        public static void CheckGridSpawnLimitsInOB(List<MyObjectBuilder_CubeGrid> obs, IMyFaction faction, HangarType hangarType, int index, SpawnType spawnType, long playerId)
        {
            if (hangarType == HangarType.Faction)
            {
                if (faction != null)
                {
                    if (Session.Instance.allHangarData.IsFactionGridAutoHangared(faction.FactionId, index))
                        if (Session.Instance.config.autoHangarConfig.autoBypassSpawnLimits) return;

                    if (BypassLimits(spawnType, obs[0].PositionAndOrientation.Value.Position)) return;
                }
            }

            if (hangarType == HangarType.Private)
            {
                if (Session.Instance.allHangarData.IsPrivateGridAutoHangared(playerId, index))
                    if (Session.Instance.config.autoHangarConfig.autoBypassSpawnLimits) return;

                if (BypassLimits(spawnType, obs[0].PositionAndOrientation.Value.Position)) return;
            }

            foreach (var grid in obs)
            {
                foreach(var block in grid.CubeBlocks)
                {
                    if (block.TypeId == typeof(MyObjectBuilder_BatteryBlock))
                    {
                        var baseBlock = block as MyObjectBuilder_Base;
                        if (baseBlock == null) continue;

                        var battery = baseBlock as MyObjectBuilder_BatteryBlock;
                        if (battery == null) continue;

                        float remain = battery.CurrentStoredPower;
                        MyVisualScriptLogicProvider.ShowNotification($"Battery = {remain}", 20000);
                        battery.CurrentStoredPower = Session.Instance.config.spawnConfig.batteryPercentage / 100;

                        continue;
                    }

                    if (block.TypeId == typeof(MyObjectBuilder_GasTank))
                    {
                        var baseBlock = block as MyObjectBuilder_Base;
                        if (baseBlock == null) continue;

                        var tank = baseBlock as MyObjectBuilder_GasTank;
                        if (tank == null) continue;

                        float remain = tank.FilledRatio;
                        MyVisualScriptLogicProvider.ShowNotification($"Gas = {remain}", 20000);
                        tank.FilledRatio = Session.Instance.config.spawnConfig.h2Percentage / 100;
                    }
                }
            }
        }

        public static void RemovePlayersFromSeats(MyCubeGrid grid)
        {
            List<IMyCockpit> Blocks = new List<IMyCockpit>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid).GetBlocksOfType(Blocks, x => x.IsFunctional);

            foreach(var block in Blocks)
            {
                if (block.IsOccupied)
                    block.RemovePilot();
            }
        }
    }
}
