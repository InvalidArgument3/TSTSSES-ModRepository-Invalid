using Math0424.AnimationCore;
using Math0424.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using static Math0424.AnimationCoreAPI.AnimationCoreAPI;

namespace ResourceNodes
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Drill), false, "AdvancedStaticDrill")]
    class AdvancedStaticDrill : MyDrillBlock
    {
        MySubpart drillHead, powerLever;

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
                packet.colors.Add("Emissive3", color.PackedValue);
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
            baseSpeed = 100;
            invMultiplier = 6;
            gatherammount = 8;            
            basePowerDraw = 1.2f;
            maxPowerDraw = 8.5f;
        }

        public override bool LoadSubparts()
        {
            powerLever?.Delete();

            drillHead = LoadSubpartFromFile("DrillHead", "Models/AdvancedDrill/DrillHead");
            powerLever = LoadSubpartFromFile("PowerLever", "Models/AdvancedDrill/PowerLever");

            drillHead.SubpartDeleted += ReloadSubparts;

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
            drillHead.Pos.SetOriginMatrix(MatrixD.CreateTranslation(-Vector3.Up));
            drillHead.Pos.ResetPosRot();

            powerLever.Pos.SetOriginMatrix(MatrixD.CreateTranslation(new Vector3(1.47f, -3.42, 3.07f)));
            powerLever.Pos.ResetPosRot();

            powerLever.AddComponent<PowerLeverComp>().Create(this, new Vector3(.54, 0, .83), 50, -180, Color.Green, Color.Red, "lever");

        }

        int AnimationFrames = -1;
        public override void AnimationUpdate()
        {
            if (IsProducing)
            {
                AnimationFrames++;
                if (AnimationFrames % 200 == 0)
                {
                    drillHead.Pos.Rotate(40, Vector3.Up, 180, LerpType.Bounce, EaseType.Out);
                    drillHead.Pos.Translate(40, Vector3.Down * 1.8f, LerpType.Elastic, EaseType.Out); 
                    //drillHead.Effects.PlaySound("AdvancedDrillSlam");

                    drillHead.Pos.Rotate(40, Vector3.Up, 180, LerpType.Expo, EaseType.InOut, 100);
                    drillHead.Pos.Translate(40, Vector3.Up * 1.8f, LerpType.Expo, EaseType.InOut, 100);

                    drillHead.Pos.ResetPos(140);

                    drillHead.Pos.Translate(40, Vector3.Up * .1f, LerpType.Back, EaseType.Out, 141);
                }
            } 
            else
            {
                AnimationFrames = -1;
                drillHead.ClearActions();
                drillHead.Pos.ResetPos();
            }
        }

        int level = 3;
        private void TickEmissive()
        {
            level = (level + 1) % 5;

            if (level == 4)
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
            Block.SetEmissiveParts("Emissive3", color, level);
        }
    }
}
