using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Math0424.AnimationCore
{
    class IKLimb
    {

        private IKSolver solver;
        private MyAbstractAnimatedBlock Block;

        public Vector3 Position;
        public List<IKBone> Bones;

        public IKLimb(MyAbstractAnimatedBlock Block, IKSolverEnum solverEnum)
        {
            this.Block = Block;

            if (solverEnum == IKSolverEnum.FABRIK)
            {
                solver = new FABRIKSolver(this);
            } 
            else
            {
                solver = new CCDIKSolver(this);
            }

            Position = Vector3.Zero;
            Bones = new List<IKBone>();

            Bones.Add(new IKBone(0, Vector3.Zero)); //back
            Bones.Add(new IKBone(0, Vector3.Zero)); //front
        }

        public void AddBone(IKBone bone)
        {
            Bones.Insert(Bones.Count-1, bone);
        }

        public void MoveTo(Vector3 goal, int iterations = 1)
        {
            Vector3 totalPosition = Position;
            foreach (var b in Bones)
            {
                b.ResetPosition();

                totalPosition -= b.LocalBone;
                b.Position = totalPosition;
            }
            solver.Solve(Vector3.Transform(goal, Block.Block.PositionComp.WorldMatrixInvScaled), iterations);
        }

        public void DebugDraw()
        {
            foreach (var b in Bones)
            {
                Matrix m = Block.Block.WorldMatrix;
                Vector3 pos = Vector3.Transform(b.Position, m);
                m.Translation = Vector3.Zero;
                Vector3 bone = Vector3.Transform(b.LocalBone, m);

                DrawDebugLine(pos, -bone, 0, 255, 255);
                DrawDebugSphere(pos, .1f, 0, 255, 0);
            }
        }

        private void DrawDebugLine(Vector3D pos, Vector3D dir, int r, int g, int b)
        {
            Vector4 color = new Vector4(r / 255, g / 255, b / 255, 1);
            MySimpleObjectDraw.DrawLine(pos, pos + dir, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
        }

        private void DrawDebugSphere(Vector3D pos, float size, int r, int g, int b)
        {
            Color color = new Vector4(r / 255, g / 255, b / 255, 1);

            var matrix = MatrixD.CreateWorld(pos);
            MySimpleObjectDraw.DrawTransparentSphere(ref matrix, size, ref color, MySimpleObjectRasterizer.Solid, 30, MyStringId.GetOrCompute("Square"));
        }

    }
}
