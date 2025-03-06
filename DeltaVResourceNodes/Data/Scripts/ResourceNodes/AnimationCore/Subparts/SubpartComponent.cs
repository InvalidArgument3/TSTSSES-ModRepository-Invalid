using VRage.Game.Entity;

namespace Math0424.AnimationCore
{
    abstract class SubpartComponent
    {

        protected MySubpart Subpart;

        public void Initalize(MySubpart Parent)
        {
            this.Subpart = Parent;
        }

        public virtual void Init() { }
        public virtual void Update() { }

        public virtual void Close() { }


    }
}
