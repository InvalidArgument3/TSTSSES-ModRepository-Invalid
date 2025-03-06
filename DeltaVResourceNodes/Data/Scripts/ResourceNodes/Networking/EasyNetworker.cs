using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Math0424.Networking
{

    /// <summary>
    /// Author: Math0424
    /// Version 1.0
    /// </summary>
    public class EasyNetworker
    {

        private readonly ushort CommsId;
        public List<IMyPlayer> TempPlayers { get; private set; }
        
        /// <summary>
        /// Final packet in
        /// </summary>
        public Action<PacketIn> OnRecievedPacket;

        /// <summary>
        /// Before sending to players, serverside only
        /// </summary>
        public Action<PacketIn> ProcessPacket;

        public EasyNetworker(ushort CommsId)
        {
            this.CommsId = CommsId;
            TempPlayers = null;
        }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(CommsId, RecivedPacket);
        }

        public void UnRegister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(CommsId, RecivedPacket);
        }

        public void TransmitToServer(IPacket data, bool SendToAllPlayers = true, bool SendToSender = false)
        {
            PacketBase packet = new PacketBase(data.GetId(), SendToSender);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet));
        }

        public void TransmitToPlayer(IPacket data, ulong playerId)
        {
            PacketBase packet = new PacketBase(data.GetId(), false);
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), playerId);
        }

        public void TransmitToPlayersWithinRange(Vector3D pos, IPacket data, bool SendToSender = false)
        {
            TransmitToPlayersWithinRange(pos, data, MyAPIGateway.Session.SessionSettings.SyncDistance);
        }

        public void TransmitToPlayersWithinRange(Vector3D pos, IPacket data, double range, bool SendToSender = false)
        {
            PacketBase packet = new PacketBase(data.GetId(), SendToSender);
            packet.Range = range;
            packet.TransmitLocation = pos;
            packet.Wrap(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet));
        }

        private void RecivedPacket(ushort handler, byte[] raw, ulong id, bool isFromServer)
        {
            try
            {
                PacketBase packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(raw);
                PacketIn packetIn = new PacketIn(packet.Id, packet.Data, id, isFromServer);

                ProcessPacket?.Invoke(packetIn);
                if (packetIn.IsCancelled)
                {
                    return;
                }

                if (MyAPIGateway.Session.IsServer)
                {
                    TransmitPacketToAllPlayers(id, packet);
                }
                

                if ((!isFromServer && MyAPIGateway.Session.IsServer) ||
                    (isFromServer && (!MyAPIGateway.Session.IsServer || packet.SendToSender)) ||
                    (isFromServer && MyAPIGateway.Session.IsServer))
                {
                    OnRecievedPacket?.Invoke(packetIn);
                }

            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"Malformed packet from {id}!");
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
            }
        }

        private void TransmitPacketToAllPlayers(ulong sender, PacketBase packet)
        {
            if (TempPlayers == null)
                TempPlayers = new List<IMyPlayer>(MyAPIGateway.Session.SessionSettings.MaxPlayers);
            else
                TempPlayers.Clear();

            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (var p in TempPlayers)
            {
                if (p.IsBot || (packet.SendToSender && MyAPIGateway.Utilities.IsDedicated) || (!packet.SendToSender && p.SteamUserId == sender))
                    continue;
                
                if (packet.Range != -1)
                {
                    if (packet.Range >= Vector3D.Distance(p.GetPosition(), packet.TransmitLocation))
                    {
                        MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), p.SteamUserId);
                    }
                }
                else
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(CommsId, MyAPIGateway.Utilities.SerializeToBinary(packet), p.SteamUserId);
                }
            }
        }

        [ProtoContract]
        private class PacketBase
        {

            [ProtoMember(1)]
            public readonly int Id;
            [ProtoMember(2)]
            public double Range = -1;
            [ProtoMember(3)]
            public Vector3D TransmitLocation;
            [ProtoMember(4)]
            public readonly bool SendToSender;

            [ProtoMember(5)]
            public byte[] Data;

            public PacketBase() { }

            public PacketBase(int Id, bool SendToSender)
            {
                this.Id = Id;
                this.SendToSender = SendToSender;
            }

            public void Wrap(object data)
            {
                Data = MyAPIGateway.Utilities.SerializeToBinary(data);
            }
        }

        public interface IPacket
        {
            int GetId();
        }

        public class PacketIn {
            public bool IsCancelled { protected set; get; }
            public int PacketId { protected set; get; }
            public ulong SenderId { protected set; get; }
            public bool IsFromServer { protected set; get; }
            
            private readonly byte[] Data;

            public PacketIn(int packetId, byte[] data, ulong senderId, bool isFromServer)
            {
                this.PacketId = packetId;
                this.SenderId = senderId;
                this.IsFromServer = isFromServer;
                this.Data = data;
            }

            public T UnWrap<T>()
            {
                return MyAPIGateway.Utilities.SerializeFromBinary<T>(Data);
            }

            public void SetCancelled(bool value)
            {
                this.IsCancelled = value;
            }
        }

    }
}
