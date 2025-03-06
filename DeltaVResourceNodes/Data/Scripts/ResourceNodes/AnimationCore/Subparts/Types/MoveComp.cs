using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{
    class MoveComp : SubpartComponent
    {

        private Matrix originMatrix;

        public override void Init()
        {
            SetOriginMatrix();
        }

        public void SetOriginMatrix()
        {
            originMatrix = new Matrix(Subpart.MyPart.PositionComp.LocalMatrixRef);
        }

        public void SetOriginMatrix(Matrix orgin)
        {
            originMatrix = new Matrix(orgin);
        }

        public Matrix GetOrginMatrix()
        {
            return new Matrix(originMatrix);
        }

        public void ResetPos(int delay = 0)
        {
            Subpart.AddAction(new CustomAction(Subpart, () => {
                Matrix x = Subpart.MyPart.PositionComp.LocalMatrixRef;
                x.Translation = originMatrix.Translation;
                Subpart.MyPart.PositionComp.SetLocalMatrix(ref x);
            }, delay));
        }

        public void ResetPosRot(int delay = 0)
        {
            Subpart.AddAction(new CustomAction(Subpart, () => {
                Subpart.MyPart.PositionComp.SetLocalMatrix(ref originMatrix);
            }, delay));
        }

        public void Translate(int frames, Vector3 position, LerpType lerp, EaseType ease, int delay = 0)
        {
            Subpart.AddAction(new MoveAction(Subpart, frames, position, lerp, ease, delay));
        }

        public void TranslateToOrginMatix(int frames, LerpType lerp, EaseType ease, int delay = 0)
        {
            Subpart.AddAction(new MatrixAction(Subpart, frames, originMatrix, lerp, ease, delay));
        }

        public void Rotate(int frames, Vector3D axis, float angle, LerpType lerp, EaseType ease, int delay = 0)
        {
            Quaternion q = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(angle));
            Subpart.AddAction(new RotateAction(Subpart, frames, q, lerp, ease, delay));
        }

        public void ConstantRotationAboutOrigin(int frames, Vector3D axis, float degreesPerFrame, int delay = 0)
        {
            Quaternion q = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(degreesPerFrame));
            Subpart.AddAction(new RotationAroundOrigin(Subpart, frames, q, delay));
        }

        public void ConstantRotation(int frames, Vector3D axis, float degreesPerFrame, int delay = 0)
        {
            Quaternion q = Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(degreesPerFrame));
            Subpart.AddAction(new RotationAction(Subpart, frames, q, delay));
        }

        public void Vibrate(int frames, float scale, int delay = 0)
        {
            Subpart.AddAction(new VibrateAction(Subpart, frames, scale, delay));
        }

        public void TranslateMatrix(int frames, Matrix dest, LerpType lerp, EaseType ease, int delay = 0)
        {
            Subpart.AddAction(new MatrixAction(Subpart, frames, dest, lerp, ease, delay));
        }

    }
}
