﻿using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using Sandbox.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Sandbox.Engine.Physics;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities
{
    public class AsteroidDamageHandler
    {
        private int AblationStage { get; set; } = 0;  // Tracks the current ablation stage
        private const int MaxAblationStages = 3;  // Maximum number of ablation stages
        private readonly float[] ablationMultipliers = new float[] { 1.0f, 0.75f, 0.5f };  // Multiplier for each ablation stage

        private void CreateEffects(Vector3D position)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("roidbreakparticle1", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("roidbreak", position);
        }

        public void SplitAsteroid(AsteroidEntity asteroid)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            int splits = MainSession.I.Rand.Next(2, 5);

            if (splits > asteroid.Size)
                splits = (int)Math.Ceiling(asteroid.Size);

            float newSize = asteroid.Size / splits;

            CreateEffects(asteroid.PositionComp.GetPosition());

            if (newSize <= AsteroidSettings.MinSubChunkSize)
            {
                MyPhysicalItemDefinition item = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Ore), asteroid.Type.ToString()));
                var newObject = MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeId.ToString()) as MyObjectBuilder_PhysicalObject;
                for (int i = 0; i < splits; i++)
                {
                    int dropAmount = GetRandomDropAmount(asteroid.Type);
                    MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(dropAmount, newObject), asteroid.PositionComp.GetPosition() + RandVector(MainSession.I.Rand) * asteroid.Size, Vector3D.Forward, Vector3D.Up, asteroid.Physics);
                }

                var removalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true, false, Quaternion.Identity);
                var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, removalMessageBytes);

                MainSession.I._spawner.TryRemoveAsteroid(asteroid); // Use the TryRemoveAsteroid method
                asteroid.Close();
                return;
            }

            for (int i = 0; i < splits; i++)
            {
                Vector3D newPos = asteroid.PositionComp.GetPosition() + RandVector(MainSession.I.Rand) * asteroid.Size;
                Vector3D newVelocity = RandVector(MainSession.I.Rand) * AsteroidSettings.GetRandomSubChunkVelocity(MainSession.I.Rand);
                Vector3D newAngularVelocity = RandVector(MainSession.I.Rand) * AsteroidSettings.GetRandomSubChunkAngularVelocity(MainSession.I.Rand);
                Quaternion newRotation = Quaternion.CreateFromYawPitchRoll(
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi);

                var subChunk = AsteroidEntity.CreateAsteroid(newPos, newSize, newVelocity, asteroid.Type, newRotation);
                subChunk.Physics.AngularVelocity = newAngularVelocity;

                MainSession.I._spawner.AddAsteroid(subChunk); // Use the AddAsteroid method

                var message = new AsteroidNetworkMessage(newPos, newSize, newVelocity, newAngularVelocity, asteroid.Type, true, subChunk.EntityId, false, true, newRotation);
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);
            }

            var finalRemovalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true, false, Quaternion.Identity);
            var finalRemovalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(finalRemovalMessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, finalRemovalMessageBytes);

            MainSession.I._spawner.TryRemoveAsteroid(asteroid); // Use the TryRemoveAsteroid method
            asteroid.Close();
        }

        private int GetRandomDropAmount(AsteroidType type)
        {
            switch (type)
            {
                case AsteroidType.Ice:
                    return MainSession.I.Rand.Next(AsteroidSettings.IceDropRange[0], AsteroidSettings.IceDropRange[1]);
                case AsteroidType.Stone:
                    return MainSession.I.Rand.Next(AsteroidSettings.StoneDropRange[0], AsteroidSettings.StoneDropRange[1]);
                case AsteroidType.Iron:
                    return MainSession.I.Rand.Next(AsteroidSettings.IronDropRange[0], AsteroidSettings.IronDropRange[1]);
                case AsteroidType.Nickel:
                    return MainSession.I.Rand.Next(AsteroidSettings.NickelDropRange[0], AsteroidSettings.NickelDropRange[1]);
                case AsteroidType.Cobalt:
                    return MainSession.I.Rand.Next(AsteroidSettings.CobaltDropRange[0], AsteroidSettings.CobaltDropRange[1]);
                case AsteroidType.Magnesium:
                    return MainSession.I.Rand.Next(AsteroidSettings.MagnesiumDropRange[0], AsteroidSettings.MagnesiumDropRange[1]);
                case AsteroidType.Silicon:
                    return MainSession.I.Rand.Next(AsteroidSettings.SiliconDropRange[0], AsteroidSettings.SiliconDropRange[1]);
                case AsteroidType.Silver:
                    return MainSession.I.Rand.Next(AsteroidSettings.SilverDropRange[0], AsteroidSettings.SilverDropRange[1]);
                case AsteroidType.Gold:
                    return MainSession.I.Rand.Next(AsteroidSettings.GoldDropRange[0], AsteroidSettings.GoldDropRange[1]);
                case AsteroidType.Platinum:
                    return MainSession.I.Rand.Next(AsteroidSettings.PlatinumDropRange[0], AsteroidSettings.PlatinumDropRange[1]);
                case AsteroidType.Uraninite:
                    return MainSession.I.Rand.Next(AsteroidSettings.UraniniteDropRange[0], AsteroidSettings.UraniniteDropRange[1]);
                default:
                    return 0;
            }
        }

        private int[] GetDropRange(AsteroidType type)
        {
            switch (type)
            {
                case AsteroidType.Ice:
                    return AsteroidSettings.IceDropRange;
                case AsteroidType.Stone:
                    return AsteroidSettings.StoneDropRange;
                case AsteroidType.Iron:
                    return AsteroidSettings.IronDropRange;
                case AsteroidType.Nickel:
                    return AsteroidSettings.NickelDropRange;
                case AsteroidType.Cobalt:
                    return AsteroidSettings.CobaltDropRange;
                case AsteroidType.Magnesium:
                    return AsteroidSettings.MagnesiumDropRange;
                case AsteroidType.Silicon:
                    return AsteroidSettings.SiliconDropRange;
                case AsteroidType.Silver:
                    return AsteroidSettings.SilverDropRange;
                case AsteroidType.Gold:
                    return AsteroidSettings.GoldDropRange;
                case AsteroidType.Platinum:
                    return AsteroidSettings.PlatinumDropRange;
                case AsteroidType.Uraninite:
                    return AsteroidSettings.UraniniteDropRange;
                default:
                    return null;
            }
        }

        private void AblateAsteroid(AsteroidEntity asteroid, MyHitInfo? hitInfo = null)
        {
            AblationStage++;
            float newSize = asteroid.Size * ablationMultipliers[AblationStage];

            if (newSize < AsteroidSettings.MinSubChunkSize)
            {
                Log.Info("Asteroid too small after ablation, removing it.");
                asteroid.OnDestroy();
                return;
            }

            float previousSize = asteroid.Size;
            asteroid.UpdateSizeAndPhysics(newSize);

            // Scale integrity based on size change
            float integrityScale = newSize / previousSize;
            asteroid._integrity = Math.Max(1f, asteroid._integrity * integrityScale);

            Log.Info($"Asteroid ablated to stage {AblationStage}, new size: {newSize}, new integrity: {asteroid._integrity}");

            if (hitInfo.HasValue)
            {
                SpawnDebrisAtImpact(asteroid, hitInfo.Value.Position, 1.0f);
            }
        }

        public void SpawnDebrisAtImpact(AsteroidEntity asteroid, Vector3D impactPosition, float healthLostRatio)
        {
            // Define the drop range based on asteroid type
            int[] dropRange = GetDropRange(asteroid.Type);
            if (dropRange == null)
            {
                Log.Warning("Invalid asteroid type or drop range not defined.");
                return;
            }

            // Calculate the base drop amount proportional to health lost
            int minDrop = dropRange[0];
            int maxDrop = dropRange[1];

            // Apply additional scaling for weak weapons to limit debris from small hits
            // Weak hits result in almost no debris unless a significant amount of health is lost
            float scalingFactor = 0.5f; // Adjust this as needed to fine-tune how much weak weapons contribute
            int dropAmount = (int)((minDrop + (maxDrop - minDrop) * healthLostRatio) * scalingFactor);

            // Ensure that very small drops (from weak hits) are handled
            if (dropAmount < minDrop * 0.1f)
            {
                dropAmount = 1;  // Smallest possible drop, trace amount
            }

            Log.Info($"Spawning {dropAmount} debris at impact location due to {healthLostRatio:P} health lost.");

            // Create the floating debris
            MyPhysicalItemDefinition item = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Ore), asteroid.Type.ToString()));
            var newObject = MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeId.ToString()) as MyObjectBuilder_PhysicalObject;

            // Spawn the items at the impact site
            MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(dropAmount, newObject), impactPosition, Vector3D.Forward, Vector3D.Up, asteroid.Physics);
        }

        private Vector3D RandVector(Random rand)
        {
            var theta = rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(rand.NextDouble(), 1 / 3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }

        public bool DoDamage(AsteroidEntity asteroid, float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true, MyStringHash? extraInfo = null)
        {
            try
            {
                // Pass the damageSource and hitInfo to ReduceIntegrity
                ReduceIntegrity(asteroid, damage, damageSource, hitInfo);

                if (asteroid._integrity <= 0)
                {
                    asteroid.OnDestroy();  // Call destruction logic when integrity reaches zero
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), "Exception in DoDamage");
                return false;
            }
        }

        private void ReduceIntegrity(AsteroidEntity asteroid, float damage, MyStringHash damageSource, MyHitInfo? hitInfo)
        {
            float finalDamage = damage;
            float initialIntegrity = asteroid._integrity;

            if (damageSource.String == "Explosion")
            {
                // Large explosion (warheads, etc.) - splits the asteroid
                finalDamage *= 10.0f;
                Log.Info($"Large explosion detected! Applying 10x damage multiplier. Original Damage: {damage}, Final Damage: {finalDamage}");

                asteroid._integrity -= finalDamage;
                if (asteroid._integrity <= 0)
                {
                    SplitAsteroid(asteroid);
                }
            }
            else if (damageSource.String == "SmallExplosion") // For rockets, missiles
            {
                finalDamage *= 5.0f;
                Log.Info($"Small explosion detected! Applying 5x damage multiplier. Original Damage: {damage}, Final Damage: {finalDamage}");

                asteroid._integrity -= finalDamage;
                if (asteroid._integrity <= 0)
                {
                    if (AblationStage < MaxAblationStages - 1)
                    {
                        AblateAsteroid(asteroid, hitInfo);
                    }
                    else
                    {
                        SplitAsteroid(asteroid);
                    }
                }
            }
            else if (damageSource.String == "Bullet")
            {
                Log.Info($"Bullet damage detected. Original Damage: {damage}");
                asteroid._integrity -= finalDamage;

                // Calculate size reduction based on damage percentage
                float damagePercentage = finalDamage / initialIntegrity;
                if (damagePercentage > 0.1f) // Only reduce size if significant damage is done
                {
                    float sizeReduction = asteroid.Size * (damagePercentage * 0.1f); // 10% of the damage percentage
                    float newSize = Math.Max(AsteroidSettings.MinSubChunkSize, asteroid.Size - sizeReduction);

                    if (newSize != asteroid.Size)
                    {
                        asteroid.UpdateSizeAndPhysics(newSize);

                        // Spawn debris proportional to size reduction
                        if (hitInfo.HasValue)
                        {
                            SpawnDebrisAtImpact(asteroid, hitInfo.Value.Position, damagePercentage);
                        }
                    }
                }

                if (asteroid._integrity <= 0)
                {
                    if (asteroid.Size <= AsteroidSettings.MinSubChunkSize)
                    {
                        asteroid.OnDestroy();
                    }
                    else
                    {
                        AblateAsteroid(asteroid, hitInfo);
                    }
                }
            }
            else
            {
                asteroid._integrity -= finalDamage;
                if (asteroid._integrity <= 0)
                {
                    asteroid.OnDestroy();
                }
            }
        }
    }
}