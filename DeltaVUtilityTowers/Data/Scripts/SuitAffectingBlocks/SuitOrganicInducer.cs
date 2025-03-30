using VRage.ModAPI;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using System.Collections.Generic;
using System.Linq;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Entities;
using System;
using VRage.Game.Entity;

namespace SuitOrganicInducer
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "SuitOrganicInducer", "SmallSuitOrganicInducer")]
    public class SuitOrganicInducer : MyGameLogicComponent
    {
        private const float ChargeAmount = 0.01f;
        private IMyBeacon _inducerBlock;
        private IMyCharacter _currentTarget;
        private int _noTargetCounter = 0;
        private static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");
        private static Random _random = new Random();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            _inducerBlock = Entity as IMyBeacon;
            if (_inducerBlock != null)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (_inducerBlock?.CubeGrid?.Physics == null)
                return;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        {
            try
            {
                if (_inducerBlock?.IsWorking != true)
                {
                    _inducerBlock.HudText = "Organic Inducer Offline";
                    return;
                }

                // If we have no target or current target is invalid
                if (_currentTarget == null || _noTargetCounter >= 30 || !IsValidTarget(_currentTarget))
                {
                    _currentTarget = FindNewTarget();
                    _noTargetCounter = 0;
                }

                if (_currentTarget != null)
                {
                    var controllingPlayer = _currentTarget.ControllerInfo?.ControllingIdentityId;
                    if (controllingPlayer.HasValue)
                    {
                        float energyLevel = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(controllingPlayer.Value);
                        if (energyLevel >= 1.0f)
                        {
                            _inducerBlock.HudText = "Target fully charged. Searching for new target...";
                            _currentTarget = null;
                        }
                        else
                        {
                            ChargeTarget(_currentTarget);
                            if (_currentTarget != null) // Check again as ChargeTarget might set it to null
                            {
                                UpdateHudText(_currentTarget);
                            }
                        }
                    }
                }
                else
                {
                    _noTargetCounter++;
                    _inducerBlock.HudText = "Searching for target...";
                }
            }
            catch (System.Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"SuitOrganicInducer: Error in UpdateAfterSimulation100: {e}");
            }
        }

        private void UpdateHudText(IMyCharacter target)
        {
            if (target == null || _inducerBlock == null) return;

            var controllingPlayer = target.ControllerInfo?.ControllingIdentityId;
            if (!controllingPlayer.HasValue) return;

            var playerName = MyVisualScriptLogicProvider.GetPlayersName(controllingPlayer.Value);
            var energyLevel = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(controllingPlayer.Value);

            if (energyLevel >= 1.0f)
            {
                _inducerBlock.HudText = $"{playerName}'s suit is fully charged";
            }
            else
            {
                _inducerBlock.HudText = $"Charging {playerName} ({energyLevel:P0})";
            }
        }

        private IMyCharacter FindNewTarget()
        {
            var sphere = new BoundingSphereD(_inducerBlock.GetPosition(), _inducerBlock.Radius);
            var targetEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            var validTargets = new List<KeyValuePair<IMyCharacter, float>>();

            foreach (IMyEntity entity in targetEntities)
            {
                var character = entity as IMyCharacter;
                if (IsValidTarget(character))
                {
                    var controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
                    if (controllingPlayer.HasValue)
                    {
                        float energyLevel = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(controllingPlayer.Value);
                        validTargets.Add(new KeyValuePair<IMyCharacter, float>(character, energyLevel));
                    }
                }
            }

            if (validTargets.Count == 0)
                return null;

            // Sort by energy level and add some randomness
            return validTargets
                .OrderBy(kvp => kvp.Value + (float)_random.NextDouble() * 0.1f)
                .First().Key;
        }

        private bool IsValidTarget(IMyCharacter character)
        {
            if (character == null || character.IsDead)
                return false;

            var controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
            if (!controllingPlayer.HasValue || !IsFriendly(controllingPlayer.Value))
                return false;

            // Check if the character is within range
            double distanceSquared = Vector3D.DistanceSquared(character.GetPosition(), _inducerBlock.GetPosition());
            return distanceSquared <= _inducerBlock.Radius * _inducerBlock.Radius;
        }

        private void ChargeTarget(IMyCharacter character)
        {
            if (character == null) return;

            var controllingPlayer = character.ControllerInfo?.ControllingIdentityId;
            if (!controllingPlayer.HasValue) return;

            var playerid = controllingPlayer.Value;
            var elevel = MyVisualScriptLogicProvider.GetPlayersEnergyLevel(playerid);
            elevel += ChargeAmount;

            if (elevel >= 1)
            {
                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(playerid, 1);
                _currentTarget = null;
            }
            else
            {
                MyVisualScriptLogicProvider.SetPlayersEnergyLevel(playerid, elevel);
            }
        }

        private bool IsFriendly(long playerId)
        {
            var relation = MyIDModule.GetRelationPlayerBlock(_inducerBlock.OwnerId, playerId);
            return relation == MyRelationsBetweenPlayerAndBlock.FactionShare || relation == MyRelationsBetweenPlayerAndBlock.Friends || relation == MyRelationsBetweenPlayerAndBlock.Owner;
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
                MyLog.Default.WriteLineAndConsole($"SuitOrganicInducer: Error in UpdateAfterSimulation: {e}");
            }
        }

        private void DrawDebugLineToTarget()
        {
            if (_inducerBlock != null && _inducerBlock.IsWorking && _currentTarget != null)
            {
                Vector3D sourcePosition = _inducerBlock.GetPosition();
                Vector3D targetPosition = _currentTarget.GetPosition();
                Vector4 blue = Color.Blue.ToVector4();
                MySimpleObjectDraw.DrawLine(sourcePosition, targetPosition, MaterialSquare, ref blue, 0.1f);
            }
        }
    }
}