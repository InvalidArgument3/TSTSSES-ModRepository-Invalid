using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{
    class PowerLeverComp : InteractableComp
    {
        public Action LeverClose;

        private MyAbstractAnimatedBlock myBlock;
        private float angle;
        private int speed;
        private Color onColor, offColor;
        private Vector3 axis;

        public void Create(MyAbstractAnimatedBlock myBlock, Vector3 axis, int speed, float angle, Color onColor, Color offColor, string hitboxName)
        {
            Create(myBlock.Block, hitboxName);

            this.speed = speed;
            this.angle = angle;
            this.onColor = onColor;
            this.offColor = offColor;
            this.myBlock = myBlock;
            this.axis = axis;

            myBlock.Block.IsWorkingChanged += OnWorkingChanged;
            
            OnWorkingChanged(myBlock.Block);

            OnHover += HoverChange;
            OnUnHover += HoverChange;
            OnInteract += Interacted;
        }

        private void HoverChange()
        {
            if (IsHovering)
            {
                if (myBlock.Block.IsWorking)
                {
                    MyVisualScriptLogicProvider.SetHighlightLocal(Subpart.MyPart.Name, 20, 0, offColor * .3f);
                }
                else
                {
                    MyVisualScriptLogicProvider.SetHighlightLocal(Subpart.MyPart.Name, 20, 0, onColor * .3f);
                }
            }
            else
            {
                MyVisualScriptLogicProvider.SetHighlightLocal(Subpart.MyPart.Name, 0);
            }
        }

        private void Interacted()
        {
            MyVisualScriptLogicProvider.PlayHudSoundLocal();
            ((IMyFunctionalBlock)myBlock.Block).Enabled = !((IMyFunctionalBlock)myBlock.Block).Enabled;
        }

        public void OnWorkingChanged(MyCubeBlock block)
        {
            Subpart.ClearActions();
            Subpart.Pos.ResetPosRot();
            if (block.IsWorking)
            {
                Subpart.Pos.Rotate(speed, axis, angle, LerpType.Expo, EaseType.InOut);
            }
            else
            {
                Subpart.Pos.Rotate(speed, axis, angle, LerpType.Instant, EaseType.InOut);
                Subpart.Pos.Rotate(speed, axis, -angle, LerpType.Expo, EaseType.InOut);
            }
            Subpart.AddAction(new CustomAction(Subpart, () => { LeverClose?.Invoke(); }, speed));
        }

        public override void Close()
        {
            myBlock.Block.IsWorkingChanged -= OnWorkingChanged;
        }

    }
}
