using Sandbox.ModAPI;
using VRageMath;

namespace Math0424.AnimationCore
{
    class GravityComp : SubpartComponent
    {

        private IKComp IK;
        private PhysicsComp Physics;
        public float Multiplier = 1;

        public override void Init()
        {
            if (Subpart.HasComponent<PhysicsComp>())
            {
                Physics = Subpart.GetComponent<PhysicsComp>();
            }
            if (Subpart.HasComponent<IKComp>())
            {
                IK = Subpart.GetComponent<IKComp>();
            }
        }

        public override void Update()
        {
            if (Physics != null)
            {
                float what;
                Vector3 grav = MyAPIGateway.Physics.CalculateNaturalGravityAt(Subpart.MyPart.Parent.PositionComp.GetPosition(), out what);
                Subpart.MyPart.Physics.Gravity = grav * Multiplier;
            }
            if (IK != null)
            {
                float what;
                Vector3 grav = MyAPIGateway.Physics.CalculateNaturalGravityAt(Subpart.MyPart.Parent.PositionComp.GetPosition(), out what);
                IK.Bone.Weight = grav * Multiplier;
            }
        }

    }
}
