using ProtoBuf;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Utils;
using VRage.ModAPI;
using System;

namespace TeleportMechanisms
{
    [ProtoContract]
    public class JumpInitiatedMessage
    {
        [ProtoMember(1)]
        public long GatewayId { get; set; }
        [ProtoMember(2)]
        public double JumpDistance { get; set; }
        [ProtoMember(3)]
        public int CountdownTicks { get; set; }
        [ProtoMember(4)]
        public float PowerRequired { get; set; }
    }

    [ProtoContract]
    public class TeleportRequestMessage
    {
        [ProtoMember(1)]
        public ulong PlayerId { get; set; }
        [ProtoMember(2)]
        public long SourceGatewayId { get; set; }
        [ProtoMember(3)]
        public string TeleportLink { get; set; }
    }

    [ProtoContract]
    public class TeleportResponseMessage
    {
        [ProtoMember(1)]
        public ulong PlayerId { get; set; }
        [ProtoMember(2)]
        public bool Success { get; set; }
        [ProtoMember(3)]
        public Vector3D NewPosition { get; set; }
        [ProtoMember(4)]
        public SerializableMatrixD NewOrientation { get; set; }
        [ProtoMember(5)]
        public SerializableMatrixD SourceGatewayMatrix { get; set; }
        [ProtoMember(6)]
        public SerializableMatrixD DestinationGatewayMatrix { get; set; }
    }

    [ProtoContract]
    public class JumpRequestMessage
    {
        [ProtoMember(1)]
        public long GatewayId { get; set; }
        [ProtoMember(2)]
        public string Link { get; set; }
    }

    [ProtoContract]
    public class SyncSettingsMessage
    {
        [ProtoMember(1)]
        public long EntityId { get; set; }
        [ProtoMember(2)]
        public TeleportGatewaySettings Settings { get; set; }
    }



    [ProtoContract]
    public struct SerializableMatrixD
    {
        [ProtoMember(1)]
        public double M11;
        [ProtoMember(2)]
        public double M12;
        [ProtoMember(3)]
        public double M13;
        [ProtoMember(4)]
        public double M14;
        [ProtoMember(5)]
        public double M21;
        [ProtoMember(6)]
        public double M22;
        [ProtoMember(7)]
        public double M23;
        [ProtoMember(8)]
        public double M24;
        [ProtoMember(9)]
        public double M31;
        [ProtoMember(10)]
        public double M32;
        [ProtoMember(11)]
        public double M33;
        [ProtoMember(12)]
        public double M34;
        [ProtoMember(13)]
        public double M41;
        [ProtoMember(14)]
        public double M42;
        [ProtoMember(15)]
        public double M43;
        [ProtoMember(16)]
        public double M44;

        public static implicit operator MatrixD(SerializableMatrixD s)
        {
            return new MatrixD(s.M11, s.M12, s.M13, s.M14, s.M21, s.M22, s.M23, s.M24, s.M31, s.M32, s.M33, s.M34, s.M41, s.M42, s.M43, s.M44);
        }

        public static implicit operator SerializableMatrixD(MatrixD m)
        {
            return new SerializableMatrixD
            {
                M11 = m.M11,
                M12 = m.M12,
                M13 = m.M13,
                M14 = m.M14,
                M21 = m.M21,
                M22 = m.M22,
                M23 = m.M23,
                M24 = m.M24,
                M31 = m.M31,
                M32 = m.M32,
                M33 = m.M33,
                M34 = m.M34,
                M41 = m.M41,
                M42 = m.M42,
                M43 = m.M43,
                M44 = m.M44
            };
        }
    }

    public static class NetworkHandler
    {
        public const ushort TeleportRequestId = 8001;
        public const ushort TeleportResponseId = 8002;
        public const ushort JumpRequestId = 8003;
        public const ushort SyncSettingsId = 8004;
        public const ushort JumpInitiatedId = 8005;


        public static void Register()
        {
            MyLogger.Log("NetworkHandler: Register: Registering message handlers");
            MyAPIGateway.Multiplayer.RegisterMessageHandler(TeleportRequestId, HandleTeleportRequest);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(TeleportResponseId, HandleTeleportResponse);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(JumpRequestId, HandleJumpRequest);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(SyncSettingsId, HandleSyncSettings);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(JumpInitiatedId, HandleJumpInitiated);

        }

        public static void Unregister()
        {
            MyLogger.Log("NetworkHandler: Unregister: Unregistering message handlers");
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(TeleportRequestId, HandleTeleportRequest);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(TeleportResponseId, HandleTeleportResponse);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(JumpRequestId, HandleJumpRequest);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(SyncSettingsId, HandleSyncSettings);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(JumpInitiatedId, HandleJumpInitiated);

        }

        private static void HandleSyncSettings(byte[] data)
        {
            MyLogger.Log("NetworkHandler: HandleSyncSettings: called");
            SyncSettingsMessage message = MyAPIGateway.Utilities.SerializeFromBinary<SyncSettingsMessage>(data);

            TeleportGateway instance;
            if (TeleportCore._instances.TryGetValue(message.EntityId, out instance))
            {
                instance.ApplySettings(message.Settings);
                MyLogger.Log($"NetworkHandler: HandleSyncSettings: Applied settings to EntityId: {message.EntityId}");
            }
            else
            {
                MyLogger.Log($"NetworkHandler: HandleSyncSettings: No instance found for EntityId: {message.EntityId}");
            }

            if (MyAPIGateway.Multiplayer.IsServer)
            {
                // Relay the message to other clients
                MyAPIGateway.Multiplayer.SendMessageToOthers(SyncSettingsId, data);
            }
        }
        private static void HandleTeleportRequest(byte[] data)
        {
            MyLogger.Log("NetworkHandler: HandleTeleportRequest: called");
            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                MyLogger.Log("NetworkHandler: HandleTeleportRequest: Not server, ignoring TeleportRequest");
                return;
            }

            var message = MyAPIGateway.Utilities.SerializeFromBinary<TeleportRequestMessage>(data);
            TeleportCore.ServerProcessTeleportRequest(message);
        }

        private static void HandleTeleportResponse(byte[] data)
        {
            MyLogger.Log("NetworkHandler: HandleTeleportResponse: called");
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                MyLogger.Log("NetworkHandler: HandleTeleportResponse: Server received TeleportResponse, ignoring");
                return;
            }

            var message = MyAPIGateway.Utilities.SerializeFromBinary<TeleportResponseMessage>(data);
            TeleportCore.ClientApplyTeleportResponse(message);
        }

        private static void HandleJumpRequest(byte[] data)
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            var message = MyAPIGateway.Utilities.SerializeFromBinary<JumpRequestMessage>(data);
            TeleportGateway.ProcessJumpRequest(message.GatewayId, message.Link);
        }

        private static void HandleJumpInitiated(byte[] data)
        {
            var message = MyAPIGateway.Utilities.SerializeFromBinary<JumpInitiatedMessage>(data);
            var gateway = MyAPIGateway.Entities.GetEntityById(message.GatewayId) as IMyCollector;
            if (gateway != null)
            {
                var gatewayLogic = gateway.GameLogic.GetAs<TeleportGateway>();
                if (gatewayLogic != null)
                {
                    MyLogger.Log($"TPGate: HandleJumpInitiated: Received jump initiation. Distance: {message.JumpDistance / 1000:F1}km");

                    // Set values for countdown
                    gatewayLogic._jumpDistance = message.JumpDistance;
                    gatewayLogic._teleportCountdown = message.CountdownTicks;
                    gatewayLogic._isTeleporting = true;
                    gatewayLogic._showSphereDuringCountdown = true;

                    // Ensure power is deducted
                    gatewayLogic.Settings.StoredPower = Math.Max(0, gatewayLogic.Settings.StoredPower - message.PowerRequired);
                    gatewayLogic.Settings.Changed = true;

                    float totalSeconds = message.CountdownTicks / 60f;

                    // Send initial notification
                    TeleportGateway.NotifyPlayersInRange(
                        $"Initiating {message.JumpDistance / 1000:F1}km jump - {totalSeconds:F1} seconds",
                        gateway.GetPosition(),
                        100,
                        "White"
                    );

                    // Send first countdown notification immediately
                        TeleportGateway.NotifyPlayersInRange(
                        $"Jump in {totalSeconds:F1}s... Distance: {message.JumpDistance / 1000:F1}km",
                        gateway.GetPosition(),
                        100,
                        "White"
                    );

                    gatewayLogic.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                    MyLogger.Log($"TPGate: HandleJumpInitiated: Started countdown. Time: {totalSeconds}s");
                }
            }
        }
    }
}
