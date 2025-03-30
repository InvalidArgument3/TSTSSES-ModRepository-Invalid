using ObjectBuilders.SafeZone;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using Sandbox.Game.EntityComponents;

namespace SiegableSafeZones
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SafeZoneBlock), false)]
    public class SafeZoneGameLogic : MyGameLogicComponent
    {

        public bool isServer;
        public bool isDedicated;
        public IMySafeZoneBlock zoneBlock;
        //private MyResourceSinkComponent sink;
        //private MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            isServer = MyAPIGateway.Session.IsServer;
            isDedicated = MyAPIGateway.Utilities.IsDedicated;
            zoneBlock = Entity as IMySafeZoneBlock;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            zoneBlock.CubeGrid.GetBlocks(blocks);
            if (zoneBlock.CubeGrid?.Physics == null && blocks.Count > 1) return;

            //sink = zoneBlock.ResourceSink as MyResourceSinkComponent;
            //sink.SetRequiredInputByType(ElectricityId, 0);

            Session.Instance.InitControls(zoneBlock);
            Session.Instance.LoadSafeZoneSettings(zoneBlock, isServer);
        }

        public override void OnRemovedFromScene()
        {
            if (Entity == null) return;
            if (zoneBlock == null) return;
            if (zoneBlock?.CubeGrid?.Physics == null) return;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            zoneBlock.CubeGrid.GetBlocks(blocks);
            if (zoneBlock.CubeGrid?.Physics == null && blocks.Count > 1) return;

            zoneBlock.AppendingCustomInfo -= Session.Instance.UpdateCustomInfo;

            if (isServer)
            {

                Session.Instance.zoneBlockSettingsCache.Remove(zoneBlock.EntityId);
                Comms.SendRemoveBlockFromCache(zoneBlock.EntityId);

            }
            else
            {
                ZoneBlockSettings settings;
                if (!Session.Instance.zoneBlockSettingsCache.TryGetValue(zoneBlock.EntityId, out settings)) return;

                settings.Block = null;
            }
        }
    }
}