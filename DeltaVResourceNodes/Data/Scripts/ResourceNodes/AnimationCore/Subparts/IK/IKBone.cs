using VRageMath;

namespace Math0424.AnimationCore
{

    class IKBone
    {

        public float Length;
        public Vector3 Position;

        //attraction to local arbitrary location
        public Vector3 Weight;

        //Joints
        public Vector3 Axis; //Must be normalized
        public float MaxAngle, MaxRotation;
        public Quaternion Rotation { get { return _rotation; } set { _rotation = Quaternion.Normalize(value); } }

        private Quaternion _rotation;
        private Quaternion StartRotation;

        public void ResetPosition()
        {
            Rotation = StartRotation;
        }
        public void SetStartPosition(Quaternion rotation)
        {
            StartRotation = rotation;
            ResetPosition();
        }

        public Vector3 WorldBone(Matrix m)
        {
            Quaternion q1 = Quaternion.CreateFromRotationMatrix(m);
            q1 *= _rotation;
            return (q1 * Vector3.Forward * Length) + m.Translation;
        }

        public void SetOrientation(Vector3 vec)
        {
            if (Vector3.IsZero(vec))
            {
                _rotation = Quaternion.Identity;
                return;
            }
            if (!Vector3.IsZero(Axis))
            {
                _rotation = Quaternion.Normalize(Quaternion.CreateFromForwardUp(Vector3.Normalize(vec), Axis));
            }
            else
            {
                _rotation = Quaternion.Normalize(Quaternion.CreateFromForwardUp(Vector3.Normalize(vec), Vector3.Up));
            }
        }

        public Vector3 LocalBone
        {
            get
            {
                if (Length == 0)
                    return Vector3.Zero;
                return _rotation * Vector3.Forward * Length;
            }
            set
            {
                if (Vector3.IsZero(value))
                {
                    _rotation = Quaternion.Identity;
                    Length = 0;
                    return;
                }
                Length = value.Length();
                if (!Vector3.IsZero(Axis))
                {
                    _rotation = Quaternion.Normalize(Quaternion.CreateFromForwardUp(Vector3.Normalize(value), Axis));
                }
                else 
                {
                    _rotation = Quaternion.Normalize(Quaternion.CreateFromForwardUp(Vector3.Normalize(value), Vector3.Up));
                }
            }
        }

        public IKBone(float boneLength, Vector3 Axis)
        {
            this.MaxRotation = 360;
            this.MaxAngle = 300;
            this.Weight = Vector3.Zero;
            this.Axis = Vector3.Normalize(Axis);
            this.LocalBone = Vector3.Forward * boneLength;
        }

    }
}
