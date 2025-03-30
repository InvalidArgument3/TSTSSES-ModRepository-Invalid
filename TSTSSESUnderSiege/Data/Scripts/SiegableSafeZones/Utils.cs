using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Game;
using VRageMath;
using Sandbox.Game;
using VRage;
using VRage.ModAPI;
using static VRage.Game.MyObjectBuilder_AIComponent;
using SpaceEngineers.Game.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using Sandbox.Game.Entities;

namespace SiegableSafeZones
{
    public static class Utils
    {
        public static Random Rnd = new Random();

        public static bool AreFactionsEnemies(IMyFaction faction1, IMyFaction faction2, bool alliesFriendly, bool omitNPCs)
        {
            if (faction1 == null || faction2 == null) return true;
            if (faction1 == faction2) return false;
            if (faction1.IsEveryoneNpc() || faction2.IsEveryoneNpc())
                if (omitNPCs) return false;

            var relation = MyAPIGateway.Session.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId);
            if (relation == MyRelationsBetweenFactions.Enemies) return true;
            if (relation == MyRelationsBetweenFactions.Friends)
                if (!alliesFriendly) return true;

            return false;
        }

        public static bool IsFactionOnline(IMyFaction faction, ZoneBlockSettings settings)
        {
            if (faction == null)
                return IsPlayerOnline(settings);

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach(var player in players)
                if (faction.IsMember(player.IdentityId)) return true;

            return false;
        }

        public static bool IsPlayerOnline(ZoneBlockSettings settings)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var player in players)
                if (player.DisplayName == settings.ZoneBlockOwnerName) return true;

            return false;
        }

        public static bool CheckUnsiegableAreas(Vector3D pos)
        {
            foreach (var area in Session.Instance.config._siegeConfig._unsiegableAreas)
            {
                if (!area._enableArea) continue;
                if (Vector3D.Distance(pos, area._areaCenter) <= area._areaRadius) return true;
            }

            return false;
        }

        public static float RandomFloat(float minValue, float maxValue)
        {
            var minInflatedValue = (float)Math.Round(minValue, 3) * 1000;
            var maxInflatedValue = (float)Math.Round(maxValue, 3) * 1000;
            var randomValue = (float)Rnd.Next((int)minInflatedValue, (int)maxInflatedValue) / 1000;
            return randomValue;

        }

        public static void ActivateCharge(long entityId)
        {
            ZoneBlockSettings settings;
            if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(entityId, out settings)) return;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(settings.Block.OwnerId);
            if (faction != null && faction.IsEveryoneNpc()) return;

            if (settings.IsActive) return;

            settings.IsActive = true;

            if (settings.SiegeCompleted)
            {
                settings.CurrentCharge = Session.Instance.config._initCharge;
                settings.SiegeCompleted = false;
            }
            
            settings.ZoneBlockPos = settings.Block.GetPosition();
            //IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(settings.Block.OwnerId);
            if (faction != null)
            {
                settings.ZoneBlockFactionId = faction.FactionId;
                settings.ZoneBlockFactionTag = faction.Tag;
            }

            IMyPlayer player = GetPlayerFromId(settings.Block.OwnerId);
            if (player != null)
                settings.ZoneBlockOwnerName = player.DisplayName;

            StringBuilder sb = new StringBuilder();
            sb.Append("/n--- Siegable Safe Zones ---\n");
            sb.Append("[Charge State]: Active\n");
            sb.Append($"[Current Charge]: {Math.Round(settings.CurrentCharge, 2)}%");
            settings.DetailInfo = sb.ToString();

            //MyVisualScriptLogicProvider.ShowNotification("Charge Active!", 15000, "Green");
        }

        public static void DeactiveCharge(long entityId)
        {
            ZoneBlockSettings settings;
            if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(entityId, out settings)) return;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(settings.Block.OwnerId);
            if (faction != null && faction.IsEveryoneNpc()) return;

            if (!settings.IsActive) return;

            settings.IsActive = false;
            settings.IsSieging = false;
            settings.JDBlock = null;
            settings.JDSiegingId = 0;
            settings.PlayerSieging = 0;

            StringBuilder sb = new StringBuilder();
            sb.Append("\n--- Siegable Safe Zones ---\n");
            sb.Append("[Charge State]: Deactived");
            settings.DetailInfo = sb.ToString();

            //MyVisualScriptLogicProvider.ShowNotification("Charge Deactive!", 15000, "Red");

        }

        public static void ChargeShield(ZoneBlockSettings settings)
        {
            MySafeZone zone = settings.Block as MySafeZone;
            if (zone != null)
                if (zone.SafeZoneBlockId == 0) return;

            if (settings.CurrentCharge >= 100) return;
            settings.CurrentCharge += Session.Instance.config._rechargeRate / 60f;
            //MyVisualScriptLogicProvider.ShowNotification($"Charging! Shield Charge: {settings.CurrentCharge}", 1000, "Green");

            if (settings.CurrentCharge > 100) 
                settings.CurrentCharge = 100f;

            StringBuilder sb = new StringBuilder();
            sb.Append("\n--- Siegable Safe Zones ---\n");
            sb.Append("[Charge State]: Charging\n");
            sb.Append($"[Current Charge]: {Math.Round(settings.CurrentCharge, 2)}%");
            settings.DetailInfo = sb.ToString();
        }

        public static void DrainShield(ZoneBlockSettings settings)
        {
            IMyFaction zonefaction = MyAPIGateway.Session.Factions.TryGetFactionById(settings.ZoneBlockFactionId);
            IMyFaction siegefaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(settings.JDBlock.OwnerId);

            if (!IsBlockInRangeAndFunctional(settings))
            {
                if (zonefaction != null)
                    new ModMessage($"Faction [{siegefaction.Tag}] failed to siege [{zonefaction.Tag}]", Color.Red);
                else
                    new ModMessage($"Faction [{siegefaction.Tag}] failed to siege [{settings.ZoneBlockOwnerName}]", Color.Red);

                settings.IsSieging = false;
                settings.JDBlock = null;
                settings.JDSiegingId = 0;

                return;
            }

    
            settings.CurrentCharge -= Session.Instance.config._drainRate / 60f;
            if (Math.Round(settings.CurrentCharge, 0) % 10 == 0)
            {
                if (!settings.Alerted)
                {
                    settings.Alerted = true;
                    if (zonefaction != null)
                        Comms.SendMessageToChat($"Faction [{siegefaction.Tag}] is sieging [{zonefaction.Tag}], shield at {Math.Round(settings.CurrentCharge, 2)}%", Color.Red);
                    else
                        Comms.SendMessageToChat($"Faction [{siegefaction.Tag}] is sieging [{settings.ZoneBlockOwnerName}], shield at {Math.Round(settings.CurrentCharge, 2)}%", Color.Red);

                }
            }
            else
                settings.Alerted = false;
            
            //MyVisualScriptLogicProvider.ShowNotification($"Draining! Shield Charge: {settings.CurrentCharge}", 1000, "Red");



            if (settings.CurrentCharge <= 0)
            {

                if (zonefaction != null)
                    new ModMessage($"Faction [{siegefaction.Tag}] successfully sieged [{zonefaction.Tag}]", Color.Green);
                else
                    new ModMessage($"Faction [{siegefaction.Tag}] successfully sieged [{settings.ZoneBlockOwnerName}]", Color.Green);

                settings.IsActive = false;
                settings.IsSieging = false;
                settings.CurrentCharge = 0;
                settings.SiegeCompleted = true;
                if (settings.Block == null) return;

                settings.Block.Enabled = false;
                settings.JDBlock = null;
                settings.JDSiegingId = 0;

                StringBuilder sb2 = new StringBuilder();
                sb2.Append("\n--- Siegable Safe Zones ---\n");
                sb2.Append("[Charge State]: Deactivated\n");
                sb2.Append($"[Current Charge]: {Math.Round(settings.CurrentCharge, 2)}%");
                settings.DetailInfo = sb2.ToString();

                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("\n--- Siegable Safe Zones ---\n");
            sb.Append("[Charge State]: Draining\n");
            sb.Append($"[Current Charge]: {Math.Round(settings.CurrentCharge, 2)}%");
            settings.DetailInfo = sb.ToString();
        }

        public static bool IsBlockInRangeAndFunctional(ZoneBlockSettings settings)
        {
            if (settings.JDBlock == null || settings.JDBlock.MarkedForClose) return false;
            if (Vector3D.Distance(settings.ZoneBlockPos, settings.JDBlock.GetPosition()) > Session.Instance.config._siegeConfig._siegeRange) return false;
            if (!settings.JDBlock.IsWorking) return false;

            return true;
        }

        public static IMyPlayer GetPlayerFromId(long playerId)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach(var player in players)
            {
                if (player.IdentityId == playerId)
                    return player;
            }

            return null;
        }

        public static bool IsInFaction(long playerId)
        {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId) != null;
        }

        public static bool TakeTokens(IMyEntity entity, ZoneBlockSettings settings)
        {
            var jd = entity as IMyJumpDrive;
            if (jd == null) return false;

            IMyCubeGrid cubeGrid = jd.CubeGrid;
            if (cubeGrid == null) return false;

            int tokensToRemove = Session.Instance.config._siegeConfig._siegeConsumptionAmt == -1 ? (int)Math.Round(settings.CurrentCharge, 0) : Session.Instance.config._siegeConfig._siegeConsumptionAmt;


            if (tokensToRemove == 0) return true;

            MyDefinitionId tokenId;
            if (!MyDefinitionId.TryParse(Session.Instance.config._siegeConfig._siegeConsumptionItem, out tokenId)) return false;

            List<IMyInventory> cachedInventory = new List<IMyInventory>();
            List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid).GetBlocksOfType(Blocks, x => x.HasInventory);
            MyFixedPoint tokens = 0;

            foreach (var tblock in Blocks)
            {
                tokens = 0;
                IMyInventory blockInv = tblock.GetInventory();
                tokens = blockInv.GetItemAmount(tokenId);

                if (tokens != 0)
                {
                    tokensToRemove -= (int)tokens;
                    if (cachedInventory.Contains(blockInv)) continue;
                    cachedInventory.Add(blockInv);
                }

                if (tokensToRemove <= 0) break;
            }

            if (tokensToRemove > 0) return false;

            tokensToRemove = Session.Instance.config._siegeConfig._siegeConsumptionAmt == -1 ? (int)Math.Round(settings.CurrentCharge, 0) : Session.Instance.config._siegeConfig._siegeConsumptionAmt;
            foreach (MyInventory inventory in cachedInventory)
            {
                var removed = (int)inventory.RemoveItemsOfType(tokensToRemove, tokenId);
                tokensToRemove -= removed;
                if (tokensToRemove <= 0) return true;
            }

            if (tokensToRemove <= 0) return true;

            return false;
        }

        public static void SaveConfigToSandbox(Config config)
        {
            var newByteData = MyAPIGateway.Utilities.SerializeToBinary(config);
            var base64string = Convert.ToBase64String(newByteData);

            MyAPIGateway.Utilities.SetVariable(Session.Instance.ConfigToSandboxVariable, base64string);
        }

        public static Config LoadConfigFromSandbox()
        {
            string base64string;
            MyAPIGateway.Utilities.GetVariable(Session.Instance.ConfigToSandboxVariable, out base64string);
            var byteData = Convert.FromBase64String(base64string);
            return MyAPIGateway.Utilities.SerializeFromBinary<Config>(byteData);
        }

        public static void DrainAllJDs(IMyEntity entity)
        {
            if (entity == null) return;
            IMyJumpDrive jd = entity as IMyJumpDrive;
            if (jd == null) return;
            IMyCubeGrid cubeGrid = jd.CubeGrid;
            if (cubeGrid == null) return;

            List<IMyJumpDrive> Blocks = new List<IMyJumpDrive>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(cubeGrid).GetBlocksOfType(Blocks, x => x.IsFunctional);

            foreach (var item in Blocks)
            {
                item.CurrentStoredPower = 0f;
            }

        }

    }
}
