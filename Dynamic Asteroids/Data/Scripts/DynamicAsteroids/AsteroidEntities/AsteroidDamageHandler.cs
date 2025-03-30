using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities {
    public class AsteroidDamageHandler {

        private void CreateEffects(Vector3D position) {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("roidbreakparticle1", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("roidbreak", position);
        }

        public bool DoDamage(AsteroidEntity asteroid, float damage, MyStringHash damageSource, bool sync, MyHitInfo? hitInfo = null, long attackerId = 0, long realHitEntityId = 0, bool shouldDetonateAmmo = true, MyStringHash? extraInfo = null) {
            // Direct conversion of damage to mass loss
            float massToRemove = damage * AsteroidSettings.KgLossPerDamage;

            // Handle instability and chunk spawning
            float instabilityIncrease = damage * AsteroidSettings.InstabilityPerDamage;
            asteroid.AddInstability(instabilityIncrease);

            if (asteroid.Properties.ShouldSpawnChunk()) {
                // Spawn 10% of current mass as a chunk
                float chunkMass = asteroid.Properties.Mass * 0.1f;
                SpawnDebrisAtImpact(asteroid, hitInfo?.Position ?? asteroid.PositionComp.GetPosition(), chunkMass);
                asteroid.Properties.ReduceMass(chunkMass);
                asteroid.Properties.ResetInstability();
            }

            // Handle direct damage mass loss
            if (hitInfo.HasValue) {
                asteroid.Properties.ReduceMass(massToRemove);
                SpawnDebrisAtImpact(asteroid, hitInfo.Value.Position, massToRemove);
            }

            if (asteroid.Properties.IsDestroyed()) {
                asteroid.OnDestroy();
                return true;
            }

            return true;
        }

        public void SpawnDebrisAtImpact(AsteroidEntity asteroid, Vector3D impactPosition, float massLost) {
            MyPhysicalItemDefinition itemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(
                new MyDefinitionId(typeof(MyObjectBuilder_Ore), asteroid.Type.ToString()));

            var newObject = MyObjectBuilderSerializer.CreateNewObject(
                itemDefinition.Id.TypeId,
                itemDefinition.Id.SubtypeId.ToString()) as MyObjectBuilder_PhysicalObject;
            // Try to find nearby debris of same type to combine with
            float groupingRadius = 10.0f;
            List<MyFloatingObject> nearbyDebris = GetNearbyDebris(impactPosition, groupingRadius, newObject);

            if (nearbyDebris.Count > 0) {
                MyFloatingObject closestDebris = nearbyDebris[0];
                MyFloatingObjects.AddFloatingObjectAmount(closestDebris, (VRage.MyFixedPoint)massLost);
            }
            else {
                MyFloatingObjects.Spawn(
                    new MyPhysicalInventoryItem((VRage.MyFixedPoint)massLost, newObject),
                    impactPosition,
                    Vector3D.Forward,
                    Vector3D.Up,
                    asteroid?.Physics,
                    entity => {
                        MyFloatingObject debris = entity as MyFloatingObject;
                        if (debris?.Physics != null) {
                            debris.Physics.LinearVelocity = asteroid?.Physics?.LinearVelocity ?? Vector3D.Zero;
                            debris.Physics.AngularVelocity = MyUtils.GetRandomVector3Normalized() * 5;
                        }
                    }
                );
            }
        }

        private List<MyFloatingObject> GetNearbyDebris(Vector3D position, float radius, MyObjectBuilder_PhysicalObject itemType) {
            List<MyFloatingObject> nearbyDebris = new List<MyFloatingObject>();
            BoundingSphereD boundingSphereD = new BoundingSphereD(position, radius);

            foreach (var entity in MyAPIGateway.Entities.GetEntitiesInSphere(ref boundingSphereD)) {
                MyFloatingObject floatingObj = entity as MyFloatingObject;
                // Only group with same type of floating objects
                if (floatingObj != null && floatingObj.Item.Content.GetType() == itemType.GetType()
                    && floatingObj.Item.Content.SubtypeName == itemType.SubtypeName) {
                    nearbyDebris.Add(floatingObj);
                }
            }

            return nearbyDebris;
        }

        private Vector3D RandVector(Random rand) {
            var theta = rand.NextDouble() * 2.0 * Math.PI;
            var phi = Math.Acos(2.0 * rand.NextDouble() - 1.0);
            var sinPhi = Math.Sin(phi);
            return Math.Pow(rand.NextDouble(), 1 / 3d) *
                   new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
        }

    }
}