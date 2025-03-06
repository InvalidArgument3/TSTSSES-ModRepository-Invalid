using Math0424.AnimationCore;
using Math0424.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace ResourceNodes
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false, "StaticDrill")]
    class MediumStaticDrill : MyDrillBlock
    {
        MySubpart slammer, pipes;

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
                packet.colors.Add("Emissive2", color.PackedValue);
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
            baseSpeed = 200;
            invMultiplier = 4;
            gatherammount = 5;
            basePowerDraw = 1.2f;
            maxPowerDraw = 6.5f;
        }

        public override bool LoadSubparts()
        {
            pipes?.Delete();

            slammer = LoadSubpartFromFile("Slammer", "Models/MediumDrill/Slammer");
            pipes = LoadSubpartFromFile("Pipes", "Models/MediumDrill/Pipes");

            slammer.SubpartDeleted += ReloadSubparts;
            return true;
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
            MatrixD m = MatrixD.CreateTranslation((Vector3.Right * 1.15f) + (Vector3.Up * 1.5f));

            slammer.Pos.SetOriginMatrix(m);
            slammer.Pos.ResetPosRot();

            m = MatrixD.CreateRotationY(3.14);
            m.Translation = (Vector3.Right * -.75f) + (Vector3.Up * .25f) + (Vector3.Forward * -1.15f);
            pipes.Pos.SetOriginMatrix(m);
            pipes.Pos.ResetPosRot();

            AnimationFrames = -1;
        }

        int AnimationFrames = -1;
        public override void AnimationUpdate()
        {
            if (IsProducing)
            {
                AnimationFrames++;
                if (AnimationFrames % 100 == 0)
                {
                    slammer.Pos.Rotate(20, Vector3.Up, 180, LerpType.Bounce, EaseType.Out);
                    slammer.Pos.Translate(20, Vector3.Down, LerpType.Elastic, EaseType.Out);

                    //slammer.AddAction(new CustomAction(slammer, () => { slammer.Effects.PlaySound("MediumDrillSlam"); }, 15));

                    slammer.Pos.Rotate(20, Vector3.Up, 180, LerpType.Expo, EaseType.InOut, 50);
                    slammer.Pos.Translate(20, Vector3.Up, LerpType.Expo, EaseType.InOut, 50);

                    pipes.Pos.Rotate(30, Vector3.Up, -29, LerpType.Cubic, EaseType.In, 60);
                    pipes.Pos.ResetPosRot(90);

                    slammer.Pos.ResetPos(70);

                    slammer.Pos.Translate(20, Vector3.Up * .1f, LerpType.Back, EaseType.Out, 71);
                }
            } 
            else
            {
                if (AnimationFrames != -1)
                {
                    slammer.Pos.TranslateToOrginMatix(50, LerpType.Back, EaseType.Out);
                    pipes.Pos.TranslateToOrginMatix(50, LerpType.Back, EaseType.Out);
                }
                AnimationFrames = -1;
            }
        }

        int level = 2;
        private void TickEmissive()
        {
            level = (level + 1) % 4;

            if (level == 3)
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
            Block.SetEmissiveParts("Emissive2", color, level);
        }

    }
}
