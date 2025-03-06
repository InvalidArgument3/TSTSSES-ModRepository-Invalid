using ResourceNodes;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.IO;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Math0424.AnimationCore
{
    abstract class MyAbstractAnimatedBlock : MyGameLogicComponent//MyCompositeGameLogicComponent
    {

        public MyCubeBlock Block { get; private set; }
        protected int CurrentFrame = -1;

        private Dictionary<string, MySubpart> SubParts;
        private bool subpartsRegistered = false;
        private bool subpartsInit = false;
        private bool wasBuilt = false;

        public void LoadOntoBlock()
        {
            //block.GameLogic.Container.Add(this);
            //block.Components.Add(this);

            SubParts = new Dictionary<string, MySubpart>();
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Block = (MyCubeBlock)Entity;
        }

        public override void UpdateOnceBeforeFrame()
        {
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            BeforeFirstUpdate();
        }

        public void ReloadSubparts()
        {
            if (Block != null && !Block.MarkedForClose && !Block.Closed && Block.InScene)
            {
                SubParts.Clear();
                subpartsRegistered = false;
                subpartsInit = false;
                CurrentFrame = -1;
            }
        }

        protected MySubpart LoadSubpart(string name, MySubpart parent = null)
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return null;

            MyEntity ent = (parent != null ? parent.MyPart : (MyEntity)Block);
            MyEntitySubpart basePart;
            if (ent.TryGetSubpart(name, out basePart))
            {
                MySubpart p;
                if (SubParts.TryGetValue(name, out p))
                {
                    return p;
                }
                MySubpart part = new MySubpart(basePart);
                SubParts.Add(name, part);
                return part;
            }
            return null;
        }

        public MySubpart LoadSubpartFromFile(string loadName, string fileName)
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return null;

            MySubpart returned;
            MyEntitySubpart entSubpart;
            if (Block.Subparts.TryGetValue(loadName, out entSubpart))
            {
                returned = new MySubpart(entSubpart);
                SubParts.Add(loadName, returned);
                return returned;
            }

            entSubpart = new MyEntitySubpart();

            string model = Path.Combine(ResourceNode.Instance.ModPath, fileName) + ".mwm";
            entSubpart.Render.EnableColorMaskHsv = Block.Render.EnableColorMaskHsv;
            entSubpart.Render.ColorMaskHsv = Block.Render.ColorMaskHsv;
            entSubpart.Render.TextureChanges = Block.Render.TextureChanges;
            entSubpart.Render.MetalnessColorable = Block.Render.MetalnessColorable;

            entSubpart.Init(null, model, Block, null, null);
            Block.Subparts[fileName] = entSubpart;
            if (Block.InScene)
            {
                entSubpart.OnAddedToScene(Block);
            }

            returned = new MySubpart(entSubpart);
            SubParts.Add(loadName, returned);
            return returned;
        }

        public MySubpart GetSubpart(string name)
        {
            return SubParts[name];
        }

        public abstract bool LoadSubparts();
        public virtual void BeforeFirstUpdate() { }
        public virtual void GameUpdate() { }
        public virtual void BeforeFirstAnimationUpdate() { }
        public virtual void AnimationUpdate() { }
        public virtual void ClosedBlock() { }


        int subpartAttempts = 0;
        public override void UpdateBeforeSimulation()
        {
            if (Block == null || Block.Closed || Block.MarkedForClose)
                return;

            GameUpdate();

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (wasBuilt != Block.IsBuilt)
            {
                ReloadSubparts();
                wasBuilt = false;
            }

            if (!Block.IsBuilt || !Block.InScene)
                return;

            wasBuilt = true;
            if (!subpartsRegistered)
            {
                subpartAttempts++;
                subpartsRegistered = LoadSubparts();
                if (subpartAttempts == 100)
                {
                    MyAPIGateway.Utilities.ShowMessage("AnimationCore", "Tried to load subparts 100 times, all failed.");
                }
                return;
            }

            if (!subpartsInit)
            {
                subpartsInit = true;
                foreach (MySubpart p in SubParts.Values)
                {
                    p.InitComponents();
                }
                BeforeFirstAnimationUpdate();
            }

            if (Vector3D.DistanceSquared(MyAPIGateway.Session.Camera.Position, Block.PositionComp.GetPosition()) < 2000000) //2KM animation range
            {
                CurrentFrame++;
                foreach (MySubpart p in SubParts.Values)
                {
                    p.Update();
                }
                AnimationUpdate();
            }
        }
        
        public override void Close()
        {
            foreach (MySubpart p in SubParts?.Values)
            {
                p?.Delete();
            }
            SubParts?.Clear();

            SubParts = null;
            Block = null;
            
            base.Close();
            ClosedBlock();
        }

    }
}
