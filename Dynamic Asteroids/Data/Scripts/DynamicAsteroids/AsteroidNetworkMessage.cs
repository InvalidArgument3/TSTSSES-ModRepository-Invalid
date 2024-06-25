using DynamicAsteroids.AsteroidEntities;
using VRageMath;
using ProtoBuf;

namespace DynamicAsteroids
{
    [ProtoContract]
    public class AsteroidNetworkMessageContainer
    {
        [ProtoMember(1)]
        public int Version { get; set; } = 1;

        [ProtoMember(2)]
        public AsteroidNetworkMessage[] Messages { get; set; }

        public AsteroidNetworkMessageContainer() { }

        public AsteroidNetworkMessageContainer(AsteroidNetworkMessage[] messages)
        {
            Messages = messages;
        }
    }

    [ProtoContract]
    public struct AsteroidNetworkMessage
    {
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
        [ProtoMember(12)]
        public bool IsSubChunk;
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

        public AsteroidNetworkMessage(Vector3D position, float size, Vector3D initialVelocity, Vector3D angularVelocity, AsteroidType type, bool isSubChunk, long entityId, bool isRemoval, bool isInitialCreation, Quaternion rotation)
        {
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
            IsSubChunk = isSubChunk;
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
}