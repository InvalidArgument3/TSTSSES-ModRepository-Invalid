using System;
using System.Collections.Generic;
using Invalid.DeltaVQuestLog.Commands;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Invalid.DeltaVQuestLog
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class PersistentFactionObjectives : MySessionComponentBase
    {
        public static PersistentFactionObjectives I;

        private const string FileName = "FactionObjectives.txt";

        private Dictionary<long, QuestLogManager> _factionObjectives = new Dictionary<long, QuestLogManager>();
        private bool isServer;

        internal QuestLogNetworking Networking;

        public override void LoadData()
        {
            I = this;
            isServer = MyAPIGateway.Multiplayer.IsServer;
            bool isDedicated = MyAPIGateway.Utilities.IsDedicated;

            if (isServer && !isDedicated)
            {
                LoadObjectives();
            }
            else if (isDedicated)
            {
                LoadObjectives();
            }

            if (isServer)
            {
                MyAPIGateway.Session.Factions.FactionStateChanged += FactionsOnFactionStateChanged;
                MyVisualScriptLogicProvider.PlayerConnected += OnPlayerConnected;
            }

            CommandHandler.Init();
            Networking = new QuestLogNetworking();
        }

        private void FactionsOnFactionStateChanged(MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            if (action == MyFactionStateChange.FactionMemberAcceptJoin)
            {
                GetFactionManger(fromFactionId)?.SendNetworkUpdate();
            }
            if (action == MyFactionStateChange.FactionMemberLeave || action == MyFactionStateChange.FactionMemberKick)
            {
                MyVisualScriptLogicProvider.SetQuestlog(false, "", playerId);
            }
        }

        protected override void UnloadData()
        {
            Networking.Close();
            CommandHandler.Close();

            if (isServer)
            {
                SaveObjectives();
                MyVisualScriptLogicProvider.PlayerConnected -= OnPlayerConnected;
                MyAPIGateway.Session.Factions.FactionStateChanged -= FactionsOnFactionStateChanged;
            }

            I = null;
        }

        private int _ticks;
        public override void UpdateAfterSimulation()
        {
            if (!isServer) return;
            _ticks++;

            if (_ticks % 10 == 0)
                foreach (var manager in _factionObjectives.Values)
                    manager.Update10();
        }

        private void OnPlayerConnected(long playerId)
        {
            if (!isServer) return;

            var playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            if (playerFaction == null) return;

            var factionId = playerFaction.FactionId;
            MyVisualScriptLogicProvider.SetQuestlog(false, "", playerId);
            GetFactionManger(factionId)?.SendNetworkUpdate();
        }

        public QuestLogManager GetFactionManger(long? factionId)
        {
            if (factionId == null)
                return null;

            if (!_factionObjectives.ContainsKey(factionId.Value))
                _factionObjectives[factionId.Value] = new QuestLogManager(factionId.Value);
            return _factionObjectives[factionId.Value];
        }

        public void UpdateManager(QuestLogManager manager)
        {
            if (manager == null)
                return;

            _factionObjectives[manager.FactionId] = manager;
            manager.UpdateFactionQuestlog(isNetworkUpdate: true);
        }

        public static bool IsFactionLeaderOrFounder(long factionId, long playerId)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
            if (faction == null)
            {
                MyLog.Default.WriteLineAndConsole($"Faction not found for ID {factionId}");
                return false;
            }

            MyFactionMember member;
            if (faction.Members.TryGetValue(playerId, out member))
            {
                return member.IsLeader || member.IsFounder;
            }

            MyLog.Default.WriteLineAndConsole($"Player {playerId} is not a member of faction {factionId}");
            return false;
        }

        private void SaveObjectives()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(PersistentFactionObjectives)))
                {
                    foreach (var kvp in _factionObjectives)
                    {
                        foreach (var obj in kvp.Value.Objectives)
                        {
                            writer.WriteLine($"{kvp.Key}:{obj}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error saving objectives: {ex.Message}");
            }
        }

        private void LoadObjectives()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(PersistentFactionObjectives)))
                    return;

                using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(PersistentFactionObjectives)))
                {
                    string line;
                    long factionId;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(new[] { ':' }, 2);
                        if (parts.Length == 2 && long.TryParse(parts[0], out factionId))
                        {
                            if (!_factionObjectives.ContainsKey(factionId))
                                _factionObjectives[factionId] = new QuestLogManager(factionId);

                            if (!_factionObjectives[factionId].Objectives.Contains(parts[1]))
                                _factionObjectives[factionId].Objectives.Add(parts[1]);
                        }
                    }

                    foreach (var objective in _factionObjectives.Values)
                        objective.UpdateFactionQuestlog();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Error loading objectives: {ex.Message}");
            }
        }

    }

}
