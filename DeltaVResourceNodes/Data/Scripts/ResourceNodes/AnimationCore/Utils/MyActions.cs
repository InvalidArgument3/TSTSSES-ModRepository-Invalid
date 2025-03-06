using System;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{

    abstract class BaseAction
    {
        public virtual bool IsFinished { get { return Delay <= -1; } }
        public int Delay { get; protected set; }

        protected MySubpart sub;

        public BaseAction(MySubpart subpart, int delay)
        {
            this.sub = subpart;
            this.Delay = delay;
        }

        public virtual void Update()
        {
            if (Delay <= 0)
            {
                Tick();
            }
            Delay--;
        }

        public abstract void Tick();
    }

    abstract class TimedAction : BaseAction
    {
        public override bool IsFinished { get { return frame >= endFrame; } }

        protected int frame, endFrame;
        protected LerpType lerp;
        protected EaseType ease;
        protected double val;

        public TimedAction(MySubpart subpart, int time, int delay, LerpType lerp, EaseType ease) : base(subpart, delay)
        {
            this.frame = -1;
            this.endFrame = time;
            this.lerp = lerp;
            this.ease = ease;
        }

        public override void Update()
        {
            if (Delay <= 0)
            {
                frame++;
                val = frame != 0 ? ((double)frame) / endFrame : 0;
                Tick();
            }
            Delay--;
        }
    }

    class MoveAction : TimedAction
    {
        private Vector3 end, prev;
        public MoveAction(MySubpart part, int time, Vector3D end, LerpType lerp, EaseType ease, int delay = 0) : base(part, time, delay, lerp, ease)
        {
            this.prev = Vector3.Zero;
            this.end = end;
        }
        public override void Tick()
        {
            Matrix matrix = sub.MyPart.PositionComp.LocalMatrixRef;

            Vector3 vec = lerp.Lerp(ease, Vector3.Zero, end, val);
            Vector3 temp = vec;

            vec -= prev;
            prev = temp;

            Matrix transformed = sub.MyPart.PositionComp.LocalMatrixRef;
            transformed.Translation = Vector3.Zero;

            matrix.Translation += Vector3.Transform(vec, transformed);

            sub.MyPart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }

    class VibrateAction : TimedAction
    {
        private static readonly Random rand = new Random();

        private Vector3D prev;
        private float scale;
        public VibrateAction(MySubpart part, int time, float scale, int delay = 0) : base(part, time, delay, LerpType.Instant, EaseType.InOut)
        {
            this.scale = scale;
            this.prev = Vector3D.Zero;
        }
        public override void Tick()
        {
            Matrix matrix = sub.MyPart.PositionComp.LocalMatrixRef;
            if (prev.LengthSquared() != 0)
            {
                matrix.Translation -= prev;
                prev = Vector3D.Zero;
            } 
            else
            {
                prev = new Vector3D(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()) * scale;
                matrix.Translation += prev;
            }

            sub.MyPart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }

    class RotateAction : TimedAction
    {
        private Quaternion start, end, prev;
        public RotateAction(MySubpart part, int time, Quaternion end, LerpType lerp, EaseType ease, int delay = 0) : base(part, time, delay, lerp, ease)
        {
            this.start = Quaternion.Identity;
            this.prev = start;
            this.end = end;
        }
        public override void Tick()
        {
            Quaternion quat = lerp.Lerp(ease, start, end, val);
            Quaternion temp = quat;

            quat /= prev;
            prev = temp;
            
            Matrix local = sub.MyPart.PositionComp.LocalMatrixRef;
            Matrix matrix = Matrix.Transform(local, quat);
            matrix.Translation = local.Translation;

            sub.MyPart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }

    class RotationAction : TimedAction
    {
        private Quaternion rot;
        public RotationAction(MySubpart part, int time, Quaternion rot, int delay = 0) : base(part, time, delay, LerpType.Linear, EaseType.InOut)
        {
            this.rot = rot;
        }
        public override void Tick()
        {
            Matrix local = sub.MyPart.PositionComp.LocalMatrixRef;
            Matrix matrix = Matrix.Transform(local, rot);
            matrix.Translation = local.Translation;

            sub.MyPart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }

    class RotationAroundOrigin : TimedAction
    {
        private Quaternion rot;
        public RotationAroundOrigin(MySubpart part, int time, Quaternion rot, int delay = 0) : base(part, time, delay, LerpType.Linear, EaseType.InOut)
        {
            this.rot = rot;
        }
        public override void Tick()
        {
            Matrix current = sub.MyPart.PositionComp.LocalMatrixRef;
            Matrix rotation = Matrix.Transform(current, rot);

            Vector3 originalPos = sub.Pos.GetOrginMatrix().Translation;
            Vector3 currentPos = current.Translation;

            Vector3 translation = Vector3.Transform(currentPos - originalPos, rot);

            rotation.Translation = translation + originalPos;

            sub.MyPart.PositionComp.SetLocalMatrix(ref rotation);
        }
    }

    class MatrixAction : TimedAction
    {
        private Vector3 Vstart, Vend, Vprev;
        private Quaternion Qstart, Qend, Qprev;
        public MatrixAction(MySubpart part, int time, Matrix end, LerpType lerp, EaseType ease, int delay = 0) : base(part, time, delay, lerp, ease)
        {
            Qstart = Quaternion.CreateFromRotationMatrix(part.MyPart.PositionComp.LocalMatrixRef);
            Qend = Quaternion.CreateFromRotationMatrix(end);
            Qprev = Qstart;

            Vstart = part.MyPart.PositionComp.LocalMatrixRef.Translation;
            Vend = end.Translation;
            Vprev = Vstart;
        }
        public override void Tick()
        {
            Quaternion quat = lerp.Lerp(ease, Qstart, Qend, val);
            Quaternion temp = quat;

            quat /= Qprev;
            Qprev = temp;

            Matrix local = sub.MyPart.PositionComp.LocalMatrixRef;
            Matrix matrix = Matrix.Transform(local, quat);

            matrix.Translation = lerp.Lerp(ease, Vstart, Vend, val);

            sub.MyPart.PositionComp.SetLocalMatrix(ref matrix);
        }
    }

    class CustomAction : BaseAction
    {
        private Action act;
        public CustomAction(MySubpart part, Action act, int delay = 0) : base(part, delay)
        {
            this.act = act;
        }
        public override void Tick()
        {
            act?.Invoke();
        }
    }


}
