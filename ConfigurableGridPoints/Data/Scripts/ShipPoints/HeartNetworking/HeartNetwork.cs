using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace CGP.ShareTrack.HeartNetworking
{
    public class HeartNetwork
    {
        public static HeartNetwork I;

        private int _networkLoadUpdate;

        public int NetworkLoadTicks = 240;


        private readonly List<IMyPlayer> TempPlayers = new List<IMyPlayer>();
        public Dictionary<Type, int> TypeNetworkLoad = new Dictionary<Type, int>();

        public ushort NetworkId { get; private set; }
        public int TotalNetworkLoad { get; private set; }

        public void LoadData(ushort networkId) {
            if (I != null)
                return;

            I = this;
            NetworkId = networkId;

            TypeNetworkLoad.Clear();
            foreach (var type in PacketBase.Types)
                TypeNetworkLoad.Add(type, 0);

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, ReceivedPacket);
        }

        public void UnloadData() {
            if (NetworkId > 0)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, ReceivedPacket);

            TypeNetworkLoad?.Clear();
            NetworkId = 0;
            I = null;
        }

        public void Update() {
            if (MasterSession.I == null || Log._unloaded)
                return;

            _networkLoadUpdate--;
            if (_networkLoadUpdate <= 0) {
                _networkLoadUpdate = NetworkLoadTicks;
                TotalNetworkLoad = 0;

                foreach (var networkLoadArray in TypeNetworkLoad.Keys.ToArray()) {
                    TotalNetworkLoad += TypeNetworkLoad[networkLoadArray];
                    TypeNetworkLoad[networkLoadArray] = 0;
                }

                TotalNetworkLoad /= NetworkLoadTicks / 60; // Average per-second
            }
        }

        private void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer) {
            // Add check if mod is unloaded
            if (MasterSession.I == null || Log._unloaded)
                return;

            try {
                var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(serialized);
                if (packet == null)
                    return;

                if (TypeNetworkLoad.ContainsKey(packet.GetType()))
                    TypeNetworkLoad[packet.GetType()] += serialized.Length;

                HandlePacket(packet, senderSteamId);
            }
            catch (Exception ex) {
                if (!Log._unloaded)
                    Log.Error(ex);
            }
        }

        private void HandlePacket(PacketBase packet, ulong senderSteamId) {
            if (packet == null || MasterSession.I == null)
                return;

            try {
                packet.Received(senderSteamId);
            }
            catch (Exception ex) {
                if (!Log._unloaded)
                    Log.Error(ex);
            }
        }


        public KeyValuePair<Type, int> HighestNetworkLoad()
        {
            Type highest = null;

            foreach (var networkLoadArray in TypeNetworkLoad)
                if (highest == null || networkLoadArray.Value > TypeNetworkLoad[highest])
                    highest = networkLoadArray.Key;

            return new KeyValuePair<Type, int>(highest, TypeNetworkLoad[highest]);
        }

        public void SendToPlayer(PacketBase packet, ulong playerSteamId, byte[] serialized = null)
        {
            RelayToClient(packet, playerSteamId, MyAPIGateway.Session?.Player?.SteamUserId ?? 0, serialized);
        }

        public void SendToEveryone(PacketBase packet, byte[] serialized = null)
        {
            RelayToClients(packet, MyAPIGateway.Session?.Player?.SteamUserId ?? 0, serialized);
        }

        public void SendToServer(PacketBase packet, byte[] serialized = null)
        {
            RelayToServer(packet, MyAPIGateway.Session?.Player?.SteamUserId ?? 0, serialized);
        }

        private void RelayToClients(PacketBase packet, ulong senderSteamId = 0, byte[] serialized = null)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            TempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (var p in TempPlayers)
            {
                // skip sending to self (server player) or back to sender
                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == senderSteamId)
                    continue;

                if (serialized == null) // only serialize if necessary, and only once.
                    serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(NetworkId, serialized, p.SteamUserId);
            }

            TempPlayers.Clear();
        }

        private void RelayToClient(PacketBase packet, ulong playerSteamId, ulong senderSteamId,
            byte[] serialized = null)
        {
            if (playerSteamId == MyAPIGateway.Multiplayer.ServerId || playerSteamId == senderSteamId)
                return;

            if (serialized == null) // only serialize if necessary, and only once.
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(NetworkId, serialized, playerSteamId);
        }

        private void RelayToServer(PacketBase packet, ulong senderSteamId = 0, byte[] serialized = null)
        {
            if (senderSteamId == MyAPIGateway.Multiplayer.ServerId)
                return;

            if (serialized == null) // only serialize if necessary, and only once.
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, serialized);
        }
    }
}