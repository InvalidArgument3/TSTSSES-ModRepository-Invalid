using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using Sandbox.Game;
using VRageMath;
using Sandbox.Engine.Utils;
using VRage.ObjectBuilders;
using System.Reflection;

namespace CustomHangar
{

    public enum DataType
    {
        Sync,
        StoreGrid,
        FactionList,
        PrivateList,
        LoadFactionGrid,
        RequestGridData,
        SendGridData,
        SendObToSpawn,
        RequestConfig,
        SendConfig,
        ClientRequestTransfer,
        AddClientCooldown,
        UpdateIdentities,
        RequestGridRemoval,
        FactionToPrivateTransfer,
        PrivateToFactionTransfer,
        SendChatMessage,
        AddTime

    }

    [ProtoContract]
    public class ObjectContainer
    {
        [ProtoMember(1)] public Config settings;
        [ProtoMember(2)] public long playerId;
        [ProtoMember(3)] public List<GridData> gridData;
        [ProtoMember(4)] public string stringData;
        [ProtoMember(5)] public bool boolValue;
        [ProtoMember(6)] public int intValue;
        [ProtoMember(7)] public HangarType hangarType;
        [ProtoMember(8)] public long factionId;
        [ProtoMember(9)] public ulong steamId;
        [ProtoMember(10)] public MyObjectBuilder_Base ob;
        [ProtoMember(11)] public List<MyObjectBuilder_CubeGrid> cubeGridObs;
        [ProtoMember(12)] public Config config;
        [ProtoMember(13)] public TimerType timerType;
        [ProtoMember(14)] public List<MyObjectBuilder_Identity> identityObs;
        [ProtoMember(15)] public long requesterId;
        [ProtoMember(16)] public bool originalLocation;
        [ProtoMember(17)] public long cost;
        [ProtoMember(18)] public SpawnType spawnType;
        [ProtoMember(19)] public bool force;
    }

    [ProtoContract]
    public class ChatMessage
    {
        [ProtoMember(1)] public string message;
        [ProtoMember(2)] public string color;
        [ProtoMember(3)] public long playerId;
        [ProtoMember(4)] public Color col;
    }

    [ProtoContract]
    public class CommsPackage
    {
        [ProtoMember(1)]
        public DataType Type;

        [ProtoMember(2)]
        public byte[] Data;

        public CommsPackage()
        {
            Type = DataType.Sync;
            Data = new byte[0];
        }

        public CommsPackage(DataType type, ObjectContainer oc)
        {
            Type = type;
            Data = MyAPIGateway.Utilities.SerializeToBinary(oc);
        }

        public CommsPackage(DataType type, ChatMessage cm)
        {
            Type = type;
            Data = MyAPIGateway.Utilities.SerializeToBinary(cm);
        }
    }

    public static class Comms
    {
        private static readonly ushort handler = Session.Instance.NetworkHandle;

        public static void MessageHandler(byte[] data)
        {
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                if (package == null) return;

                // Server
                if (package.Type == DataType.StoreGrid)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Utils.Storegrid(packet);
                    return;
                }

                // Server
                if (package.Type == DataType.FactionList)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Utils.GetFactionList(packet);
                    return;
                }

                // Server
                if (package.Type == DataType.PrivateList)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Utils.GetPrivateList(packet);
                    return;
                }

                // Server
                if (package.Type == DataType.RequestGridRemoval)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Utils.TryHangarGridRemoval(packet);
                    return;
                }

                // Server
                if (package.Type == DataType.FactionToPrivateTransfer)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
                    if (faction != null)
                        Session.Instance.allHangarData.TransferFactionToPrivate(faction.FactionId, packet.intValue, packet.playerId);

                    return;
                }

                // Server
                if (package.Type == DataType.PrivateToFactionTransfer)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
                    if (faction != null)
                        Session.Instance.allHangarData.TransferPrivateToFaction(faction.FactionId, packet.intValue, packet.playerId);

                    return;
                }

                // Server
                if (package.Type == DataType.RequestGridData)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Utils.GetGridData(packet);
                    return;
                }

                // Client
                if (package.Type == DataType.SendGridData)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    MyObjectBuilder_CubeGrid[] cubeGridObs = Session.Instance.GetGridFromGridData(packet.ob, packet.intValue, packet.hangarType);
                    if (cubeGridObs == null)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to get grids", Color.Red, "[FactionHangar]", MyAPIGateway.Session.Player.IdentityId, "Red");
                        return;
                    }

                    Session.Instance.SpawnClientSideProjectedGrid(cubeGridObs);
                }

                // Server
                if (package.Type == DataType.SendObToSpawn)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Session.Instance.SpawnGridsFromOb(packet.cubeGridObs, packet.intValue, packet.hangarType, packet.playerId, packet.cost, packet.spawnType, false);
                }

                // Server
                if (package.Type == DataType.RequestConfig)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    ServerSendConfig(Session.Instance.config, packet.steamId);
                }

                // Client
                if (package.Type == DataType.SendConfig)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Session.Instance.config = packet.config;
                    foreach(var area in packet.config.spawnAreas)
                    {
                        if (area.inverseArea)
                        {
                            Session.Instance.useInverseSpawnArea = true;
                            return;
                        }
                    }
                }

                // Server
                if (package.Type == DataType.ClientRequestTransfer)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(packet.playerId);
                    if (faction == null)
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to transfer grids.", Color.Red, "[FactionHangar]", packet.playerId, "Red");
                        return;
                    }

                    if (packet.hangarType == HangarType.Faction)
                        Session.Instance.allHangarData.TransferPrivateToFaction(faction.FactionId, packet.intValue, packet.playerId);
                    else
                        Session.Instance.allHangarData.TransferFactionToPrivate(faction.FactionId, packet.intValue, packet.playerId);

                    return;
                }

                // Client
                if (package.Type == DataType.AddClientCooldown)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    TimerType type = packet.timerType;

                    if (type == TimerType.StorageCooldown)
                        Session.Instance.storeTimer = packet.intValue;

                    if (type == TimerType.RetrievalCooldown)
                        Session.Instance.retrievalTimer = packet.intValue;
                }

                // Server
                if (package.Type == DataType.AddTime)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(packet.factionId);
                    if (faction == null) return;

                    FactionTimers.AddTimer(faction, packet.timerType, packet.intValue);
                }

                // All Clients
                if (package.Type == DataType.UpdateIdentities)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Session.Instance.allIdentities = packet.identityObs;
                }

                // Server
                if (package.Type == DataType.SendChatMessage)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ChatMessage>(package.Data);
                    if (packet == null) return;

                    MyVisualScriptLogicProvider.SendChatMessageColored($"{packet.message}", packet.col, "[FactionHangar]", packet.playerId, $"{packet.color}");
                    return;
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void ClientRequestStoreGrid(long requesterId, long playerId, List<GridData> gridData, string playerName, HangarType hangarType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                requesterId = requesterId,
                playerId = playerId,
                gridData = gridData,
                stringData = playerName,
                hangarType = hangarType
            };

            CommsPackage package = new CommsPackage(DataType.StoreGrid, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientRequestFactionList(long playerId, string playerName)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                playerId = playerId,
                stringData = playerName,
            };

            CommsPackage package = new CommsPackage(DataType.FactionList, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientRequestPrivateList(long playerId, string playerName)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                playerId = playerId,
                stringData = playerName
            };

            CommsPackage package = new CommsPackage(DataType.PrivateList, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientLoadGrid(int index, long playerId, HangarType hangarType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId,
                hangarType = hangarType
            };

            CommsPackage package = new CommsPackage(DataType.LoadFactionGrid, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientRequestGridData(int index, long playerId, HangarType hangarType, ulong steamId, bool original, bool force)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId,
                hangarType = hangarType,
                steamId = steamId,
                originalLocation = original,
                force = force
            };

            CommsPackage package = new CommsPackage(DataType.RequestGridData, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void SendOBToClient(MyObjectBuilder_Base data, ulong steamId, int index, HangarType hangarType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                ob = data,
                intValue = index,
                hangarType = hangarType
            };

            CommsPackage package = new CommsPackage(DataType.SendGridData, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageTo(handler, sendData, steamId);
        }

        public static void SendGridsToSpawn(List<MyObjectBuilder_CubeGrid> obs, int index, HangarType hangarType, long playerId, long cost, SpawnType spawnType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                cubeGridObs = obs,
                intValue = index,
                hangarType = hangarType,
                playerId = playerId,
                cost = cost,
                spawnType = spawnType
            };

            CommsPackage package = new CommsPackage(DataType.SendObToSpawn, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientRequestConfig(ulong steamId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                steamId = steamId
            };

            CommsPackage package = new CommsPackage(DataType.RequestConfig, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ServerSendConfig(Config config, ulong steamId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                config = config
            };

            CommsPackage package = new CommsPackage(DataType.SendConfig, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageTo(handler, sendData, steamId);
        }

        public static void ClientRequestTransfer(int index, long playerId, HangarType hangarType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId,
                hangarType = hangarType
            };

            CommsPackage package = new CommsPackage(DataType.ClientRequestTransfer, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void AddClientCooldown(ulong steamId, bool privateStorage, TimerType type)
        {
            if (steamId == 0) return;
            int cooldown = 0;
            if (type == TimerType.StorageCooldown)
            {
                if (privateStorage)
                    cooldown = Session.Instance.config.privateHangarConfig.privateHangarCooldown;
                else
                    cooldown = Session.Instance.config.factionHangarConfig.factionHangarCooldown;
            }

            if (type == TimerType.RetrievalCooldown)
            {
                if (privateStorage)
                    cooldown = Session.Instance.config.privateHangarConfig.privateRetrievalCooldown;
                else
                    cooldown = Session.Instance.config.factionHangarConfig.factionRetrievalCooldown;
            }

            ObjectContainer oc = new ObjectContainer()
            {
                intValue = cooldown,
                timerType = type
            };

            CommsPackage package = new CommsPackage(DataType.AddClientCooldown, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageTo(handler, sendData, steamId);
        }

        public static void SendIdentitiesToClients(List<MyObjectBuilder_Identity> list)
        {
            if (list == null)
                return;

            ObjectContainer oc = new ObjectContainer()
            {
                identityObs = new List<MyObjectBuilder_Identity>(list)
            };

            CommsPackage package = new CommsPackage(DataType.UpdateIdentities, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToOthers(handler, sendData);
        }

        public static void RequestGridRemoval(int index, long playerId, HangarType hangarType)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId,
                hangarType = hangarType
            };

            CommsPackage package = new CommsPackage(DataType.RequestGridRemoval, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void RequestTransferFactionToPrivate(long playerId, int index)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId
            };

            CommsPackage package = new CommsPackage(DataType.FactionToPrivateTransfer, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void RequestTransferPrivateToFaction(long playerId, int index)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                intValue = index,
                playerId = playerId
            };

            CommsPackage package = new CommsPackage(DataType.PrivateToFactionTransfer, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void SendChatMessage(string message, string color, long playerId, Color col)
        {
            ChatMessage cm = new ChatMessage()
            {
                message = message,
                color = color,
                playerId = playerId,
                col = col
            };

            CommsPackage package = new CommsPackage(DataType.SendChatMessage, cm);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void AddTimer(long factionId, TimerType timerType, int timeAmt)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                factionId = factionId,
                timerType = timerType,
                intValue = timeAmt
            };

            CommsPackage package = new CommsPackage(DataType.AddTime, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }
    }
}
