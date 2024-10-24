﻿using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.IO;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;
using Color = VRageMath.Color;
using VRage;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities
{
    public enum AsteroidType
    {
        Ice,
        Stone,
        Iron,
        Nickel,
        Cobalt,
        Magnesium,
        Silicon,
        Silver,
        Gold,
        Platinum,
        Uraninite
    }

    public class AsteroidEntity : MyEntity, IMyDestroyableObject
    {
        private static readonly string[] IceAsteroidModels =
        {
            @"Models\IceAsteroid_1.mwm",
            @"Models\IceAsteroid_2.mwm",
            @"Models\IceAsteroid_3.mwm",
            @"Models\IceAsteroid_4.mwm"
        };

        private static readonly string[] StoneAsteroidModels =
        {
            @"Models\StoneAsteroid_1.mwm",
            @"Models\StoneAsteroid_2.mwm",
            @"Models\StoneAsteroid_3.mwm",
            @"Models\StoneAsteroid_4.mwm",
            @"Models\StoneAsteroid_5.mwm",
            @"Models\StoneAsteroid_6.mwm",
            @"Models\StoneAsteroid_7.mwm",
            @"Models\StoneAsteroid_8.mwm",
            @"Models\StoneAsteroid_9.mwm",
            @"Models\StoneAsteroid_10.mwm",
            @"Models\StoneAsteroid_11.mwm",
            @"Models\StoneAsteroid_12.mwm",
            @"Models\StoneAsteroid_13.mwm",
            @"Models\StoneAsteroid_14.mwm",
            @"Models\StoneAsteroid_15.mwm",
            @"Models\StoneAsteroid_16.mwm"
        };

        private static readonly string[] IronAsteroidModels = { @"Models\OreAsteroid_Iron.mwm" };
        private static readonly string[] NickelAsteroidModels = { @"Models\OreAsteroid_Nickel.mwm" };
        private static readonly string[] CobaltAsteroidModels = { @"Models\OreAsteroid_Cobalt.mwm" };
        private static readonly string[] MagnesiumAsteroidModels = { @"Models\OreAsteroid_Magnesium.mwm" };
        private static readonly string[] SiliconAsteroidModels = { @"Models\OreAsteroid_Silicon.mwm" };
        private static readonly string[] SilverAsteroidModels = { @"Models\OreAsteroid_Silver.mwm" };
        private static readonly string[] GoldAsteroidModels = { @"Models\OreAsteroid_Gold.mwm" };
        private static readonly string[] PlatinumAsteroidModels = { @"Models\OreAsteroid_Platinum.mwm" };
        private static readonly string[] UraniniteAsteroidModels = { @"Models\OreAsteroid_Uraninite.mwm" };

        public AsteroidType Type { get; private set; }
        public string ModelString = "";
        public AsteroidPhysicalProperties Properties { get; private set; }

        public float Integrity => Properties.CurrentIntegrity;

        public bool IsUnstable() => Properties.IsUnstable();

        public void UpdateInstability() => Properties.UpdateInstability();

        public void AddInstability(float amount) => Properties.AddInstability(amount);

        // Required property implementation for `IMyDestroyableObject`
        public bool UseDamageSystem => true;


        public static AsteroidEntity CreateAsteroid(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation = null, long? entityId = null)
        {
            var ent = new AsteroidEntity();

            try
            {
                if (entityId.HasValue)
                {
                    ent.EntityId = entityId.Value;
                }

                // Create initial matrix with position
                MatrixD worldMatrix = MatrixD.Identity;
                worldMatrix.Translation = position;

                // Set the world matrix before any other initialization
                ent.WorldMatrix = worldMatrix;

                // Initialize the entity
                ent.Init(position, size, initialVelocity, type, rotation);

                Log.Info($"Creating asteroid at position {position}");

                // Add to entities after initialization
                MyEntities.Add(ent);

                // Verify final position
                Vector3D finalPos = ent.PositionComp.GetPosition();
                Log.Info($"Asteroid {ent.EntityId} final position after creation: {finalPos}");

                return ent;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), "Failed to initialize AsteroidEntity");
                return null;
            }
        }

        private void SetupInitialPositionAndRotation(Vector3D position, Quaternion? rotation)
        {
            // Create rotation matrix
            MatrixD rotationMatrix;
            if (rotation.HasValue)
            {
                rotationMatrix = MatrixD.CreateFromQuaternion(rotation.Value);
            }
            else
            {
                rotationMatrix = MatrixD.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi));
            }

            // Create the world matrix properly
            this.WorldMatrix = MatrixD.Identity;

            // Set rotation first
            MatrixD worldMatrix = this.WorldMatrix;
            worldMatrix.Forward = rotationMatrix.Forward;
            worldMatrix.Up = rotationMatrix.Up;
            worldMatrix.Right = rotationMatrix.Right;

            // Set position directly
            worldMatrix.Translation = position;

            Log.Info($"Setting up asteroid at position {position}:");
            Log.Info($"Final matrix translation: {worldMatrix.Translation}");
            Log.Info($"Final matrix forward: {worldMatrix.Forward}");
            Log.Info($"Final matrix up: {worldMatrix.Up}");
        }

        private void Init(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation)
        {
            try
            {
                Type = type;
                ModelString = SelectModelForAsteroidType(type);
                Properties = new AsteroidPhysicalProperties(size, AsteroidPhysicalProperties.DEFAULT_DENSITY, this);

                // Initialize the base entity
                Init(null, ModelString, null, Properties.Diameter);

                // Set position explicitly
                this.PositionComp.SetPosition(position);

                // Create physics at the correct position
                CreatePhysics();

                // Set initial velocities
                if (this.Physics != null)
                {
                    this.Physics.LinearVelocity = initialVelocity;
                    if (MyAPIGateway.Session.IsServer)
                    {
                        // Only set initial angular velocity on server
                        const float initialSpin = 0.1f;
                        this.Physics.AngularVelocity = RandVector() * initialSpin;
                    }
                }

                Log.Info($"Initialized asteroid {EntityId} at {this.PositionComp.GetPosition()}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), "Failed to initialize AsteroidEntity");
            }
        }

        private string SelectModelForAsteroidType(AsteroidType type)
        {
            // Select model based on asteroid type (same as before, refactor for clarity)
            string modPath = MainSession.I.ModContext.ModPath;
            switch (type)
            {
                case AsteroidType.Ice:
                    return GetRandomModel(IceAsteroidModels, modPath);
                case AsteroidType.Stone:
                    return GetRandomModel(StoneAsteroidModels, modPath);
                case AsteroidType.Iron:
                    return GetRandomModel(IronAsteroidModels, modPath);
                case AsteroidType.Nickel:
                    return GetRandomModel(NickelAsteroidModels, modPath);
                case AsteroidType.Cobalt:
                    return GetRandomModel(CobaltAsteroidModels, modPath);
                case AsteroidType.Magnesium:
                    return GetRandomModel(MagnesiumAsteroidModels, modPath);
                case AsteroidType.Silicon:
                    return GetRandomModel(SiliconAsteroidModels, modPath);
                case AsteroidType.Silver:
                    return GetRandomModel(SilverAsteroidModels, modPath);
                case AsteroidType.Gold:
                    return GetRandomModel(GoldAsteroidModels, modPath);
                case AsteroidType.Platinum:
                    return GetRandomModel(PlatinumAsteroidModels, modPath);
                case AsteroidType.Uraninite:
                    return GetRandomModel(UraniniteAsteroidModels, modPath);
                default:
                    Log.Info("Invalid AsteroidType, no model selected.");
                    return string.Empty;
            }
        }

        private string GetRandomModel(string[] models, string modPath)
        {
            if (models.Length == 0)
            {
                Log.Info("Model array is empty");
                return string.Empty;
            }

            int modelIndex = MainSession.I.Rand.Next(models.Length);
            Log.Info($"Selected model index: {modelIndex}");
            return Path.Combine(modPath, models[modelIndex]);
        }

        public void DrawDebugSphere()
        {
            Vector3D asteroidPosition = this.PositionComp.GetPosition();
            float radius = Properties.Radius;
            Color sphereColor = Color.Red;
            Color otherColor = Color.Yellow;

            // Draw the physics radius
            MatrixD worldMatrix = MatrixD.CreateTranslation(asteroidPosition);
            MySimpleObjectDraw.DrawTransparentSphere(ref worldMatrix, radius, ref sphereColor,
                MySimpleObjectRasterizer.Wireframe, 20);

            // Optionally draw the entity's bounding box for comparison
            BoundingBoxD localBox = PositionComp.LocalAABB;
            MatrixD boxWorldMatrix = WorldMatrix;
            MySimpleObjectDraw.DrawTransparentBox(ref boxWorldMatrix, ref localBox,
                ref otherColor, MySimpleObjectRasterizer.Wireframe, 1, 0.1f);
        }
   
        public void OnDestroy()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            // Play destruction effects
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("roidbreakparticle1", PositionComp.GetPosition());
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("roidbreak", PositionComp.GetPosition());

            // Spawn remaining mass as floating objects
            var damageHandler = new AsteroidDamageHandler();
            damageHandler.SpawnDebrisAtImpact(this, PositionComp.GetPosition(), Properties.Mass);

            // Send network message and clean up
            var finalRemovalMessage = new AsteroidNetworkMessage(
                PositionComp.GetPosition(),
                Properties.Diameter,
                Vector3D.Zero,
                Vector3D.Zero,
                Type,
                false,
                EntityId,
                true,
                false,
                Quaternion.Identity
            );

            var finalRemovalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(finalRemovalMessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, finalRemovalMessageBytes);

            // Remove from spawner and entities
            if (MainSession.I?._spawner != null)
            {
                MainSession.I._spawner.TryRemoveAsteroid(this);
            }
            MyEntities.Remove(this);
            Close();
        }

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null,
            long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true,
            MyStringHash? extraInfo = null)
        {
            Log.Info(
                $"DoDamage called with damage: {damage}, damageSource: {damageSource}, " +
                $"integrity before damage: {Properties.CurrentIntegrity}");

            var damageHandler = new AsteroidDamageHandler();
            return damageHandler.DoDamage(this, damage, damageSource, sync, hitInfo, attackerId, realHitEntityId,
                shouldDetonateAmmo, extraInfo);
        }

        public void CreatePhysics()
        {
            try
            {
                if (Physics != null)
                {
                    Physics.Close();
                    Physics = null;
                }

                // Get current position and create matrix
                Vector3D currentPos = this.PositionComp.GetPosition();
                MatrixD physicsMatrix = MatrixD.Identity;
                physicsMatrix.Translation = currentPos;

                PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForPhysics(
                    this,
                    physicsMatrix,
                    Vector3.Zero,
                    linearDamping: 0f,
                    angularDamping: 0.01f,
                    rigidBodyFlags: RigidBodyFlag.RBF_DEFAULT,
                    collisionLayer: CollisionLayers.NoVoxelCollisionLayer,
                    isPhantom: false,
                    mass: new ModAPIMass(Properties.Volume, Properties.Mass, Vector3.Zero, Properties.Mass * Matrix.Identity)
                );

                MyAPIGateway.Physics.CreateSpherePhysics(settings, Properties.Radius);

                Log.Info($"Created physics for asteroid {EntityId} at {currentPos}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), $"Error creating physics for asteroid {EntityId}");
            }
        }

        private Vector3D RandVector()
        {
            var theta = MainSession.I.Rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * MainSession.I.Rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(MainSession.I.Rand.NextDouble(), 1 / 3d) *
                   new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }
    }
}