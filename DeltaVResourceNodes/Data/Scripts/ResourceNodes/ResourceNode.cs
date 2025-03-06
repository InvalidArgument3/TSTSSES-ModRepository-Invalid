using Math0424.Networking;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using static Math0424.Networking.EasyNetworker;

namespace ResourceNodes
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class ResourceNode : MySessionComponentBase
    {

        public List<string> MiningBlacklist = new List<string>();
        public Dictionary<string, HashSet<long>> Miners = new Dictionary<string, HashSet<long>>();
        public Dictionary<long, Vector3D> Locations = new Dictionary<long, Vector3D>();

        public EasyNetworker Network;
        public string ModPath;

        public static ResourceNode Instance { get; private set; }
        
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;

            ModPath = ModContext.ModPath;
            Network = new EasyNetworker(51821);
            Network.Register();

            Network.OnRecievedPacket += PacketIn;
        }

        private void PacketIn(PacketIn raw)
        {
            if (raw.IsFromServer && raw.PacketId == 1)
            {
                raw.UnWrap<EmissiveStateChange>()?.Execute();
            } 
            else if (raw.IsFromServer && raw.PacketId == 2)
            {
                raw.UnWrap<DrillStateUpdate>()?.Execute();
            }
        }

        protected override void UnloadData()
        {
            Network?.UnRegister();
        }

    }
}
