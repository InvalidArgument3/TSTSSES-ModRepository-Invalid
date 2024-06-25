using System;
using System.IO;
using Invalid.DynamicRoids;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace DynamicAsteroids.AsteroidEntities
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
        private static readonly string[] IceAsteroidModels = {
        @"Models\IceAsteroid_1.mwm",
        @"Models\IceAsteroid_2.mwm",
        @"Models\IceAsteroid_3.mwm",
        @"Models\IceAsteroid_4.mwm"
    };

        private static readonly string[] StoneAsteroidModels = {
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

        private void CreateEffects(Vector3D position)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("roidbreakparticle1", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("roidbreak", position);
        }

        public static AsteroidEntity CreateAsteroid(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation = null, long? entityId = null)
        {
            var ent = new AsteroidEntity();
            Log.Info($"Creating AsteroidEntity at Position: {position}, Size: {size}, InitialVelocity: {initialVelocity}, Type: {type}");

            if (entityId.HasValue)
            {
                ent.EntityId = entityId.Value;
            }

            try
            {
                ent.Init(position, size, initialVelocity, type, rotation);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), "Failed to initialize AsteroidEntity");
                return null;
            }

            if (ent.EntityId == 0)
            {
                Log.Warning("EntityId is 0, which is invalid!");
                return null;
            }

            return ent;
        }

        private void Init(Vector3D position, float size, Vector3D initialVelocity, AsteroidType type, Quaternion? rotation)
        {
            Log.Info($"AsteroidEntity.Init called with position: {position}, size: {size}, initialVelocity: {initialVelocity}, type: {type}");
            try
            {
                if (MainSession.I == null)
                {
                    Log.Exception(new Exception("MainSession.I is null"), typeof(AsteroidEntity), "MainSession.I is not initialized.");
                    return;
                }
                Log.Info("MainSession.I is initialized.");

                if (MainSession.I.ModContext == null)
                {
                    Log.Exception(new Exception("MainSession.I.ModContext is null"), typeof(AsteroidEntity), "MainSession.I.ModContext is not initialized.");
                    return;
                }
                Log.Info("MainSession.I.ModContext is initialized.");

                string modPath = MainSession.I.ModContext.ModPath;
                if (string.IsNullOrEmpty(modPath))
                {
                    Log.Exception(new Exception("MainSession.I.ModContext.ModPath is null or empty"), typeof(AsteroidEntity), "MainSession.I.ModContext.ModPath is not initialized.");
                    return;
                }
                Log.Info($"ModPath: {modPath}");

                if (MainSession.I.Rand == null)
                {
                    Log.Exception(new Exception("MainSession.I.Rand is null"), typeof(AsteroidEntity), "Random number generator is not initialized.");
                    return;
                }

                Type = type;
                Log.Info($"Asteroid Type: {type}");

                switch (type)
                {
                    case AsteroidType.Ice:
                        if (IceAsteroidModels.Length == 0)
                        {
                            Log.Info("IceAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(IceAsteroidModels.Length);
                            Log.Info($"Selected model index for Ice: {modelIndex}");
                            ModelString = Path.Combine(modPath, IceAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Stone:
                        if (StoneAsteroidModels.Length == 0)
                        {
                            Log.Info("StoneAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(StoneAsteroidModels.Length);
                            Log.Info($"Selected model index for Stone: {modelIndex}");
                            ModelString = Path.Combine(modPath, StoneAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Iron:
                        if (IronAsteroidModels.Length == 0)
                        {
                            Log.Info("IronAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(IronAsteroidModels.Length);
                            Log.Info($"Selected model index for Iron: {modelIndex}");
                            ModelString = Path.Combine(modPath, IronAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Nickel:
                        if (NickelAsteroidModels.Length == 0)
                        {
                            Log.Info("NickelAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(NickelAsteroidModels.Length);
                            Log.Info($"Selected model index for Nickel: {modelIndex}");
                            ModelString = Path.Combine(modPath, NickelAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Cobalt:
                        if (CobaltAsteroidModels.Length == 0)
                        {
                            Log.Info("CobaltAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(CobaltAsteroidModels.Length);
                            Log.Info($"Selected model index for Cobalt: {modelIndex}");
                            ModelString = Path.Combine(modPath, CobaltAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Magnesium:
                        if (MagnesiumAsteroidModels.Length == 0)
                        {
                            Log.Info("MagnesiumAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(MagnesiumAsteroidModels.Length);
                            Log.Info($"Selected model index for Magnesium: {modelIndex}");
                            ModelString = Path.Combine(modPath, MagnesiumAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Silicon:
                        if (SiliconAsteroidModels.Length == 0)
                        {
                            Log.Info("SiliconAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(SiliconAsteroidModels.Length);
                            Log.Info($"Selected model index for Silicon: {modelIndex}");
                            ModelString = Path.Combine(modPath, SiliconAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Silver:
                        if (SilverAsteroidModels.Length == 0)
                        {
                            Log.Info("SilverAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(SilverAsteroidModels.Length);
                            Log.Info($"Selected model index for Silver: {modelIndex}");
                            ModelString = Path.Combine(modPath, SilverAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Gold:
                        if (GoldAsteroidModels.Length == 0)
                        {
                            Log.Info("GoldAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(GoldAsteroidModels.Length);
                            Log.Info($"Selected model index for Gold: {modelIndex}");
                            ModelString = Path.Combine(modPath, GoldAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Platinum:
                        if (PlatinumAsteroidModels.Length == 0)
                        {
                            Log.Info("PlatinumAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(PlatinumAsteroidModels.Length);
                            Log.Info($"Selected model index for Platinum: {modelIndex}");
                            ModelString = Path.Combine(modPath, PlatinumAsteroidModels[modelIndex]);
                        }
                        break;
                    case AsteroidType.Uraninite:
                        if (UraniniteAsteroidModels.Length == 0)
                        {
                            Log.Info("UraniniteAsteroidModels array is empty");
                        }
                        else
                        {
                            int modelIndex = MainSession.I.Rand.Next(UraniniteAsteroidModels.Length);
                            Log.Info($"Selected model index for Uraninite: {modelIndex}");
                            ModelString = Path.Combine(modPath, UraniniteAsteroidModels[modelIndex]);
                        }
                        break;
                    default:
                        Log.Info("Invalid AsteroidType, setting ModelString to empty.");
                        ModelString = "";
                        break;
                }
                Log.Info($"ModelString: {ModelString}");

                if (string.IsNullOrEmpty(ModelString))
                {
                    Log.Exception(new Exception("ModelString is null or empty"), typeof(AsteroidEntity), "Failed to initialize asteroid entity");
                    return; // Early exit if ModelString is not set
                }

                Size = size;
                _integrity = AsteroidSettings.BaseIntegrity * Size;
                Log.Info($"Base Integrity: {AsteroidSettings.BaseIntegrity}, Size: {Size}, Total Integrity: {_integrity}");

                Log.Info($"Attempting to load model: {ModelString}");

                Init(null, ModelString, null, Size);

                Save = false;
                NeedsWorldMatrix = true;
                PositionComp.LocalAABB = new BoundingBox(-Vector3.Half * Size, Vector3.Half * Size);
                Log.Info($"LocalAABB: {PositionComp.LocalAABB}");

                Log.Info("Setting WorldMatrix");
                if (rotation.HasValue)
                {
                    WorldMatrix = MatrixD.CreateFromQuaternion(rotation.Value) * MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up);
                }
                else
                {
                    var randomRotation = MatrixD.CreateFromQuaternion(Quaternion.CreateFromYawPitchRoll(
                        (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                        (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                        (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi));

                    WorldMatrix = randomRotation * MatrixD.CreateWorld(position, Vector3D.Forward, Vector3D.Up);
                }

                WorldMatrix.Orthogonalize();
                Log.Info($"WorldMatrix: {WorldMatrix}");

                Log.Info("Adding entity to MyEntities");
                MyEntities.Add(this);
                Log.Info($"{(MyAPIGateway.Session.IsServer ? "Server" : "Client")}: Added asteroid entity with ID {EntityId} to MyEntities");

                Log.Info("Creating physics");
                CreatePhysics();
                Physics.LinearVelocity = initialVelocity + RandVector() * AsteroidSettings.VelocityVariability;
                Physics.AngularVelocity = RandVector() * AsteroidSettings.GetRandomAngularVelocity(MainSession.I.Rand);
                Log.Info($"Initial LinearVelocity: {Physics.LinearVelocity}, Initial AngularVelocity: {Physics.AngularVelocity}");

                Log.Info($"Asteroid model {ModelString} loaded successfully with initial angular velocity: {Physics.AngularVelocity}");

                if (MyAPIGateway.Session.IsServer)
                {
                    SyncFlag = true;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Exception Type: {ex.GetType()}");
                Log.Info($"Exception Message: {ex.Message}");
                Log.Info($"Exception Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Log.Info($"Inner Exception Type: {ex.InnerException.GetType()}");
                    Log.Info($"Inner Exception Message: {ex.InnerException.Message}");
                    Log.Info($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
                Log.Exception(ex, typeof(AsteroidEntity), $"Failed to load model: {ModelString}");
                Flags &= ~EntityFlags.Visible;
            }
        }

        public float Size;
        public string ModelString = "";
        public AsteroidType Type;
        private float _integrity;

        public void SplitAsteroid()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Log.ServerInfo($"Splitting asteroid with ID {EntityId}, Size: {Size}");

            int splits = MainSession.I.Rand.Next(2, 5);

            if (splits > Size)
                splits = (int)Math.Ceiling(Size);

            float newSize = Size / splits;

            CreateEffects(PositionComp.GetPosition());

            if (newSize <= AsteroidSettings.MinSubChunkSize)
            {
                MyPhysicalItemDefinition item = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Ore), Type.ToString()));
                var newObject = MyObjectBuilderSerializer.CreateNewObject(item.Id.TypeId, item.Id.SubtypeId.ToString()) as MyObjectBuilder_PhysicalObject;
                for (int i = 0; i < splits; i++)
                {
                    int dropAmount = GetRandomDropAmount(Type);
                    MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(dropAmount, newObject), PositionComp.GetPosition() + RandVector() * Size, Vector3D.Forward, Vector3D.Up, Physics);
                    Log.ServerInfo($"Spawned {dropAmount} of {Type} at {PositionComp.GetPosition() + RandVector() * Size}");
                }

                var removalMessage = new AsteroidNetworkMessage(PositionComp.GetPosition(), Size, Vector3D.Zero, Vector3D.Zero, Type, false, EntityId, true, false, Quaternion.Identity);
                var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, removalMessageBytes);

                Log.ServerInfo($"Sent removal message for asteroid with ID {EntityId}");

                MainSession.I._spawner._asteroids.Remove(this);
                Close();
                return;
            }

            for (int i = 0; i < splits; i++)
            {
                Vector3D newPos = PositionComp.GetPosition() + RandVector() * Size;
                Vector3D newVelocity = RandVector() * AsteroidSettings.GetRandomSubChunkVelocity(MainSession.I.Rand);
                Vector3D newAngularVelocity = RandVector() * AsteroidSettings.GetRandomSubChunkAngularVelocity(MainSession.I.Rand);
                Quaternion newRotation = Quaternion.CreateFromYawPitchRoll(
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi,
                    (float)MainSession.I.Rand.NextDouble() * MathHelper.TwoPi);

                var subChunk = CreateAsteroid(newPos, newSize, newVelocity, Type, newRotation);
                subChunk.Physics.AngularVelocity = newAngularVelocity;

                MainSession.I._spawner._asteroids.Add(subChunk);

                Log.ServerInfo($"Created sub-chunk asteroid with ID {subChunk.EntityId} at {newPos}");

                var message = new AsteroidNetworkMessage(newPos, newSize, newVelocity, newAngularVelocity, Type, true, subChunk.EntityId, false, true, newRotation);
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);
            }

            var finalRemovalMessage = new AsteroidNetworkMessage(PositionComp.GetPosition(), Size, Vector3D.Zero, Vector3D.Zero, Type, false, EntityId, true, false, Quaternion.Identity);
            var finalRemovalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(finalRemovalMessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, finalRemovalMessageBytes);

            Log.ServerInfo($"Sent final removal message for asteroid with ID {EntityId}");

            MainSession.I._spawner._asteroids.Remove(this);
            Close();
        }

        private int GetRandomDropAmount(AsteroidType type)
        {
            int dropAmount = 0;
            switch (type)
            {
                case AsteroidType.Ice:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.IceDropRange[0], AsteroidSettings.IceDropRange[1]);
                    break;
                case AsteroidType.Stone:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.StoneDropRange[0], AsteroidSettings.StoneDropRange[1]);
                    break;
                case AsteroidType.Iron:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.IronDropRange[0], AsteroidSettings.IronDropRange[1]);
                    break;
                case AsteroidType.Nickel:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.NickelDropRange[0], AsteroidSettings.NickelDropRange[1]);
                    break;
                case AsteroidType.Cobalt:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.CobaltDropRange[0], AsteroidSettings.CobaltDropRange[1]);
                    break;
                case AsteroidType.Magnesium:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.MagnesiumDropRange[0], AsteroidSettings.MagnesiumDropRange[1]);
                    break;
                case AsteroidType.Silicon:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.SiliconDropRange[0], AsteroidSettings.SiliconDropRange[1]);
                    break;
                case AsteroidType.Silver:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.SilverDropRange[0], AsteroidSettings.SilverDropRange[1]);
                    break;
                case AsteroidType.Gold:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.GoldDropRange[0], AsteroidSettings.GoldDropRange[1]);
                    break;
                case AsteroidType.Platinum:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.PlatinumDropRange[0], AsteroidSettings.PlatinumDropRange[1]);
                    break;
                case AsteroidType.Uraninite:
                    dropAmount = MainSession.I.Rand.Next(AsteroidSettings.UraniniteDropRange[0], AsteroidSettings.UraniniteDropRange[1]);
                    break;
            }
            Log.ServerInfo($"Generated drop amount for {type}: {dropAmount}");
            return dropAmount;
        }

        public void OnDestroy()
        {
            try
            {
                Log.ServerInfo($"OnDestroy called for asteroid with ID {EntityId}");
                SplitAsteroid();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidEntity), "Exception in OnDestroy:");
                throw; // Rethrow the exception for the debugger
            }
        }

        public bool DoDamage(float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true, MyStringHash? extraInfo = null)
        {
            //Disabling explosion damage is an awful way to fix this weird rocket bug, but it's okay we'll be using weaponcore :)
            var explosionDamageType = MyStringHash.GetOrCompute("Explosion");

            // Check if the damage source is explosion
            if (damageSource == explosionDamageType)
            {
                Log.Info($"Ignoring explosion damage for asteroid. Damage source: {damageSource.String}");
                return false; // Ignore the damage
            }

            _integrity -= damage;
            Log.Info($"DoDamage called with damage: {damage}, damageSource: {damageSource.String}, attackerId: {attackerId}, realHitEntityId: {realHitEntityId}, new integrity: {_integrity}");

            if (hitInfo.HasValue)
            {
                var hit = hitInfo.Value;
                Log.Info($"HitInfo - Position: {hit.Position}, Normal: {hit.Normal}, Velocity: {hit.Velocity}");
            }

            if (Integrity < 0)
            {
                Log.Info("Integrity below 0, calling OnDestroy");
                OnDestroy();
            }
            return true;
        }

        public float Integrity => _integrity;

        public bool UseDamageSystem => true;

        private void CreatePhysics()
        {
            float mass = 10000 * Size * Size * Size;
            float radius = Size / 2; // Assuming Size represents the diameter

            PhysicsSettings settings = MyAPIGateway.Physics.CreateSettingsForPhysics(
                this,
                WorldMatrix,
                Vector3.Zero,
                linearDamping: 0f, // Remove damping
                angularDamping: 0f, // Remove damping
                rigidBodyFlags: RigidBodyFlag.RBF_DEFAULT,
                collisionLayer: CollisionLayers.NoVoxelCollisionLayer,
                isPhantom: false,
                mass: new ModAPIMass(PositionComp.LocalAABB.Volume(), mass, Vector3.Zero, mass * PositionComp.LocalAABB.Height * PositionComp.LocalAABB.Height / 6 * Matrix.Identity)
            );

            MyAPIGateway.Physics.CreateSpherePhysics(settings, radius);
            Physics.Enabled = true;
            Physics.Activate();

            Log.ServerInfo($"Physics created for asteroid with ID {EntityId}, Mass: {mass}, Radius: {radius}");
        }

        private Vector3D RandVector()
        {
            var theta = MainSession.I.Rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * MainSession.I.Rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(MainSession.I.Rand.NextDouble(), 1 / 3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }

    }
}
