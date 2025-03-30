using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
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

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities {
    public enum AsteroidType {
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

    public class AsteroidEntity : MyEntity, IMyDestroyableObject {
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

        public static AsteroidEntity CreateAsteroid(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation = null, long? entityId = null) {
            var ent = new AsteroidEntity();
            try {
                if (entityId.HasValue)
                    ent.EntityId = entityId.Value;

                var massRange = AsteroidSettings.MinMaxMassByType[type];
                string ringDebugInfo;
                float distanceScale = AsteroidSettings.CalculateMassScaleByDistance(position, MainSession.I.RealGasGiantsApi, out ringDebugInfo);

                float randomFactor = (float)MainSession.I.Rand.NextDouble() * 0.2f - 0.1f;
                float finalMass = MathHelper.Lerp(massRange.MinMass, massRange.MaxMass, distanceScale + randomFactor);
                finalMass = MathHelper.Clamp(finalMass, massRange.MinMass, massRange.MaxMass);

                // Create physical properties first
                ent.Properties = AsteroidPhysicalProperties.CreateFromMass(finalMass, AsteroidPhysicalProperties.DEFAULT_DENSITY, ent);

                if (!rotation.HasValue && MyAPIGateway.Session.IsServer) {
                    Vector3D randomAxis = RandVector();
                    float randomAngle = (float)(MainSession.I.Rand.NextDouble() * Math.PI * 2);
                    rotation = Quaternion.CreateFromAxisAngle(randomAxis, randomAngle);
                }
                else if (!rotation.HasValue) {
                    rotation = Quaternion.Identity;
                }

                // Pass the calculated diameter instead of the input size
                ent.Init(position, ent.Properties.Diameter, initialVelocity, type, rotation);
                MyEntities.Add(ent);

                if (!MyEntities.EntityExists(ent.EntityId)) {
                    Log.Warning($"Asteroid {ent.EntityId} failed to be added to the scene.");
                    return null;
                }

                Log.Info($"Spawned ring asteroid {ent.EntityId}:" +
                         $"\nType: {type}" +
                         $"\nMass Range: {massRange.MinMass:N0}kg - {massRange.MaxMass:N0}kg" +
                         $"\nFinal Mass: {finalMass:N0}kg" +
                         $"\nFinal Diameter: {ent.Properties.Diameter:F2}m" +
                         $"\nRandom Factor: {randomFactor:F3}" +
                         $"\nPosition: {position}" +
                         $"\nVelocity: {initialVelocity.Length():F1}m/s" +
                         $"\n{ringDebugInfo}");

                return ent;
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(AsteroidEntity), "Exception during asteroid creation");
                return null;
            }
        }

        private void Init(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation) {
            try {
                Type = type;
                ModelString = SelectModelForAsteroidType(type);

                // Don't create new Properties here, use the one created in CreateAsteroid
                Log.Info($"Initializing asteroid at {position} with size {size} and type {type}");

                // Set up the model with the proper scale
                float modelScale = size; // dividing by 2 is a bit too small when compared to the hitbox but would make an ok bounding sphere
                Init(null, ModelString, null, modelScale);

                if (string.IsNullOrEmpty(ModelString)) {
                    Log.Warning($"Failed to assign model for asteroid type {type}");
                }

                PositionComp.SetPosition(position);

                if (rotation.HasValue) {
                    MatrixD worldMatrix = MatrixD.CreateFromQuaternion(rotation.Value);
                    worldMatrix.Translation = position;
                    WorldMatrix = worldMatrix;
                }

                CreatePhysics();

                if (Physics == null) {
                    Log.Warning($"Physics creation failed for asteroid {EntityId}");
                }

                Physics.LinearVelocity = initialVelocity;

                if (MyAPIGateway.Session.IsServer) {
                    SyncFlag = true;
                }

                Log.Info($"Asteroid {EntityId} initialized:" +
                         $"\n - Position: {PositionComp.GetPosition()}" +
                         $"\n - Velocity: {initialVelocity}" +
                         $"\n - Model Scale: {modelScale}" +
                         $"\n - Physics Radius: {Properties.Radius}");
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(AsteroidEntity), "Failed to initialize AsteroidEntity");
                Flags &= ~EntityFlags.Visible;
            }
        }

        private string SelectModelForAsteroidType(AsteroidType type) {
            // Select model based on asteroid type (same as before, refactor for clarity)
            string modPath = MainSession.I.ModContext.ModPath;
            switch (type) {
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

        private string GetRandomModel(string[] models, string modPath) {
            if (models.Length == 0) {
                Log.Info("Model array is empty");
                return string.Empty;
            }

            int modelIndex = MainSession.I.Rand.Next(models.Length);
            Log.Info($"Selected model index: {modelIndex}");
            return Path.Combine(modPath, models[modelIndex]);
        }

        public void DrawDebugSphere() {
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
            if (!MyAPIGateway.Session.IsServer) return;

            Vector3D finalPosition = PositionComp.GetPosition(); // Get position before cleanup

            // Play destruction effects (server-side is fine, clients might need synced effects)
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("roidbreakparticle1", finalPosition);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("roidbreak", finalPosition);

            // Spawn remaining mass as floating objects
            var damageHandler = new AsteroidDamageHandler();
            damageHandler.SpawnDebrisAtImpact(this, finalPosition, Properties.Mass);

            // --- Targeted Network Message ---
            var finalRemovalMessage = new AsteroidNetworkMessage(
                finalPosition,
                Properties.Diameter,
                Vector3D.Zero, // Velocity doesn't matter for removal
                Vector3D.Zero, // Angular velocity doesn't matter
                Type,
                false,
                EntityId,
                true,  // Mark as removal
                false, // Not initial creation
                Quaternion.Identity // Rotation doesn't matter
            );

            byte[] finalRemovalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(finalRemovalMessage);

            if (finalRemovalMessageBytes != null && finalRemovalMessageBytes.Length > 0)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                foreach (IMyPlayer player in players)
                {
                     if (player.SteamUserId == MyAPIGateway.Multiplayer.ServerId) continue; // Don't send to self if host

                     Vector3D playerPosition = player.GetPosition();
                     // Use the same relevance distance as updates
                     if (Vector3D.DistanceSquared(finalPosition, playerPosition) <= AsteroidSpawner.ASTEROID_UPDATE_RELEVANCE_DISTANCE * AsteroidSpawner.ASTEROID_UPDATE_RELEVANCE_DISTANCE)
                     {
                         MyAPIGateway.Multiplayer.SendMessageTo(32000, finalRemovalMessageBytes, player.SteamUserId);
                     }
                }
                 Log.Info($"Sent targeted final removal message for asteroid {EntityId}");
            }
            // --- End Targeted Network Message ---


            // Remove from spawner and entities (server-side)
            if (MainSession.I?._spawner != null)
            {
                // Need to ensure TryRemoveAsteroid exists and works correctly or use the modified RemoveAsteroid
                 MainSession.I._spawner.TryRemoveAsteroid(this); // Or call the modified RemoveAsteroid if needed
            }

             // Use MarkForClose to ensure proper cleanup sequence if needed, otherwise remove directly.
             // MyEntities.Remove(this); // Direct removal
             Close(); // Ensure entity closes properly
        }

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null,
            long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true,
            MyStringHash? extraInfo = null) {
            Log.Info(
                $"DoDamage called with damage: {damage}, damageSource: {damageSource}, " +
                $"integrity before damage: {Properties.CurrentIntegrity}");

            var damageHandler = new AsteroidDamageHandler();
            return damageHandler.DoDamage(this, damage, damageSource, sync, hitInfo, attackerId, realHitEntityId,
                shouldDetonateAmmo, extraInfo);
        }

        public void CreatePhysics() {
            try {
                if (Physics != null) {
                    Physics.Close();
                    Physics = null;
                }

                Log.Info($"Creating physics for asteroid {EntityId}:" +
                         $"\n - Mass: {Properties.Mass:N0}kg" +
                         $"\n - Volume: {Properties.Volume:N0}m³" +
                         $"\n - Radius: {Properties.Radius:F2}m");

                PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForPhysics(
                    this,
                    MatrixD.CreateTranslation(this.PositionComp.GetPosition()),
                    Vector3.Zero,
                    linearDamping: 0f,
                    angularDamping: 0.01f,
                    rigidBodyFlags: RigidBodyFlag.RBF_DEFAULT,
                    collisionLayer: CollisionLayers.NoVoxelCollisionLayer,
                    isPhantom: false,
                    mass: new ModAPIMass(Properties.Volume, Properties.Mass, Vector3.Zero,
                        Properties.Mass * Matrix.Identity)
                );

                MyAPIGateway.Physics.CreateSpherePhysics(settings, Properties.Radius);

                if (MyAPIGateway.Session.IsServer) {
                    const float initialMaxSpin = 0.2f;
                    Vector3D randomSpin = RandVector() * initialMaxSpin;
                    this.Physics.AngularVelocity = randomSpin;
                    Log.Info($"Server: Set initial spin for asteroid {EntityId}: {randomSpin}");
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(AsteroidEntity), $"Error creating physics for asteroid {EntityId}");
            }
        }
        private static Vector3D RandVector() {
            var theta = MainSession.I.Rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * MainSession.I.Rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(MainSession.I.Rand.NextDouble(), 1 / 3d) *
                   new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }
    }
}