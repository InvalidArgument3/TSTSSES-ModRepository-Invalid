using EmptyKeys.UserInterface.Generated.EditFactionIconView_Bindings;
using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SiegableSafeZones
{
    public static class ActionControls
    {
        public static string selectedSafeZone = "";
        public static string rawSelectedSafeZone = "";
        public static long selectedSafeZoneId = 0;
        public static bool FactionError;
        public static bool ShowErrors;
        public static bool ValidTokens;
        public static bool ValidEnergy;
        public static bool FactionOffline;
        public static bool InRange;
        public static bool EnemyIsNear;
        public static bool IsBeingSieged;

        public static bool IsEnemyNear(IMyTerminalBlock block)
        {
            EnemyIsNear = false;
            IMySafeZoneBlock zone = block as IMySafeZoneBlock;
            if (zone != null)
            {
                if (zone.IsSafeZoneEnabled())
                    return !block.CubeGrid.IsStatic;
            }            

            double radius = Session.Instance.config._enemyCheckStartup._enemyCheckDistance;
            BoundingSphereD sphere = new BoundingSphereD(block.GetPosition(), radius);
            List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            IMyFaction ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);

            foreach (IMyEntity entity in entities)
            {
                if (entity == null || !MyAPIGateway.Entities.Exist(entity) || entity.MarkedForClose) continue;

                var cubeGrid = entity as IMyCubeGrid;
                var grid = entity as MyCubeGrid;
                if (cubeGrid == null || grid == null) continue;

                if (!grid.IsPowered) continue;

                var owner = cubeGrid.BigOwners.FirstOrDefault();
                if (owner == 0) continue;
                IMyFaction otherfaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);

                

                if (otherfaction == null || ownerFaction == null)
                {
                    if (otherfaction != null)
                    {
                        if (otherfaction.IsEveryoneNpc() || otherfaction.Tag.Length >= 4)
                            if (Session.Instance.config._enemyCheckStartup._omitNPCs)
                                continue;
                    }

                    if (owner != block.OwnerId)
                    {
                        EnemyIsNear = true;
                        return true;
                    }
                }

                if (Utils.AreFactionsEnemies(ownerFaction, otherfaction, Session.Instance.config._enemyCheckStartup._alliesFriendly, Session.Instance.config._enemyCheckStartup._omitNPCs))
                {
                    EnemyIsNear = true;
                    return true;
                }
            }

            return !block.CubeGrid.IsStatic;
        }

        public static bool CheckEnemyNearError(IMyTerminalBlock block)
        {
            return EnemyIsNear;
        }

        public static void GetSafeZoneList(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems, List<MyTerminalControlListBoxItem> selectedItems)
        {
            IMyFaction driveFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);

            string objectText = "abc";
            var dummy = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select SafeZone Below-"), MyStringId.GetOrCompute("-Select SafeZone Below-"), objectText);
            listItems.Add(dummy);
            if (driveFaction == null || rawSelectedSafeZone.Contains("abc") || string.IsNullOrEmpty(rawSelectedSafeZone))
            {
                selectedItems.Add(dummy);
            }

            if (driveFaction == null) return;
            foreach (var zone in Session.Instance.zoneBlockSettingsCache.Values)
            {
                /*if (zone.JDSiegingId == block.EntityId)
                {
                    listItems.Clear();
                    listItems.Add(dummy);
                    return;
                }*/

                if (zone == null)
                    MyLog.Default.WriteLineAndConsole("SiegableSafeZones: ZoneBlock settings are null");

                if (!zone.IsActive) continue;

                if (Session.Instance.config == null)
                    MyLog.Default.WriteLineAndConsole("SiegableSafeZones: Config is null");

                if (!Session.Instance.config._allowingSiegingOffline)
                    if (!Utils.IsFactionOnline(MyAPIGateway.Session.Factions.TryGetFactionById(zone.ZoneBlockFactionId), zone)) continue;

                double distance = Vector3D.Distance(block.GetPosition(), zone.ZoneBlockPos);
                if (distance <= Session.Instance.config._siegeConfig._siegeRange && !Utils.CheckUnsiegableAreas(zone.ZoneBlockPos))
                {
                    if (zone.Block == null)
                        MyLog.Default.WriteLineAndConsole("SiegableSafeZones: ZoneBlock block is null");

                    IMyFaction zoneFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(zone.Block.OwnerId);
                    if (zoneFaction == null) continue;
                    if (!Utils.AreFactionsEnemies(zoneFaction, driveFaction, !Session.Instance.config._siegeConfig._siegeAllies, true)) continue;

                    objectText = zone.ZoneBlockEntityId.ToString();
                    var toList = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"[{zoneFaction.Tag}] SafeZone: {Math.Round(distance, 2)}m"), MyStringId.GetOrCompute($"[{zoneFaction.Tag}] SafeZone: {Math.Round(distance, 2)}m"), objectText);
                    listItems.Add(toList);
                    if (rawSelectedSafeZone == objectText)
                        selectedItems.Add(toList);
                }
            }
        }

        public static void SetSafeZone(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> listItems)
        {
            if (listItems.Count == 0) return;

            //var split = listItems[0].UserData.ToString().Split('_');
            //if (split.Length < 2) return;

            long entityId = 0;
            if (!long.TryParse(listItems[0].UserData.ToString(), out entityId))
            {
                rawSelectedSafeZone = "abc";
                selectedSafeZone = "";
                selectedSafeZoneId = 0;
                RefreshControls(block);
                return;
            }

            selectedSafeZone = entityId.ToString();
            selectedSafeZoneId = entityId;
            rawSelectedSafeZone = listItems[0].UserData.ToString();
            RefreshControls(block);

            //selectedSafeZone = listItems[0].Text.ToString();
        }

        public static bool AllowSiegeEnable(IMyTerminalBlock block)
        {
            FactionOffline = false;
            CheckInFaction(block);
            CheckValidTokens(block);
            CheckJumpDriveCharge(block);
            InSiegeRange(block);
            CheckSiegeState(block);

            if (string.IsNullOrEmpty(rawSelectedSafeZone) || rawSelectedSafeZone == "-Select SafeZone Below-") return false;

            ZoneBlockSettings settings;
            if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(selectedSafeZoneId, out settings)) return false;
            if (!Session.Instance.config._allowingSiegingOffline)
                if (!Utils.IsFactionOnline(MyAPIGateway.Session.Factions.TryGetFactionById(settings.ZoneBlockFactionId), settings))
                {
                    FactionOffline = true;
                    return false;
                };

            

            if (FactionError) return false;
            if (!ValidTokens) return false;
            if (!ValidEnergy) return false;
            if (FactionOffline) return false;
            if (!InRange) return false;
            if (IsBeingSieged) return false;


            return true;
        }

        public static bool InSiegeRange(IMyTerminalBlock block)
        {
            InRange = false;
            ZoneBlockSettings settings;
            if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(selectedSafeZoneId, out settings)) return false;

            if (Vector3D.Distance(block.GetPosition(), settings.ZoneBlockPos) <= (double)Session.Instance.config._siegeConfig._siegeRange)
            {
                InRange = true;
                return true;
            }

            return false;
        }

        public static bool CheckInFaction(IMyTerminalBlock block)
        {
            FactionError = false;
            FactionError = !Utils.IsInFaction(MyAPIGateway.Session.LocalHumanPlayer.IdentityId);
            return !FactionError;
        }

        public static bool CheckJumpDriveCharge(IMyTerminalBlock block)
        {
            ValidEnergy = false;

            if (!block.IsWorking) return false;
            IMyJumpDrive jd = block as IMyJumpDrive;
            if (jd == null) return false;

            if (jd.CurrentStoredPower != jd.MaxStoredPower) return false;
            ValidEnergy = true;
            return true;
        }

        public static bool DisplayErrors(IMyTerminalBlock block)
        {
            return FactionError || FactionOffline || !ValidTokens || !ValidEnergy || !InRange;
        }

        public static bool CheckForFactionError(IMyTerminalBlock block)
        {
            return FactionError;
        }

        public static bool CheckFactionOfflineError(IMyTerminalBlock block)
        {
            return FactionOffline;
        }

        public static bool CheckValidTokensError(IMyTerminalBlock block)
        {
            return !ValidTokens;
        }

        public static bool CheckValidEnergyError(IMyTerminalBlock block)
        {
            return !ValidEnergy;
        }

        public static bool CheckInRangeError(IMyTerminalBlock block)
        {
            return !InRange;
        }

        public static bool CheckSiegeError(IMyTerminalBlock block)
        {
            return IsBeingSieged;
        }

        public static bool CheckSiegeState(IMyTerminalBlock block)
        {
            IsBeingSieged = false;
            ZoneBlockSettings settings;
            long entityId;
            if (!long.TryParse(selectedSafeZone, out entityId)) return false;
            Session.Instance.zoneBlockSettingsCache.TryGetValue(entityId, out settings);
            if (settings == null) return false;

            if (settings.IsSieging)
            {
                IsBeingSieged = true;
                return true;
            }

            return false;
        }

        public static bool CheckValidTokens(IMyTerminalBlock block)
        {
            ValidTokens = false;

            ZoneBlockSettings settings;
            long entityId;
            if (!long.TryParse(selectedSafeZone, out entityId)) return false;
            Session.Instance.zoneBlockSettingsCache.TryGetValue(entityId, out settings);
            if (settings == null) return false;

            if (Session.Instance.config._siegeConfig._siegeConsumptionAmt == 0)
            {
                ValidTokens = true;
                return true;
            }

            MyDefinitionId tokenId;
            if (!MyDefinitionId.TryParse(Session.Instance.config._siegeConfig._siegeConsumptionItem, out tokenId)) return false;

            IMyCubeGrid cubeGrid = block.CubeGrid;
            if (cubeGrid == null) return false;

            List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid).GetBlocksOfType(Blocks, x => x.HasInventory);
            MyFixedPoint tokens = 0;
            int tokensNeeded = Session.Instance.config._siegeConfig._siegeConsumptionAmt == -1 ? (int)Math.Round(settings.CurrentCharge, 0) : Session.Instance.config._siegeConfig._siegeConsumptionAmt;

            foreach (var tblock in Blocks)
            {
                var blockInv = tblock.GetInventory();
                tokens += blockInv.GetItemAmount(tokenId);

                if (tokens >= tokensNeeded) break;
            }

            if (tokens >= tokensNeeded)
            {
                ValidTokens = true;
                return true;
            }

            return false;
        }

        public static void StartSiege(IMyTerminalBlock block)
        {
            //var split = selectedSafeZone.Split('_');
            //if (split.Length < 2) return;
            if (!InSiegeRange(block)) return;
            if (!CheckValidTokens(block)) return;

            //long entityId = 0;
            IMyEntity ent;
            //if (!long.TryParse(selectedSafeZone, out entityId)) return;
            if (!MyAPIGateway.Entities.TryGetEntityById(selectedSafeZoneId, out ent)) return;
            
            IMySafeZoneBlock zone = ent as IMySafeZoneBlock;
            if (zone == null) return;

            ZoneBlockSettings settings = new ZoneBlockSettings();
            if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(zone.EntityId, out settings)) return;

            if (!settings.IsActive || settings.IsSieging) return;
            //Utils.TakeTokens(block, settings);
            settings.IsSieging = true;
            settings.JDSiegingId = block.EntityId;
            settings.JDBlock = block;
            settings.PlayerSieging = MyAPIGateway.Session.LocalHumanPlayer.IdentityId;
            selectedSafeZone = "";
            selectedSafeZoneId = 0;
            rawSelectedSafeZone = "";
            //Utils.DrainAllJDs(block);
            Comms.ClientBeginSiege(settings);

            RefreshControls(block);

            IMyFaction zonefaction = MyAPIGateway.Session.Factions.TryGetFactionById(settings.ZoneBlockFactionId);
            IMyFaction siegefaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(settings.JDBlock.OwnerId);
            if (zonefaction != null)
                Comms.SendMessageToChat($"Faction [{siegefaction.Tag}] started sieging [{zonefaction.Tag}], shield at {Math.Round(settings.CurrentCharge, 2)}%", Color.Red);
            else
                Comms.SendMessageToChat($"Faction [{siegefaction.Tag}] started sieging [{settings.ZoneBlockOwnerName}], shield at {Math.Round(settings.CurrentCharge, 2)}%", Color.Red);
        }

        public static void RefreshControls(IMyTerminalBlock block, bool force = true)
        {

            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
               // if (Session.Instance.controlRefreshDelay > 0 && force == false) return;
                var myCubeBlock = block as MyCubeBlock;

                if (myCubeBlock.IDModule != null)
                {

                    var share = myCubeBlock.IDModule.ShareMode;
                    var owner = myCubeBlock.IDModule.Owner;
                    myCubeBlock.ChangeOwner(owner, share == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.None);
                    myCubeBlock.ChangeOwner(owner, share);
                }
            }
        }
    }
}
