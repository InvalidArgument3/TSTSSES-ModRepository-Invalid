using ProtoBuf;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRageMath;
using static Math0424.Networking.EasyNetworker;

namespace Math0424.Networking
{

    [ProtoContract]
    class EmissiveStateChange : IPacket
    {
        [ProtoMember(1)] public long blockId;
        [ProtoMember(2)] public Dictionary<string, uint> colors = new Dictionary<string, uint>();

        public int GetId()
        {
            return 1;
        }

        public void Execute()
        {
            var e = MyEntities.GetEntityById(blockId);
            if (e != null && e is MyCubeBlock)
            {
                var b = e as MyCubeBlock;
                if (!b.MarkedForClose)
                {
                    foreach (var m in colors)
                    {
                        b.SetEmissiveParts(m.Key, new Color(m.Value), 1);
                    }
                }
            }
        }

    }
}
