using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRageMath;

namespace CustomHangar
{

    public enum HangarType
    {
        Private,
        Faction
    }

    public enum SpawnType
    {
        None,
        Dynamic,
        Nearby,
        SpawnArea,
        Original
    }

    [XmlRoot("HangarData")]
    public class AllHangarData
    {
        [XmlElement("FactionData")] public List<FactionData> factionData = new List<FactionData>();
        [XmlElement("PrivateData")] public List<PrivateData> privateData = new List<PrivateData>();

        public AllHangarData() { }

        public void AddFactionData(long factionId, string gridName, long gridId, long playerId, string path, string playerName, bool autoHangar)
        {
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionId);
            if (faction == null) return;

            GridData gData = new GridData()
            {
                gridId = gridId,
                gridName = gridName,
                owner = playerId,
                gridPath = path,
                ownerName = playerName,
                autoHangared = autoHangar
            };

            FactionData fData = GetFactionData(factionId);
            if (fData == null)
            {
                HangarData hData = new HangarData()
                {
                    gridData = new List<GridData>() { gData },
                    type = HangarType.Faction
                };
                
                FactionData data = new FactionData()
                {
                    factionId = factionId,
                    factionHangarData = hData,
                    factionName = faction.Name
                };

                factionData.Add(data);
            }
            else
                fData.factionHangarData.gridData.Add(gData);


        }

        public void RemoveFactionData(long factionId, int index, bool nullBlueprint = false)
        {
            if (factionData == null) return;

            foreach (var fData in factionData)
                if (fData.factionId == factionId)
                    if (index <= fData.factionHangarData.gridData.Count - 1)
                    {
                        if (nullBlueprint)
                            Session.Instance.cacheGridPaths.Add(fData.factionHangarData.gridData[index].gridPath);

                        fData.factionHangarData.gridData.RemoveAt(index);
                        return;
                    }
        }

        public int GetFactionSlots(long factionId)
        {
            if (factionData == null) return 0;

            foreach (var fData in factionData)
            {
                int slots = 0;
                if (fData.factionId == factionId)
                {
                    foreach(var gData in fData.factionHangarData.gridData)
                    {
                        if (!gData.autoHangared)
                            slots++;
                    }

                    return slots;
                }
            }

            return 0;
        }

        public FactionData GetFactionData(long factionId)
        {
            if (factionData == null) return null;

            foreach (var fData in factionData)
                if (fData.factionId == factionId) return fData;

            return null;
        }

        public GridData GetFactionGridData(long factionId, int index)
        {
            FactionData fData = GetFactionData(factionId);
            if (fData == null) return null;

            if (index >= 0)
            {
                if (index > fData.factionHangarData.gridData.Count - 1) return null;
                return fData.factionHangarData.gridData[index];
            }

            return null;
        }

        public string GetFactionsGridNames(long factionId, long playerId, bool isLeader = true)
        {
            if (factionData == null) return null;

            string names = "";
            foreach (var fData in factionData)
            {
                if (fData.factionId == factionId)
                {
                    if (fData.factionHangarData.gridData.Count == 0)
                        return names;

                    for (int i = 0; i < fData.factionHangarData.gridData.Count; i++)
                    {
                        names += "\n";
                        if (isLeader)
                            names += $"[{i}] {fData.factionHangarData.gridData[i].gridName}";
                        else
                        {
                            if (fData.factionHangarData.gridData[i].owner == playerId)
                                names += $"[{i}] {fData.factionHangarData.gridData[i].gridName}";
                        }

                        if (fData.factionHangarData.gridData[i].autoHangared)
                            names += " [AutoHangar]";
                    }

                    names += $"\nFaction Hangar Totals: {GetFactionSlots(factionId)}/{Session.Instance.config.factionHangarConfig.maxFactionSlots}";
                }
                    
            }

            return names;
        }

        public bool IsFactionGridAutoHangared(long factionId, int index)
        {
            FactionData fData = GetFactionData(factionId);
            if (fData == null) return false;

            if (index >= 0)
            {
                if (index > fData.factionHangarData.gridData.Count - 1) return false;
                return fData.factionHangarData.gridData[index].autoHangared;
            }

            return false;
        }

        public int GetPrivateSlots(long playerId)
        {
            if (privateData == null) return 0;
            foreach (var pData in privateData)
            {
                if (pData.playerId == playerId)
                {
                    int slots = 0;
                    foreach(var gData in pData.privateHangarData.gridData)
                    {
                        if (!gData.autoHangared)
                            slots++;
                    }

                    return slots;
                }
            }

            return 0;
        }

        public string GetPrivateGridNames(long playerId)
        {
            if (privateData == null) return null;

            string names = "";
            foreach (var pData in privateData)
            {
                if (pData.playerId == playerId)
                {
                    if (pData.privateHangarData.gridData.Count == 0)
                        return names;

                    for (int i = 0; i < pData.privateHangarData.gridData.Count; i++)
                    {
                        names += "\n";
                        if (pData.privateHangarData.gridData[i].owner == playerId)
                            names += $"[{i}] {pData.privateHangarData.gridData[i].gridName}";

                        if (pData.privateHangarData.gridData[i].autoHangared)
                            names += " [AutoHangar]";
                    }

                    names += $"\nPrivate Hangar Totals: {GetPrivateSlots(playerId)}/{Session.Instance.config.privateHangarConfig.maxPrivateSlots}";
                }
            }

            return names;
        }

        public PrivateData GetPrivateData(long playerId)
        {
            if (privateData == null) return null;

            foreach (var pData in privateData)
                if (pData.playerId == playerId) return pData;

            return null;
        }

        public GridData GetPrivateGridData(long playerId, int index)
        {
            PrivateData pData = GetPrivateData(playerId);
            if (pData == null) return null;

            if (index >= 0)
            {
                if (index > pData.privateHangarData.gridData.Count - 1) return null;
                return pData.privateHangarData.gridData[index];
            }

            return null;
        }

        public bool IsPrivateGridAutoHangared(long playerId, int index)
        {
            PrivateData pData = GetPrivateData(playerId);
            if (pData == null) return false;

            if (index >= 0)
            {
                if (index > pData.privateHangarData.gridData.Count - 1) return false;
                return pData.privateHangarData.gridData[index].autoHangared;
            }

            return false;
        }

        public bool TransferFactionToPrivate(long factionId, int index, long playerId)
        {
            if (!Session.Instance.config.factionHangarConfig.factionToPrivateTransfer)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Faction to private transfers are not allowed.", Color.Red, "[FactionHangar]", playerId, "Red");
                return false;
            }

            GridData gridData = GetFactionGridData(factionId, index);
            if (gridData == null) return false;

            if (gridData.owner != playerId)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"You can only transfer grids that you own.", Color.Red, "[FactionHangar]", playerId, "Red");
                return false;
            }

            if (GetPrivateSlots(playerId) >= Session.Instance.config.privateHangarConfig.maxPrivateSlots)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to transfer grid, you have exceeded your private hangar slots.", Color.Red, "[FactionHangar]", playerId, "Red");
                return false;
            }

            RemoveFactionData(factionId, index);
            AddPrivateData(gridData.gridName, gridData.gridId, playerId, gridData.gridPath, gridData.ownerName, gridData.autoHangared);
            return true;
        }

        public bool TransferPrivateToFaction(long factionId, int index, long playerId)
        {
            if (!Session.Instance.config.privateHangarConfig.privateToFactionTransfer)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Private to faction transfers are not allowed.", Color.Red, "[FactionHangar]", playerId, "Red");
                return false;
            }

            GridData gridData = GetPrivateGridData(playerId, index);
            if (gridData == null) return false;

            if (GetFactionSlots(factionId) >= Session.Instance.config.factionHangarConfig.maxFactionSlots)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to transfer grid, you have exceeded the faction hangar slots.", Color.Red, "[FactionHangar]", playerId, "Red");
                return false;
            }

            RemovePrivateData(playerId, index);
            AddFactionData(factionId, gridData.gridName, gridData.gridId, playerId, gridData.gridPath, gridData.ownerName, gridData.autoHangared);
            return true;
        }

        public void AddPrivateData(string gridName, long gridId, long playerId, string path, string playerName, bool autoHangar)
        {
            GridData gData = new GridData()
            {
                gridId = gridId,
                gridName = gridName,
                owner = playerId,
                gridPath = path,
                ownerName = playerName,
                autoHangared = autoHangar
            };

            PrivateData pData = GetPrivateData(playerId);
            if (pData == null)
            {
                HangarData hData = new HangarData()
                {
                    gridData = new List<GridData>() { gData },
                    type = HangarType.Private
                };

                PrivateData data = new PrivateData()
                {
                    privateHangarData = hData,
                    playerId = playerId,
                    playerNameRef = playerName,
                };

                privateData.Add(data);
            }
            else
                pData.privateHangarData.gridData.Add(gData);
        }

        public void RemovePrivateData(long playerId, int index, bool nullBlueprint = false)
        {
            if (privateData == null) return;

            foreach (var pData in privateData)
                if (pData.playerId == playerId)
                    if (index <= pData.privateHangarData.gridData.Count - 1)
                    {
                        if (nullBlueprint)
                            Session.Instance.cacheGridPaths.Add(pData.privateHangarData.gridData[index].gridPath);

                        pData.privateHangarData.gridData.RemoveAt(index);
                        return;
                    }
        }

        public static AllHangarData LoadHangarData()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("FactionHangarStorage.xml", typeof(AllHangarData)) == true)
            {
                try
                {
                    AllHangarData data = new AllHangarData();
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("FactionHangarStorage.xml", typeof(AllHangarData));
                    string content = reader.ReadToEnd();

                    reader.Close();
                    return data = MyAPIGateway.Utilities.SerializeFromXML<AllHangarData>(content);
                }
                catch (Exception ex)
                {
                    return new AllHangarData();
                }
            }

            return new AllHangarData();
        }
    }

    public class FactionData
    {
        [XmlElement("Faction")] public string factionName;
        [XmlElement("FactionId")] public long factionId;
        [XmlElement("FactionHangarData")] public HangarData factionHangarData;
    }

    public class PrivateData
    {
        [XmlElement("Private")] public HangarData privateHangarData;
        [XmlElement("PlayerId")] public long playerId;
        [XmlElement("Player")] public string playerNameRef;
    }

    public class HangarData
    {
        [XmlElement("HangarType")] public HangarType type;
        [XmlElement("StoredGrid")] public List<GridData> gridData = new List<GridData>();
    }

    [ProtoContract]
    public class HangarDelayData
    {
        [ProtoMember(1)] public long playerId;
        [ProtoMember(2)] public List<GridData> gridData;
        [ProtoMember(3)] public int timer;
        [ProtoMember(4)] public string playerName;
        [ProtoMember(5)] public HangarType hangarType;
        [ProtoMember(6)] public long requesterId;
    }

    [ProtoContract]
    public class GridData
    {
        [XmlElement("GridId")] [ProtoMember(1)] public long gridId;
        [XmlElement("GridName")] [ProtoMember(2)] public string gridName;
        [XmlElement("StoredPath")] [ProtoMember(3)] public string gridPath;
        [XmlElement("Owner")] [ProtoMember(4)] public long owner;
        [XmlElement("OwnerName")] [ProtoMember(5)] public string ownerName;
        [XmlElement("AutoHangared")][ProtoMember(6)] public bool autoHangared;
    }

    public class CacheGridsForStorage
    {
        public long ownerId;
        public long requesterId;
        public List<IMyCubeGrid> grids;
    }
}

