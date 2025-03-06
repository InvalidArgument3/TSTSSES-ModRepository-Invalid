using Sandbox.Engine.Physics;
using VRage.Game.Components;

namespace Math0424.AnimationCore
{
    class PhysicsComp : SubpartComponent
    {
        int layer = 15;

        public void Create(int layer)
        {
            this.layer = layer;
        }

        public override void Init()
        {
            //| RigidBodyFlag.RBF_STATIC
            Subpart.MyPart.InitModelPhysics(RigidBodyFlag.RBF_DOUBLED_KINEMATIC | RigidBodyFlag.RBF_UNLOCKED_SPEEDS, layer);
        }

        public override void Close()
        {
            Subpart.MyPart?.Physics?.Close();
        }

    }
}
