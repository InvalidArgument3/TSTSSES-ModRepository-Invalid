using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Invalid.DeltaVQuestLog
{
    /// <summary>
    /// Manages questlogs for one faction. Should only be run on the server.
    /// </summary>
    [ProtoContract]
    public class QuestLogManager
    {
        [ProtoMember(1)] public List<string> Objectives = new List<string>();
        [ProtoMember(2)] public Dictionary<string, DateTime> TemporaryObjectives = new Dictionary<string, DateTime>();
        [ProtoMember(3)] public long FactionId { get; private set; }
        [ProtoMember(4)] public double? ForceShowTime;
        /// <summary>
        /// All playerIds with notifications off.
        /// </summary>
        [ProtoMember(5)] public HashSet<long> SilencedPlayers = new HashSet<long>();

        public IMyFaction Faction => MyAPIGateway.Session.Factions.TryGetFactionById(FactionId);
        public IEnumerable<long> Players => Faction.Members.Values.Select(m => m.PlayerId);

        private QuestLogManager() { }

        public QuestLogManager(long factionId)
        {
            FactionId = factionId;
        }

        public void Update10()
        {
            // Temporary objectives
            {
                List<string> objectivesToRemove = new List<string>();
                foreach (var objective in TemporaryObjectives)
                    if (objective.Value < DateTime.UtcNow)
                        objectivesToRemove.Add(objective.Key);

                foreach (var objective in objectivesToRemove)
                {
                    Objectives.Remove(objective);
                    TemporaryObjectives.Remove(objective);
                }
            }
            
            // Force showing (broadcast)
            if (ForceShowTime != null)
            {
                ForceShowTime -= 1 / 6f;
                if (ForceShowTime > 0)
                    UpdateFactionQuestlog($"Faction Objectives [Force-Enabled for {ForceShowTime:N0}s]", true, true);
                else
                {
                    UpdateFactionQuestlog(isNetworkUpdate: !MyAPIGateway.Session.IsServer);
                    ForceShowTime = null;
                }
            }
        }

        public void ForceShow(double duration)
        {
            ForceShowTime = duration;
            UpdateFactionQuestlog($"Faction Objectives [Force-Enabled for {duration}s]");
        }

        public void AddQuest(string quest)
        {
            Objectives.Add(quest);
            UpdateFactionQuestlog();
        }

        public void AddTemporaryQuest(string quest, double duration) // TODO unused
        {
            Objectives.Add(quest);
            TemporaryObjectives.Add(quest, DateTime.UtcNow.AddSeconds(duration));
            UpdateFactionQuestlog();
        }

        /// <summary>
        /// Removes a given quest by title.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns>True if succeeded, otherwise false.</returns>
        public bool RemoveQuest(string quest)
        {
            bool didRemove = Objectives.Remove(quest);
            TemporaryObjectives.Remove(quest);
            if (didRemove)
                UpdateFactionQuestlog();
            return didRemove;
        }

        public void ClearAllQuests()
        {
            Objectives.Clear();
            TemporaryObjectives.Clear();
            UpdateFactionQuestlog();
        }

        /// <summary>
        /// Removes a given quest by offset index.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns>True if succeeded, otherwise false.</returns>
        public bool RemoveQuest(int index)
        {
            bool didRemove = index > 0 && index <= Objectives.Count;
            if (didRemove)
            {
                Objectives.RemoveAt(index - 1);
                UpdateFactionQuestlog();
            }
            return didRemove;
        }

        public void SilencePlayer(long id)
        {
            if (!SilencedPlayers.Add(id))
                SilencedPlayers.Remove(id);
            UpdateFactionQuestlog();
            SendNetworkUpdate();
        }

        public void UpdateFactionQuestlog(string title = "Faction Objectives", bool forceVisible = false, bool isNetworkUpdate = false)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                foreach (var player in Players)
                    if (player != -1)
                        UpdatePlayerQuestlog(title, player, forceVisible);
            }

            if (!isNetworkUpdate)
                SendNetworkUpdate();
        }

        public void UpdatePlayerQuestlog(string title = "Faction Objectives", long playerId = -1, bool forceVisible = false)
        {
            if ((!forceVisible && SilencedPlayers.Contains(playerId)) || Objectives == null || Objectives.Count == 0)
            {
                MyVisualScriptLogicProvider.SetQuestlog(false, "", playerId);
                return;
            }

            MyVisualScriptLogicProvider.SetQuestlog(true, title, playerId);
            MyVisualScriptLogicProvider.RemoveQuestlogDetails(playerId);

            foreach (var objective in Objectives)
            {
                MyVisualScriptLogicProvider.AddQuestlogObjective(objective, false, !forceVisible, playerId);
            }
        }

        public void SendNetworkUpdate()
        {
            PersistentFactionObjectives.I.Networking.SendMessageToAll(this);
        }
    }
}
