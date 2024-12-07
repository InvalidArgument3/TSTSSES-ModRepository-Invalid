using Sandbox.ModAPI;
using System.Collections.Generic;
using Sandbox.Game;

namespace Invalid.DeltaVQuestLog.Commands
{
    internal static class CommandMethods
    {
        private static long PlayerId => MyAPIGateway.Session.Player.IdentityId;
        private static long? FactionId => MyAPIGateway.Session.Factions.TryGetPlayerFaction(PlayerId)?.FactionId;

        private static bool IsFactionLeaderOrFounder() => FactionId != null && PersistentFactionObjectives.IsFactionLeaderOrFounder(FactionId.Value, PlayerId);

        public static void HandleAddObjective(string[] args)
        {
            string objectiveText = string.Join(" ", args, 1, args.Length - 1);

            if (string.IsNullOrWhiteSpace(objectiveText))
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Please provide an objective description.");
                return;
            }

            if (!IsFactionLeaderOrFounder())
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", $"Only faction leaders or founders can add objectives.");
                return;
            }

            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            manager.AddQuest(objectiveText);
        }

        public static void HandleListObjectives(string[] args)
        {
            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            if (manager.Objectives.Count == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "No objectives found.");
                return;
            }

            MyAPIGateway.Utilities.ShowMessage("Objectives", "Faction Objectives:");
            for (int i = 0; i < manager.Objectives.Count; i++)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", $"{i + 1}. {manager.Objectives[i]}");
            }
        }

        public static void HandleShowObjectives(string[] args)
        {
            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            if (manager.Objectives.Count == 0)
            {
                MyVisualScriptLogicProvider.SetQuestlog(false, "", PlayerId);
                MyAPIGateway.Utilities.ShowMessage("Objectives", "No objectives to display.");
                return;
            }

            manager.UpdatePlayerQuestlog();
        }

        public static void HandleRemoveObjective(string[] args)
        {
            int index;
            if (args.Length < 2 || !int.TryParse(args[1], out index))
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Please provide a valid objective index to remove.");
                return;
            }

            if (!IsFactionLeaderOrFounder())
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Only faction leaders or founders can remove objectives.");
                return;
            }

            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            if (!manager.RemoveQuest(index))
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Invalid objective index.");
                return;
            }
        }

        public static void HandleBroadcast(string[] args)
        {
            int duration;
            if (args.Length < 2 || !int.TryParse(args[1], out duration))
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Usage: /obj broadcast <duration in seconds>");
                return;
            }

            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            if (manager.Objectives.Count == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "No objectives to broadcast.");
                return;
            }

            manager.ForceShow(duration);
        }

        public static void HandleHideQuestLog(string[] args)
        {
            MyVisualScriptLogicProvider.SetQuestlog(false, "", PlayerId);
            MyAPIGateway.Utilities.ShowMessage("Objectives", "Quest log hidden.");
        }

        public static void HandleNotifications(string[] args)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(MyAPIGateway.Session.Player.IdentityId);
            if (faction == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Must be in a faction!");
                return;
            }

            var manager = PersistentFactionObjectives.I.GetFactionManger(faction.FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            manager.SilencePlayer(MyAPIGateway.Session.Player.IdentityId);
        }

        public static void HandleClearObjectives(string[] args)
        {
            if (!IsFactionLeaderOrFounder())
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "Only faction leaders or founders can clear all objectives.");
                return;
            }

            var manager = PersistentFactionObjectives.I.GetFactionManger(FactionId);
            if (manager == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "You must be in a faction to run this command.");
                return;
            }

            if (manager.Objectives.Count == 0)
            {
                MyAPIGateway.Utilities.ShowMessage("Objectives", "No objectives to clear.");
                return;
            }

            manager.ClearAllQuests();
        }
    }
}
