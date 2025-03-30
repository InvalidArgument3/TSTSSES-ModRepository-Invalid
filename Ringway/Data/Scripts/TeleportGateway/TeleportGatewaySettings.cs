using System;
using ProtoBuf;

namespace TeleportMechanisms
{
    [ProtoContract]
    public class TeleportGatewaySettings
    {
        [ProtoMember(1)]
        public string GatewayName { get; set; } = "";

        [ProtoMember(2)]
        public bool AllowPlayers { get; set; } = true;

        [ProtoMember(3)]
        public bool AllowShips { get; set; } = true;

        [ProtoMember(4)]
        public bool ShowSphere { get; set; } = false;

        [ProtoMember(5)]
        public float SphereDiameter { get; set; } = 50.0f;

        [ProtoMember(6)]
        public float StoredPower { get; set; } = 0f;
        [ProtoMember(7)]
        public float MaxStoredPower { get; set; } = 1000f;

        [ProtoIgnore]
        public bool Changed { get; set; } = false;

        [ProtoIgnore]
        public TimeSpan LastSaved { get; set; }

        public TeleportGatewaySettings()
        {
            Changed = true;
            LastSaved = TimeSpan.Zero;
        }
    }
}
