using VRageMath;

namespace Math0424.AnimationCore
{
    class IKComp : SubpartComponent
    {

        public IKBone Bone;
        private Quaternion Addition;
        private Vector3 Offset;

        public IKBone Create(Vector3 Bone, Vector3 offset, Vector3? axis = null)
        {
            Offset = offset;
            this.Bone = new IKBone(Bone.Length(), axis ?? Subpart.MyPart.PositionComp.LocalMatrixRef.Up);

            var normBone = Vector3.Normalize(Bone);
            Addition = Quaternion.Normalize(Quaternion.CreateFromForwardUp(normBone, Vector3.CalculatePerpendicularVector(normBone)));

            this.Bone.SetStartPosition(Addition);
            return this.Bone;
        }

        public override void Update()
        {
            Matrix m = Matrix.CreateFromQuaternion(Quaternion.Normalize(Bone.Rotation * Addition));
            m.Translation = Bone.Position + Vector3.Transform(Offset, m);

            Subpart.MyPart.PositionComp.SetLocalMatrix(ref m);
        }

    }
}
