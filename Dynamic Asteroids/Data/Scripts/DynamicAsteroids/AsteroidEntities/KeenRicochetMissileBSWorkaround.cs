using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities {
    public class KeenRicochetMissileBSWorkaroundHandler {
        private static IMyMissiles _missileAPI;
        private static bool _isInitialized = false;
        private static AsteroidDamageHandler _damageHandler;

        public KeenRicochetMissileBSWorkaroundHandler(AsteroidDamageHandler damageHandler) {
            _damageHandler = damageHandler;
            InitializeMissileAPI();
        }

        private void InitializeMissileAPI() {
            if (!_isInitialized) {
                _missileAPI = MyAPIGateway.Missiles;
                if (_missileAPI != null) {
                    _missileAPI.OnMissileCollided += OnMissileCollided;
                    _isInitialized = true;
                    Log.Info("Initialized Keen missile ricochet workaround.");
                }
                else {
                    Log.Warning("Failed to initialize Keen missile ricochet workaround - API not available.");
                }
            }
        }

        private float CalculateMissileDamage(IMyMissile missile) {
            var missileDefinition = missile.AmmoDefinition as MyMissileAmmoDefinition;
            if (missileDefinition == null) return 0;

            // Always use the health pool if it exists - this represents the full damage potential
            if (missile.HealthPool > 0) {
                float damage = missile.HealthPool;
                Log.Info($"Using missile health pool as damage: {damage}");
                return damage;
            }
            // Fallback to explosion damage if no health pool
            else if (missileDefinition.MissileExplosionDamage > 0) {
                return missileDefinition.MissileExplosionDamage;
            }

            return 0;
        }

        private void OnMissileCollided(IMyMissile missile) {
            try {
                if (missile?.CollidedEntity == null) return;

                var asteroid = missile.CollidedEntity as AsteroidEntity;
                if (asteroid == null) return;

                float damage = CalculateMissileDamage(missile);
                if (damage <= 0) return;

                Vector3D impactPosition = missile.CollisionPoint ?? missile.PositionComp.GetPosition();

                var hitInfo = new MyHitInfo {
                    Position = impactPosition,
                    Normal = missile.CollisionNormal,
                    Velocity = missile.LinearVelocity
                };

                _damageHandler.DoDamage(asteroid, damage, MyStringHash.GetOrCompute("Missile"), true, hitInfo, missile.Owner);

                // TODO: patch over this with a mvsp.createexplosion or something if we really want it
                _missileAPI.Remove(missile.EntityId);
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(KeenRicochetMissileBSWorkaroundHandler),
                    "Error in Keen missile ricochet workaround");
            }
        }

        public void Unload() {
            if (_isInitialized && _missileAPI != null) {
                _missileAPI.OnMissileCollided -= OnMissileCollided;
                _isInitialized = false;
                Log.Info("Cleaned up Keen missile ricochet workaround.");
            }
        }
    }
}
