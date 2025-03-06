using ProtoBuf;
using ResourceNodes;
using Sandbox.Game.Entities;
using static Math0424.Networking.EasyNetworker;

namespace Math0424.Networking
{

    [ProtoContract]
    class DrillStateUpdate : IPacket
    {

        [ProtoMember(1)] public long blockId;
        [ProtoMember(2)] public bool isInGround;
        [ProtoMember(3)] public bool isProducing;
        [ProtoMember(4)] public bool invFull;
        [ProtoMember(5)] public string oreName;
        [ProtoMember(6)] public string Count;
        [ProtoMember(7)] public string Effectiveness;
        [ProtoMember(8)] public string Productivity;
        [ProtoMember(9)] public string MinedOreRatio;
        [ProtoMember(10)] public string PowerEfficiency;


        public int GetId()
        {
            return 2;
        }

        public void Execute()
        {
            var e = MyEntities.GetEntityById(blockId);
            if (e != null && e is MyCubeBlock)
            {
                var b = e as MyCubeBlock;
                if (!b.MarkedForClose)
                {
                    AdvancedStaticDrill d = b.GameLogic.GetAs<AdvancedStaticDrill>();
                    MediumStaticDrill d1 = b.GameLogic.GetAs<MediumStaticDrill>();
                    StoneStaticDrill d3 = b.GameLogic.GetAs<StoneStaticDrill>();
                    BasicStaticDrill d2 = b.GameLogic.GetAs<BasicStaticDrill>();
                    if (d != null)
                    {
                        d.IsProducing = isProducing;
                        d.state = this;
                    }
                    else if (d1 != null)
                    {
                        d1.IsProducing = isProducing;
                        d1.state = this;
                    }
                    else if (d2 != null)
                    {
                        d2.IsProducing = isProducing;
                        d2.state = this;
                    }
                    else if (d3 != null)
                    {
                        d3.IsProducing = isProducing;
                        d3.state = this;
                    }
                }
            }
        }

    }
}
