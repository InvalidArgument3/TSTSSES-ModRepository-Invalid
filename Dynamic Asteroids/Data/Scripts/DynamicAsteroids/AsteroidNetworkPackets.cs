using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids {
    [ProtoInclude(10, typeof(AsteroidUpdatePacket))]
    [ProtoInclude(11, typeof(AsteroidRemovalPacket))]
    [ProtoInclude(12, typeof(AsteroidSpawnPacket))]
    [ProtoInclude(13, typeof(AsteroidBatchUpdatePacket))]
    [ProtoInclude(14, typeof(ZoneUpdatePacket))]
    public abstract partial class PacketBase {
    }

    [ProtoContract]
    public class AsteroidState {
        [ProtoMember(1)]
        public Vector3D Position { get; set; }

        [ProtoMember(2)]
        public Vector3D Velocity { get; set; }

        [ProtoMember(3)]
        public Quaternion Rotation { get; set; }

        [ProtoMember(4)]
        public float Size { get; set; }

        [ProtoMember(5)]
        public AsteroidType Type { get; set; }

        [ProtoMember(6)]
        public long EntityId { get; set; }

        [ProtoMember(7)]
        public Vector3D AngularVelocity { get; set; }

        public AsteroidState() { }

        public AsteroidState(AsteroidEntity asteroid) {
            Position = asteroid.PositionComp.GetPosition();
            Velocity = asteroid.Physics.LinearVelocity;
            AngularVelocity = asteroid.Physics.AngularVelocity;
            Rotation = Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix);
            Size = asteroid.Properties.Diameter;
            Type = asteroid.Type;
            EntityId = asteroid.EntityId;
        }

        private float GetQuaternionAngleDifference(Quaternion a, Quaternion b) {
            float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
            dot = MathHelper.Clamp(dot, -1f, 1f);
            return 2f * (float)Math.Acos(Math.Abs(dot));
        }

        public bool HasChanged(AsteroidEntity asteroid) {
            const double POSITION_THRESHOLD = 0.01;
            const double VELOCITY_THRESHOLD = 0.01;
            const double ROTATION_THRESHOLD = 0.01;

            return Vector3D.DistanceSquared(Position, asteroid.PositionComp.GetPosition()) > POSITION_THRESHOLD ||
                   Vector3D.DistanceSquared(Velocity, asteroid.Physics.LinearVelocity) > VELOCITY_THRESHOLD ||
                   GetQuaternionAngleDifference(Rotation, Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix)) > ROTATION_THRESHOLD;
        }
    }

    [ProtoContract]
    public class AsteroidUpdatePacket : PacketBase {
        [ProtoMember(1)]
        public List<AsteroidState> States { get; set; } = new List<AsteroidState>();
    }

    [ProtoContract]
    public class AsteroidSpawnPacket : PacketBase {
        [ProtoMember(1)]
        public Vector3D Position { get; set; }

        [ProtoMember(2)]
        public float Size { get; set; }

        [ProtoMember(3)]
        public Vector3D Velocity { get; set; }

        [ProtoMember(4)]
        public Vector3D AngularVelocity { get; set; }

        [ProtoMember(5)]
        public AsteroidType Type { get; set; }

        [ProtoMember(6)]
        public long EntityId { get; set; }

        [ProtoMember(7)]
        public Quaternion Rotation { get; set; }

        public AsteroidSpawnPacket() { }

        public AsteroidSpawnPacket(AsteroidEntity asteroid) {
            Position = asteroid.PositionComp.GetPosition();
            Size = asteroid.Properties.Diameter;
            Velocity = asteroid.Physics.LinearVelocity;
            AngularVelocity = asteroid.Physics.AngularVelocity;
            Type = asteroid.Type;
            EntityId = asteroid.EntityId;
            Rotation = Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix);
        }
    }

    [ProtoContract]
    public class AsteroidRemovalPacket : PacketBase {
        [ProtoMember(1)]
        public long EntityId { get; set; }
    }

    [ProtoContract]
    public class AsteroidBatchUpdatePacket : PacketBase {
        [ProtoMember(1)]
        public List<AsteroidState> Updates { get; set; } = new List<AsteroidState>();

        [ProtoMember(2)]
        public List<long> Removals { get; set; } = new List<long>();

        [ProtoMember(3)]
        public List<AsteroidSpawnPacket> Spawns { get; set; } = new List<AsteroidSpawnPacket>();
    }

    [ProtoContract]
    public class ZoneData {
        [ProtoMember(1)]
        public Vector3D Center { get; set; }

        [ProtoMember(2)]
        public double Radius { get; set; }

        [ProtoMember(3)]
        public long PlayerId { get; set; }

        [ProtoMember(4)]
        public bool IsActive { get; set; }

        [ProtoMember(5)]
        public bool IsMerged { get; set; }

        [ProtoMember(6)]
        public double CurrentSpeed { get; set; }
    }

    [ProtoContract]
    public class ZoneUpdatePacket : PacketBase {
        [ProtoMember(1)]
        public List<ZoneData> Zones { get; set; } = new List<ZoneData>();
    }

    [ProtoContract]
    public class AsteroidPacketData {
        [ProtoMember(1)]
        public Vector3D Position { get; set; }

        [ProtoMember(2)]
        public Vector3D Velocity { get; set; }

        [ProtoMember(3)]
        public Vector3D AngularVelocity { get; set; }

        [ProtoMember(4)]
        public float Size { get; set; }

        [ProtoMember(5)]
        public AsteroidType Type { get; set; }

        [ProtoMember(6)]
        public long EntityId { get; set; }

        [ProtoMember(7)]
        public Quaternion Rotation { get; set; }

        [ProtoMember(8)]
        public bool IsRemoval { get; set; }

        [ProtoMember(9)]
        public bool IsInitialCreation { get; set; }

        public AsteroidPacketData() { } // Required for protobuf

        public AsteroidPacketData(AsteroidEntity asteroid, bool isRemoval = false, bool isInitialCreation = false) {
            Position = asteroid.PositionComp.GetPosition();
            Velocity = asteroid.Physics.LinearVelocity;
            AngularVelocity = asteroid.Physics.AngularVelocity;
            Size = asteroid.Properties.Diameter;
            Type = asteroid.Type;
            EntityId = asteroid.EntityId;
            Rotation = Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix);
            IsRemoval = isRemoval;
            IsInitialCreation = isInitialCreation;
        }
    }

    [ProtoContract]
    public class AsteroidBatchPacket : PacketBase {
        [ProtoMember(1)]
        public List<AsteroidPacketData> Messages { get; set; } = new List<AsteroidPacketData>();

        public AsteroidBatchPacket() { }

        public AsteroidBatchPacket(IEnumerable<AsteroidEntity> asteroids) {
            Messages = new List<AsteroidPacketData>();
            foreach (var asteroid in asteroids) {
                Messages.Add(new AsteroidPacketData(asteroid));
            }
        }
    }

    //TODO: do networking something like this, but what we have...works? gotta move stuff over but then physics started syncing

    public static class NetworkHandler {
        public static void SendAsteroidUpdate(AsteroidEntity asteroid) {
            var packet = new AsteroidBatchPacket();
            packet.Messages.Add(new AsteroidPacketData(asteroid));

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, data);
        }

        public static void SendAsteroidRemoval(long entityId) {
            var packet = new AsteroidRemovalPacket { EntityId = entityId };

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, data);
        }
    }

    [ProtoContract]
    public class AsteroidNetworkMessageContainer {

        [ProtoMember(2)]
        public AsteroidNetworkMessage[] Messages { get; set; }

        public AsteroidNetworkMessageContainer() { }

        public AsteroidNetworkMessageContainer(AsteroidNetworkMessage[] messages) {
            Messages = messages;
        }
    }

    [ProtoContract]
    public class AsteroidNetworkMessage {
        [ProtoMember(1)]
        public double PosX;
        [ProtoMember(2)]
        public double PosY;
        [ProtoMember(3)]
        public double PosZ;
        [ProtoMember(4)]
        public float Size;
        [ProtoMember(5)]
        public double VelX;
        [ProtoMember(6)]
        public double VelY;
        [ProtoMember(7)]
        public double VelZ;
        [ProtoMember(8)]
        public double AngVelX;
        [ProtoMember(9)]
        public double AngVelY;
        [ProtoMember(10)]
        public double AngVelZ;
        [ProtoMember(11)]
        public int Type;
        [ProtoMember(13)]
        public long EntityId;
        [ProtoMember(14)]
        public bool IsRemoval;
        [ProtoMember(15)]
        public bool IsInitialCreation;
        [ProtoMember(16)]
        public float RotX;
        [ProtoMember(17)]
        public float RotY;
        [ProtoMember(18)]
        public float RotZ;
        [ProtoMember(19)]
        public float RotW;

        public AsteroidNetworkMessage() { }

        public AsteroidNetworkMessage(Vector3D position, float size, Vector3D initialVelocity, Vector3D angularVelocity, AsteroidType type, bool isSubChunk, long entityId, bool isRemoval, bool isInitialCreation, Quaternion rotation) {
            PosX = position.X;
            PosY = position.Y;
            PosZ = position.Z;
            Size = size;
            VelX = initialVelocity.X;
            VelY = initialVelocity.Y;
            VelZ = initialVelocity.Z;
            AngVelX = angularVelocity.X;
            AngVelY = angularVelocity.Y;
            AngVelZ = angularVelocity.Z;
            Type = (int)type;
            EntityId = entityId;
            IsRemoval = isRemoval;
            IsInitialCreation = isInitialCreation;
            RotX = rotation.X;
            RotY = rotation.Y;
            RotZ = rotation.Z;
            RotW = rotation.W;
        }

        public Vector3D GetPosition() => new Vector3D(PosX, PosY, PosZ);
        public Vector3D GetVelocity() => new Vector3D(VelX, VelY, VelZ);
        public Vector3D GetAngularVelocity() => new Vector3D(AngVelX, AngVelY, AngVelZ);
        public AsteroidType GetType() => (AsteroidType)Type;
        public Quaternion GetRotation() => new Quaternion(RotX, RotY, RotZ, RotW);
    }

    [ProtoContract]
    public class SettingsSyncMessage : PacketBase {
        [ProtoMember(1)]
        public bool EnableLogging { get; set; }
        // Add other settings that need to be synced
    }
}