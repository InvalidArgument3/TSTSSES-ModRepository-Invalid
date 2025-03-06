using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace Math0424.AnimationCore
{

    /// <summary>
    /// This entire class is disgusting - turn back now
    /// </summary>
    class EasyAnimation
    {

        private MyAbstractAnimatedBlock Block;
        private List<MyTuple<string, BaseAction>> Actions;

        public EasyAnimation(MyAbstractAnimatedBlock block, BlockAnimation anim)
        {
            Actions = new List<MyTuple<string, BaseAction>>();
            Block = block;

            foreach (string v in anim.Subparts)
            {
                ParseAndCreateAnimation(v, anim);
            }

        }

        private void ParseAndCreateAnimation(string name, BlockAnimation anim)
        {
            MySubpart p = Block.GetSubpart(name);
            if (p == null)
                throw new Exception($"Unknown subpart name :: {name}");

            switch (anim.Component)
            {
                case Components.Move:
                    Quaternion q;
                    switch (anim.Movement)
                    {
                        case MoveTypes.ResetPos:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new CustomAction(p, () => { p.Pos.ResetPos(); }, anim.Delay)
                            });
                            return;
                        case MoveTypes.ResetLerp:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new MatrixAction(p, anim.Frames, p.Pos.GetOrginMatrix(), anim.Lerp, anim.Ease, anim.Delay)
                            });
                            return;
                        case MoveTypes.ResetAll:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new CustomAction(p, () => { p.Pos.ResetPosRot(); }, anim.Delay)
                            });
                            return;
                        case MoveTypes.SetResetPos:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new CustomAction(p, () => { p.Pos.SetOriginMatrix(); }, anim.Delay)
                            });
                            return;
                        case MoveTypes.Translate:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new MoveAction(p, anim.Frames, anim.Transform, anim.Lerp, anim.Ease, anim.Delay)
                            });
                            return;
                        case MoveTypes.Rotate:
                            q = Quaternion.CreateFromAxisAngle(anim.Axis, MathHelper.ToRadians((float)anim.Angle));
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new RotateAction(p, anim.Frames, q, anim.Lerp, anim.Ease, anim.Delay)
                            });
                            return;
                        case MoveTypes.Spin:
                            q = Quaternion.CreateFromAxisAngle(anim.Axis, MathHelper.ToRadians((float)anim.Angle));
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new RotationAction(p, anim.Frames, q, anim.Delay)
                            });
                            return;
                        case MoveTypes.SpinAroundOrgin:
                            q = Quaternion.CreateFromAxisAngle(anim.Axis, MathHelper.ToRadians((float)anim.Angle));
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new RotationAroundOrigin(p, anim.Frames, q, anim.Delay)
                            });
                            return;
                        case MoveTypes.Vibrate:
                            Actions.Add(new MyTuple<string, BaseAction>()
                            {
                                Item1 = name,
                                Item2 = new VibrateAction(p, anim.Frames, (float)anim.Scale, anim.Delay)
                            });
                            return;
                        case MoveTypes.Trigger:
                            SetTrigger(anim.Trigger);
                            return;
                    }
                    return;
                case Components.Effects:
                    return;
            }
        }

        private void SetTrigger(TriggerType trigger)
        {
            switch(trigger)
            {
                case TriggerType.OnCreate:
                    QueueAnimation();
                    return;
                case TriggerType.OnBuilt:
                    Block.Block.IsWorkingChanged += (e) => { if (e.IsBuilt) { QueueAnimation(); } };
                    return;

                case TriggerType.OnWorkingChanged:
                    Block.Block.IsWorkingChanged += (e) => { QueueAnimation(); };
                    return;
                case TriggerType.OnWorking:
                    Block.Block.IsWorkingChanged += (e) => { if (e.IsWorking) { QueueAnimation(); } };
                    return;
                case TriggerType.OnNotWorking:
                    Block.Block.IsWorkingChanged += (e) => { if (!e.IsWorking) { QueueAnimation(); } };
                    return;

                case TriggerType.OnEnabledChanged:
                    ((IMyFunctionalBlock)Block.Block).EnabledChanged += (e) => { QueueAnimation(); };
                    return;
                case TriggerType.OnOwnershipChanged:
                    ((IMyFunctionalBlock)Block.Block).OwnershipChanged += (e) => { QueueAnimation(); };
                    return;
                case TriggerType.OnNameChanged:
                    ((IMyFunctionalBlock)Block.Block).CustomNameChanged += (e) => { QueueAnimation(); };
                    return;

                case TriggerType.OnStartedProducing:
                    ((IMyProductionBlock)Block.Block).StartedProducing += QueueAnimation;
                    return;
                case TriggerType.OnStoppedProducing:
                    ((IMyProductionBlock)Block.Block).StoppedProducing += QueueAnimation;
                    return;

                case TriggerType.OnLockedToGround:
                    ((IMyLandingGear)Block.Block).LockModeChanged += (e, a) => { if (a == SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode.Locked) { QueueAnimation(); } };
                    return;
                case TriggerType.OnUnLockedToGround:
                    ((IMyLandingGear)Block.Block).LockModeChanged += (e, a) => { if (a == SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode.Unlocked) { QueueAnimation(); } };
                    return;
                case TriggerType.OnReadyLockToGround:
                    ((IMyLandingGear)Block.Block).LockModeChanged += (e, a) => { if (a == SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode.ReadyToLock) { QueueAnimation(); } };
                    return;

                case TriggerType.OnDoorOpen:
                    ((IMyAdvancedDoor)Block.Block).DoorStateChanged += (b) => { if (b) { QueueAnimation(); } };
                    return;
                case TriggerType.OnDoorClose:
                    ((IMyAdvancedDoor)Block.Block).DoorStateChanged += (b) => { if (!b) { QueueAnimation(); } };
                    return;
            }
        }

        public void QueueAnimation()
        {
            foreach(var v in Actions)
            {
                Block.GetSubpart(v.Item1).AddAction(v.Item2);
            }
        }

    }
}
