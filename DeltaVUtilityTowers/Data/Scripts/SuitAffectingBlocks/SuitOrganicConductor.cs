using VRage.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Entities;
using VRage.Game.Entity;

namespace SuitOrganicConductor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "SuitOrganicConductor", "SmallSuitOrganicConductor")]
    public class SuitOrganicConductor : MyGameLogicComponent
    {
        private const float DamageAmount = 10f;
        private const float ImpulseStrength = 100000f;
        private IMyBeacon _conductorBlock;
        private IMyCharacter _currentTarget;
        private int _chargeUpCounter = 0;
        private const int ChargeUpTime = 30;
        private static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _conductorBlock = Entity as IMyBeacon;
            if (_conductorBlock != null)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (_conductorBlock?.CubeGrid?.Physics == null)
                return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
            try
            {
                if (_conductorBlock.IsWorking)
                {
                    if (_currentTarget == null || !IsValidTarget(_currentTarget))
                    {
                        _currentTarget = FindNearestTarget();
                        _chargeUpCounter = 0;
                    }

                    if (_currentTarget != null)
                    {
                        _chargeUpCounter++;
                        UpdateHudText(_currentTarget);

                        if (_chargeUpCounter >= ChargeUpTime)
                        {
                            ApplyDamageAndEffects(_currentTarget);
                            _chargeUpCounter = 0;
                        }
                    }
                    else
                    {
                        _conductorBlock.HudText = "Searching for target...";
                    }
                }
                else
                {
                    _conductorBlock.HudText = "Organic Conductor Offline";
                }
            }
            catch (System.Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"SuitOrganicConductor: Error in UpdateAfterSimulation10: {e}");
            }
        }

        private void UpdateHudText(IMyCharacter target)
        {
            var controllingPlayer = target.ControllerInfo?.ControllingIdentityId;
            if (controllingPlayer.HasValue)
            {
                var playerName = MyVisualScriptLogicProvider.GetPlayersName(controllingPlayer.Value);
                var chargePercentage = (_chargeUpCounter / (float)ChargeUpTime) * 100;

                if (_chargeUpCounter >= ChargeUpTime)
                {
                    _conductorBlock.HudText = $"Firing at {playerName}!";
                }
                else
                {
                    _conductorBlock.HudText = $"Charging to attack {playerName} ({chargePercentage:F0}%)";
                }
            }
        }

        private IMyCharacter FindNearestTarget()
        {
            var sphere = new BoundingSphereD(_conductorBlock.GetPosition(), _conductorBlock.Radius);
            var targetEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            IMyCharacter nearestTarget = null;
            double nearestDistanceSquared = double.MaxValue;

            foreach (IMyEntity entity in targetEntities)
            {
                var character = entity as IMyCharacter;
                if (IsValidTarget(character))
                {
                    double distanceSquared = Vector3D.DistanceSquared(character.GetPosition(), _conductorBlock.GetPosition());
                    if (distanceSquared < nearestDistanceSquared && distanceSquared <= _conductorBlock.Radius * _conductorBlock.Radius)
                    {
                        nearestTarget = character;
                        nearestDistanceSquared = distanceSquared;
                    }
                }
            }

            return nearestTarget;
        }

        private bool IsValidTarget(IMyCharacter character)
        {
            if (character == null || character.IsDead)
                return false;

            var controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
            if (!controllingPlayer.HasValue || !IsEnemy(controllingPlayer.Value))
                return false;

            // Check if the character is within range
            double distanceSquared = Vector3D.DistanceSquared(character.GetPosition(), _conductorBlock.GetPosition());
            return distanceSquared <= _conductorBlock.Radius * _conductorBlock.Radius;
        }
              
        private void ApplyDamageAndEffects(IMyCharacter character)
        {
            var controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
            if (controllingPlayer.HasValue)
            {
                var playerid = controllingPlayer.Value;
                var health = MyVisualScriptLogicProvider.GetPlayersHealth(playerid);
                health -= DamageAmount;

                if (health <= 0)
                {
                    MyVisualScriptLogicProvider.SetPlayersHealth(playerid, 0);
                }
                else
                {
                    MyVisualScriptLogicProvider.SetPlayersHealth(playerid, health);
                }

                Vector3D impulseDirection = Vector3D.Normalize(character.GetPosition() - _conductorBlock.GetPosition());
                character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, impulseDirection * ImpulseStrength, null, null);

                Vector3D hitPosition = character.GetPosition();
                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("Explosion_Firework_Blue", hitPosition);
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("getzappedidiot", hitPosition);


                controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
                if (controllingPlayer.HasValue)
                {
                    var playerName = MyVisualScriptLogicProvider.GetPlayersName(controllingPlayer.Value);
                    _conductorBlock.HudText = $"Fired at {playerName}!";
                }
            }
        }

        private bool IsEnemy(long playerId)
        {
            return MyIDModule.GetRelationPlayerBlock(_conductorBlock.OwnerId, playerId) == MyRelationsBetweenPlayerAndBlock.Enemies;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session?.Player != null)
                {
                    DrawDebugLineToTarget();
                }
            }
            catch (System.Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"SuitOrganicConductor: Error in UpdateAfterSimulation: {e}");
            }
        }

        private void DrawDebugLineToTarget()
        {
            if (_conductorBlock != null && _conductorBlock.IsWorking && _currentTarget != null)
            {
                Vector3D sourcePosition = _conductorBlock.GetPosition();
                Vector3D targetPosition = _currentTarget.GetPosition();
                float chargeProgress = (float)_chargeUpCounter / ChargeUpTime;
                Vector4 color = Vector4.Lerp(Color.Green.ToVector4(), Color.Red.ToVector4(), chargeProgress);
                MySimpleObjectDraw.DrawLine(sourcePosition, targetPosition, MaterialSquare, ref color, 0.1f);
            }
        }
    }
}