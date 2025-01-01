using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

// ReSharper disable UnusedType.Global
namespace TSTSSES.DoorDestroyer
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door), false)]
    internal class DoorDestroyer : GenericDoorDestroyer<IMyDoor> { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightSlideDoor), false)]
    internal class SlideGenericDoorDestroyer : GenericDoorDestroyer<IMyAirtightSlideDoor> { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AirtightHangarDoor), false)]
    internal class HangarGenericDoorDestroyer : GenericDoorDestroyer<IMyAirtightHangarDoor> { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_AdvancedDoor), false)]
    internal class AdvancedGenericDoorDestroyer : GenericDoorDestroyer<IMyAdvancedDoor> { }
    
    internal class GenericDoorDestroyer<T> : MyGameLogicComponent where T : IMyCubeBlock
    {
        private T _door;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            _door = (T) Entity;
            if(_door?.CubeGrid?.Physics == null || !(_door is MyEntity))
                return;

            foreach (var part in (_door as MyEntity).Subparts.Values)
                if (part.Physics != null)
                    part.Physics.Enabled = false;
        }
    }
}
