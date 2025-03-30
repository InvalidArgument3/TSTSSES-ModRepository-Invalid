using ObjectBuilders.SafeZone;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using System;

namespace SiegableSafeZones
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_JumpDrive), false)]
    public class JumpDriveLogic : MyGameLogicComponent
    {
        public bool isServer;
        public bool isDedicated;
        public IMyJumpDrive drive;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            isServer = MyAPIGateway.Session.IsServer;
            isDedicated = MyAPIGateway.Utilities.IsDedicated;
            drive = Entity as IMyJumpDrive;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            drive.CubeGrid.GetBlocks(blocks);
            if (drive.CubeGrid?.Physics == null && blocks.Count > 1) return;

            Session.Instance.InitControls(drive);
        }

        public override void OnRemovedFromScene()
        {
            if (Entity == null) return;
            if (drive == null) return;
            if (drive?.CubeGrid?.Physics == null) return;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            drive.CubeGrid.GetBlocks(blocks);
            if (drive.CubeGrid?.Physics == null && blocks.Count > 1) return;

            drive.AppendingCustomInfo -= Session.Instance.UpdateCustomInfo;
        }
    }
}
