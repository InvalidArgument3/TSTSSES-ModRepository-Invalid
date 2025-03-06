using VRageMath;

namespace Math0424.AnimationCore
{
    class FABRIKSolver : IKSolver
    {
        private IKLimb limb;

        public FABRIKSolver(IKLimb limb)
        {
            this.limb = limb;
        }

        public bool Solve(Vector3 dest, int iterations)
        {
            limb.Bones[0].Position = limb.Position;

            for (int i = 0; i < iterations; i++)
            {
                Vector3 currentGoal = dest + limb.Position;

                for (int b = limb.Bones.Count - 1; b > 0; b--)
                {
                    IKBone curr = limb.Bones[b];
                    IKBone prev = limb.Bones[b - 1];

                    Vector3 newBone = currentGoal - (prev.Position + prev.LocalBone);

                    curr.SetOrientation(newBone);
                    currentGoal -= curr.LocalBone;
                    curr.Position = currentGoal;
                }

                currentGoal = limb.Position;
                for (int b = 0; b < limb.Bones.Count - 1; b++)
                {
                    IKBone curr = limb.Bones[b];
                    IKBone prev = limb.Bones[b + 1];

                    Vector3 newBone = currentGoal - prev.Position;

                    curr.Position = currentGoal;
                    curr.SetOrientation(newBone);
                    currentGoal -= curr.LocalBone;
                }

            }
            return true;
        }

    }
}
