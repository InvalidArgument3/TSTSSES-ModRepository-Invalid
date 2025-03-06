using Math0424.AnimationCore;
using Math0424.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace ResourceNodes
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false, "BasicStaticDrill")]
    class BasicStaticDrill : MyDrillBlock
    {
        MySubpart drillHead, tube1, tube2, tube3;

        public override void SetEmissive(Color color)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                EmissiveStateChange packet = new EmissiveStateChange()
                {
                    blockId = Block.EntityId,
                };
                packet.colors.Add("Emissive0", color.PackedValue);
                packet.colors.Add("Emissive1", color.PackedValue);
                ResourceNode.Instance.Network.TransmitToPlayersWithinRange(Block.PositionComp.GetPosition(), packet, 500, false);
            }
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                SetEmissives(color);
            }
        }


        public override void BlockInit()
        {
            DepositedResources += TickEmissive;
            baseSpeed = 300;
            invMultiplier = 2;
            gatherammount = 2;
            basePowerDraw = 1.2f;
            maxPowerDraw = 5.0f;
        }

        public override bool LoadSubparts()
        {
            tube1?.Delete();
            tube2?.Delete();
            tube3?.Delete();

            drillHead = LoadSubpartFromFile("DrillHead", "Models/BasicDrill/DrillHead");
            tube1 = LoadSubpartFromFile("Tube1", "Models/BasicDrill/Tube");
            tube2 = LoadSubpartFromFile("Tube2", "Models/BasicDrill/Tube");
            tube3 = LoadSubpartFromFile("Tube3", "Models/BasicDrill/Tube");

            drillHead.SubpartDeleted += DeleteSubparts;
            drillHead.SubpartDeleted += ReloadSubparts;
            return true;
        }

        private void DeleteSubparts()
        {
            tube1?.Delete();
            tube2?.Delete();
            tube3?.Delete();
        }

        public void RespawnSubparts()
        {
            if (Block != null && Block.IsBuilt && Block.InScene && !Block.MarkedForClose)
            {
                ReloadSubparts();
            }
        }

        public override void BeforeFirstAnimationUpdate()
        {
            MatrixD m = MatrixD.CreateTranslation(Vector3.Right * -1.25f + Vector3.Up);

            drillHead.Pos.ResetPosRot();
            tube1.Pos.ResetPosRot();
            tube2.Pos.ResetPosRot();
            tube3.Pos.ResetPosRot();

            drillHead.Pos.SetOriginMatrix(m);
            drillHead.Pos.ResetPosRot();

            m.Translation += Vector3.Up;
            tube1.Pos.SetOriginMatrix(m);
            tube1.Pos.ResetPosRot();

            m.Translation += Vector3.Down * .75f;
            m.Down *= .75f;
            m.Forward *= .75f;
            m.Right *= .75f;
            tube2.Pos.SetOriginMatrix(m);
            tube2.Pos.ResetPosRot();

            m.Translation += Vector3.Down * .5f;
            m.Down *= .75f;
            m.Forward *= .75f;
            m.Right *= .75f;
            tube3.Pos.SetOriginMatrix(m);
            tube3.Pos.ResetPosRot();

            AnimationFrames = -1;
        }

        int AnimationFrames = -1;
        public override void AnimationUpdate()
        {
            if (IsProducing)
            {
                AnimationFrames++;
                if (AnimationFrames % 300 == 0)
                {
                    if (AnimationFrames == 0)
                    {
                        drillHead.Pos.Rotate(300, Vector3.Up, 180, LerpType.Cubic, EaseType.In);
                        //drillHead.Effects.PlaySound("ArcToolShipDrillRock");

                        tube1.Pos.Rotate(300, Vector3.Up, 180, LerpType.Cubic, EaseType.In);
                        tube2.Pos.Rotate(300, Vector3.Up, 180, LerpType.Cubic, EaseType.In);
                        tube3.Pos.Rotate(300, Vector3.Up, 180, LerpType.Cubic, EaseType.In);

                        tube1.Pos.Translate(200, Vector3.Down * .5f, LerpType.Expo, EaseType.InOut, 100);
                        tube2.Pos.Translate(200, Vector3.Down * 1f, LerpType.Expo, EaseType.InOut, 100);
                        tube3.Pos.Translate(200, Vector3.Down * 1.8f, LerpType.Expo, EaseType.InOut, 100);

                        drillHead.Pos.Translate(200, Vector3.Down * 1.8f, LerpType.Expo, EaseType.InOut, 100);
                    }
                    else
                    {
                        drillHead.Pos.ConstantRotation(300, Vector3.Up, -7);
                        tube1.Pos.ConstantRotation(300, Vector3.Up, -1);
                        tube2.Pos.ConstantRotation(300, Vector3.Up, -3);
                        tube3.Pos.ConstantRotation(300, Vector3.Up, -5);
                    }
                }
            } 
            else
            {
                if (AnimationFrames != -1)
                {
                    drillHead.Pos.TranslateToOrginMatix(50, LerpType.Expo, EaseType.InOut);
                    tube1.Pos.TranslateToOrginMatix(50, LerpType.Expo, EaseType.InOut);
                    tube2.Pos.TranslateToOrginMatix(50, LerpType.Expo, EaseType.InOut);
                    tube3.Pos.TranslateToOrginMatix(50, LerpType.Expo, EaseType.InOut);
                }
                AnimationFrames = -1;
            }
        }

        int level = 1;
        private void TickEmissive()
        {
            level = (level + 1) % 3;

            if (level == 2)
            {
                SetEmissive(Color.Gray);
            } 
            else
            {
                EmissiveStateChange packet = new EmissiveStateChange()
                {
                    blockId = Block.EntityId,
                };
                packet.colors.Add("Emissive" + level, Color.Green.PackedValue);
                ResourceNode.Instance.Network.TransmitToPlayersWithinRange(Block.PositionComp.GetPosition(), packet, 500, false);
                
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    Block.SetEmissiveParts("Emissive" + level, Color.Green, 1);
                }
            }
        }

        private void SetEmissives(Color color, float level = 0)
        {
            Block.SetEmissiveParts("Emissive0", color, level);
            Block.SetEmissiveParts("Emissive1", color, level);
        }

    }
}
