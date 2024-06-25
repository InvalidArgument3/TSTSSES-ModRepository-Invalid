using DynamicAsteroids.AsteroidEntities;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

[ProtoContract]
public class AsteroidState
{
    [ProtoMember(1)]
    public Vector3D Position { get; set; }

    [ProtoMember(2)]
    public float Size { get; set; }

    [ProtoMember(3)]
    public AsteroidType Type { get; set; }

    [ProtoMember(4)]
    public long EntityId { get; set; } // Unique ID for each asteroid
}