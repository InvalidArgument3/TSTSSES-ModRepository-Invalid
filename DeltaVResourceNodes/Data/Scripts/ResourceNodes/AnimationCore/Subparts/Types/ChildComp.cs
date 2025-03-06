using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Math0424.AnimationCore
{
    class ChildComp : SubpartComponent
    {

        private MyHierarchyComponentBase originalParent;

        public override void Init()
        {
            originalParent = Subpart.MyPart.Hierarchy.Parent;
        }

        public void AddChild(MySubpart child)
        {
            Subpart.MyPart.Hierarchy.AddChild(child.MyPart, true);
        }

        public void RemoveChild(MySubpart child)
        {
            Subpart.MyPart.Hierarchy.RemoveChild(child.MyPart, true);
            child.GetComponent<ChildComp>()?.originalParent?.AddChild(child.MyPart, true);
        }

    }

}
