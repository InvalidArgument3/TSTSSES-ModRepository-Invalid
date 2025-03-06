using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Math0424.AnimationCore
{
    class CCDIKSolver : IKSolver
    {

        private IKLimb limb;
        public CCDIKSolver(IKLimb limb)
        {
            this.limb = limb;
        }

        public bool Solve(Vector3 dest, int iterations)
        {
            DrawDebugSphere(dest, .1f, 0, 0, 255);
            AllignBones();

            for (int i = 0; i < iterations; i++)
            {
                IKBone head = limb.Bones[limb.Bones.Count - 1];
                for (int b = limb.Bones.Count - 1; b > 0; b--)
                {
                    IKBone child = limb.Bones[b];

                    Vector3 effectorDir = Vector3.Normalize(head.Position - child.Position);
                    Vector3 targetDir = Vector3.Normalize(dest - child.Position);
                    float dot = Vector3.Dot(effectorDir, targetDir);

                    if (dot < 1.0f - 0.0001f) //stop it from spazzing out
                    {
                        Quaternion q = Quaternion.CreateFromTwoVectors(effectorDir, targetDir) * child.Rotation;

                        child.Rotation = Quaternion.Normalize(q);
                        AllignBones();

                        if (!Vector3.IsZero(child.Axis)) //if its a joint
                        {
                            q = Quaternion.CreateFromTwoVectors(child.Rotation * child.Axis, limb.Bones[b - 1].Rotation * child.Axis) * child.Rotation;

                            child.Rotation = Quaternion.Normalize(q);
                            AllignBones();

                            if (child.MaxAngle != 0) //angle constraints
                            {
                                //To: future Math From: past Math
                                //you gotta be the one to finish this
                                DrawDebugLine(child.Position, q.Forward, 255, 255, 255);

                            }

                            //https://github.com/DoYouEven/IceAge-Unity/blob/master/Assets/RootMotion/FinalIK/Rotation%20Limits/RotationLimitHinge.cs
                        }
                    }

                    DrawDebugLine(child.Position, child.Axis, 255 / (i + 1), 0, 255);

                    DrawDebugLine(child.Position, child.LocalBone, 0, 255 / (i + 1), 255);
                    DrawDebugSphere(child.Position, .1f, 0, 255 / (i + 1), 255);
                }
            }
            return true;
        }

        private void AllignBones()
        {
            Vector3 pos = limb.Bones[0].Position;
            for(int i = 1; i < limb.Bones.Count; i++)
            {
                IKBone start = limb.Bones[i];
                
                start.Position = pos;

                pos += start.LocalBone;
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

        /// <summary>
        /// Quaternion to euler
        /// </summary>
        /// <param name="q"></param>
        /// <returns>Vector3; x = roll; y = pitch; z = yaw;</returns>
        Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles;

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = (float)((Math.PI / 2) * (Math.Sin(sinp) < 0 ? -1 : 1)); // use 90 degrees if out of range
            else
                angles.Y = (float)Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        //https://github.com/zi-su/CCDIK/blob/master/Assets/CCDSolver.cs
        //https://zalo.github.io/blog/inverse-kinematics
        //https://github.com/zalo/MathUtilities/blob/master/Assets/IK/CCDIK/CCDIKJoint.cs
    }
}
