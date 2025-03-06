using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace Math0424.AnimationCore
{
    abstract class InteractableComp : SubpartComponent
    {

        public Action OnHover;
        public Action OnUnHover;
        
        public Action OnInteract;
        public Action OnRightClick;

        public bool IsHovering { get; private set; }
        private string dummy;
        private IMyCubeBlock block;

        protected void Create(IMyCubeBlock block, string dummy)
        {
            this.block = block;
            this.dummy = dummy;
        }

        public override void Update()
        {
            if (block != null && MyAPIGateway.Session?.Player?.Character != null)
            {
                var x = MyAPIGateway.Session.Player.Character.GetHeadMatrix(true);
                if (Vector3.DistanceSquared(x.Translation, block.PositionComp.GetPosition()) > 50)
                {
                    return;
                }

                string raycast = block.RaycastDetectors(x.Translation, x.Translation + (x.Forward * 2));

                if (raycast != null)
                {
                    if (raycast == dummy)
                    {
                        if (!IsHovering)
                        {
                            OnHover?.Invoke();
                        }
                        IsHovering = true;

                        if (!MyAPIGateway.Gui.IsCursorVisible && !MyAPIGateway.Gui.ChatEntryVisible && MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.None)
                        {
                            if (MyAPIGateway.Input.IsNewLeftMousePressed() || MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.USE))
                            {
                                OnInteract?.Invoke();
                            } 
                            else if(MyAPIGateway.Input.IsNewRightMousePressed())
                            {
                                OnRightClick?.Invoke();
                            }
                        }
                    }
                }
                else if(IsHovering)
                {
                    IsHovering = false;
                    OnUnHover?.Invoke();
                }
            }
        }

    }
}
