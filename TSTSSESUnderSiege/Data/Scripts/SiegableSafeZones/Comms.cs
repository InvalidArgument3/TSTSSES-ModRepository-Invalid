using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace SiegableSafeZones
{
    public enum DataType
    {

        RequestConfig,
        SendConfig,
        SyncSettings,
        RequestSettings,
        SendSettings,
        RemoveBlockCache,
        SendToChat,
        BeginSiege,

    }

    [ProtoContract]
    public class ObjectContainer
    {
        [ProtoMember(1)] public Config config;
        [ProtoMember(2)] public ZoneBlockSettings zoneBlockSettings;
        [ProtoMember(3)] public ulong steamId;
        [ProtoMember(4)] public long entityId;
        [ProtoMember(5)] public string chatMessage;
        [ProtoMember(6)] public Color chatColor;
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
            Type = DataType.RequestConfig;
            Data = new byte[0];
        }

        public CommsPackage(DataType type, ObjectContainer oc)
        {
            Type = type;
            Data = MyAPIGateway.Utilities.SerializeToBinary(oc);
        }
    }

    public static class Comms
    {
        private static readonly ushort handler = Session.Instance.NetworkId;

        public static void ClientBeginSiege(ZoneBlockSettings settings)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                zoneBlockSettings = settings
            };

            CommsPackage package = new CommsPackage(DataType.BeginSiege, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void ClientRequestBlockSettings(ulong steamId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                steamId = steamId,
            };

            CommsPackage package = new CommsPackage(DataType.RequestSettings, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void SendBlockSettingsToSingleClient(ZoneBlockSettings settings, ulong steamId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                zoneBlockSettings = settings
            };

            CommsPackage package = new CommsPackage(DataType.SendSettings, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageTo(handler, sendData, steamId);
        }

        public static void SendBlockSettingsToClients(ZoneBlockSettings settings)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                zoneBlockSettings = settings
            };
            
            CommsPackage package = new CommsPackage(DataType.SendSettings, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToOthers(handler, sendData);
        }

        public static void RequestConfig(ulong steamId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                steamId = steamId
            };

            CommsPackage package = new CommsPackage(DataType.RequestConfig, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void SendConfig(ulong steamId, Config config)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                config = config
            };

            CommsPackage package = new CommsPackage(DataType.SendConfig, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageTo(handler, sendData, steamId);
        }

        public static void SendRemoveBlockFromCache(long entityId)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                entityId = entityId
            };

            CommsPackage package = new CommsPackage(DataType.RemoveBlockCache, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToOthers(handler, sendData);
        }

        public static void SyncSettings(ZoneBlockSettings settings)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                zoneBlockSettings = settings
            };

            CommsPackage package = new CommsPackage(DataType.SyncSettings, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToOthers(handler, sendData);
        }

        public static void SendMessageToChat(string message, Color color)
        {
            ObjectContainer oc = new ObjectContainer()
            {
                chatMessage = message,
                chatColor = color
            };

            CommsPackage package = new CommsPackage(DataType.SendToChat, oc);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(handler, sendData);
        }

        public static void MessageHandler(byte[] data)
        {
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                if (package == null) return;

                // To everyone/single client
                if (package.Type == DataType.SendSettings)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    if (Session.Instance.zoneBlockSettingsCache.ContainsKey(packet.zoneBlockSettings.ZoneBlockEntityId)) return;

                    IMyEntity entity;
                    if (MyAPIGateway.Entities.TryGetEntityById(packet.zoneBlockSettings.ZoneBlockEntityId, out entity))
                        packet.zoneBlockSettings.Block = entity as IMySafeZoneBlock;

                    Session.Instance.zoneBlockSettingsCache.Add(packet.zoneBlockSettings.ZoneBlockEntityId, packet.zoneBlockSettings);
                    return;
                }

                // To server
                if (package.Type == DataType.BeginSiege)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    if (packet.zoneBlockSettings.JDBlock == null) return;
                    Utils.TakeTokens(packet.zoneBlockSettings.JDBlock, packet.zoneBlockSettings);
                    Utils.DrainAllJDs(packet.zoneBlockSettings.JDBlock);
                    return;
                }

                // To server
                if (package.Type == DataType.RequestConfig)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    SendConfig(packet.steamId, Session.Instance.config);
                    return;
                }

                // To client
                if (package.Type == DataType.SendConfig)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Session.Instance.config = packet.config;
                    return;
                }

                // To everyone
                if (package.Type == DataType.RemoveBlockCache)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    Session.Instance.zoneBlockSettingsCache.Remove(packet.entityId);
                    return;
                }

                // To everyone
                if (package.Type == DataType.SyncSettings) 
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    ZoneBlockSettings settings;
                    if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(packet.zoneBlockSettings.ZoneBlockEntityId, out settings)) return;

                    // Temp store this data when overwriting the data in the class so this data can be restored because its null when passed over network.
                    NonSerializedData temp = settings.NSD;

                    // Sync data here
                    Session.Instance.zoneBlockSettingsCache[packet.zoneBlockSettings.ZoneBlockEntityId] = packet.zoneBlockSettings;

                    // Restore non-serialized data
                    Session.Instance.zoneBlockSettingsCache[packet.zoneBlockSettings.ZoneBlockEntityId].NSD = temp;

                    if (Session.Instance.isServer)
                        Session.Instance.SaveSafeZoneSettings(settings);

                    settings.Block?.RefreshCustomInfo();

                    return;
                }

                // To server
                if (package.Type == DataType.SendToChat)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    new ModMessage($"{packet.chatMessage}", packet.chatColor);
                }

                // To server
                if (package.Type == DataType.RequestSettings)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    foreach(var settings in Session.Instance.zoneBlockSettingsCache.Values)
                        SendBlockSettingsToSingleClient(settings, packet.steamId);

                    return;
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
