using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids {
    public partial class MainSession {
        private Queue<AsteroidZone> _lastRemovedZones = new Queue<AsteroidZone>(5); // Add this to class fields
        private HashSet<AsteroidEntity> _orphanedAsteroids = new HashSet<AsteroidEntity>();
        private int _orphanCheckTimer = 0;
        private const int ORPHAN_CHECK_INTERVAL = 60; // Update once per second at 60 fps
        private const double HITBOX_DISPLAY_DISTANCE = 1000; // 1km


        public override void Draw() {
            try {
                if (MyAPIGateway.Session?.Player?.Character == null)
                    return;

                Vector3D characterPosition = MyAPIGateway.Session.Player.Character.PositionComp.GetPosition();

                // Remove server check since we want drawing in both MP client and singleplayer
                if (AsteroidSettings.EnableLogging) {
                    DrawPlayerZones(characterPosition);
                    DrawNearestAsteroidDebug(characterPosition);
                    DrawOrphanedAsteroids();
                }
                else {
                    DrawNearbyAsteroidHitboxes(characterPosition);
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error in Draw");
            }
        }
        private void DrawNearbyAsteroidHitboxes(Vector3D characterPosition) {
            //if (AsteroidSettings.EnableLogging)
            //    return;

            try {
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities, e => e is AsteroidEntity &&
                                                                Vector3D.DistanceSquared(e.GetPosition(), characterPosition) <= HITBOX_DISPLAY_DISTANCE * HITBOX_DISPLAY_DISTANCE);

                foreach (AsteroidEntity asteroid in entities.Cast<AsteroidEntity>()) {
                    if (asteroid == null || asteroid.MarkedForClose)
                        continue;

                    // Calculate health percentage (assuming Properties.CurrentIntegrity and MaximumIntegrity exist)
                    float healthPercentage = asteroid.Properties.GetIntegrityPercentage() / 100f;

                    // Calculate instability percentage
                    float instabilityPercentage = asteroid.Properties.GetInstabilityPercentage() / 100f;

                    // Color interpolation: Green (full health) to Red (low health)
                    Color baseColor = Color.Lerp(
                        Color.Red,   // Low health
                        Color.Green, // Full health
                        healthPercentage
                    );

                    // Add slight transparency
                    Color hitboxColor = new Color(
                        baseColor.R,
                        baseColor.G,
                        baseColor.B,
                        15 // Keep the transparency
                    );

                    // Create wobble effect based on instability
                    Vector3D wobbleOffset = Vector3D.Zero;
                    if (instabilityPercentage > 0) {
                        // Create a time-based wobble
                        double time = DateTime.Now.TimeOfDay.TotalSeconds;
                        float wobbleAmount = instabilityPercentage * 0.2f; // Max 20% radius wobble

                        wobbleOffset = new Vector3D(
                            Math.Sin(time * 5f) * wobbleAmount,
                            Math.Cos(time * 3f) * wobbleAmount,
                            Math.Sin(time * 4f) * wobbleAmount
                        ) * asteroid.Properties.Radius;
                    }

                    // Apply wobble to position
                    MatrixD worldMatrix = MatrixD.CreateTranslation(
                        asteroid.PositionComp.GetPosition() + wobbleOffset
                    );

                    // Draw the sphere
                    MySimpleObjectDraw.DrawTransparentSphere(
                        ref worldMatrix,
                        asteroid.Properties.Radius,
                        ref hitboxColor,
                        MySimpleObjectRasterizer.Wireframe,
                        8,
                        null,
                        MyStringId.GetOrCompute("Square"),
                        0.1f
                    );

                    // Optional: Add a pulsing effect for highly unstable asteroids
                    if (instabilityPercentage > 0.5f) {
                        float pulseIntensity = (float)Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 4f) * 0.5f + 0.5f;
                        Color pulseColor = new Color(255, 0, 0, (byte)(15 * pulseIntensity));

                        MatrixD pulseMatrix = MatrixD.CreateTranslation(asteroid.PositionComp.GetPosition());
                        float pulseRadius = asteroid.Properties.Radius * (1f + (0.1f * pulseIntensity));

                        MySimpleObjectDraw.DrawTransparentSphere(
                            ref pulseMatrix,
                            pulseRadius,
                            ref pulseColor,
                            MySimpleObjectRasterizer.Wireframe,
                            8,
                            null,
                            MyStringId.GetOrCompute("Square"),
                            0.05f // Thinner lines for pulse effect
                        );
                    }
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error drawing hitbox spheres");
            }
        }
        private void DrawPlayerZones(Vector3D characterPosition) {
            // Draw active zones first
            foreach (var kvp in _clientZones) {
                DrawZone(kvp.Key, kvp.Value, characterPosition);
                if (kvp.Value.IsMerged) {
                    DrawZoneMergeConnections(kvp.Value);
                }
            }

            // Draw recently removed zones with fading effect
            foreach (var removedZone in _lastRemovedZones) {
                Color fadeColor = Color.Red * 0.5f; // Semi-transparent red for removed zones
                DrawZoneSphere(removedZone, fadeColor);
            }
        }

        private void DrawZone(long playerId, AsteroidZone zone, Vector3D characterPosition) {
            bool isLocalPlayer = playerId == MyAPIGateway.Session.Player.IdentityId;
            bool playerInZone = zone.IsPointInZone(characterPosition);

            Color zoneColor = DetermineZoneColor(isLocalPlayer, playerInZone, zone.IsMerged);

            DrawZoneSphere(zone, zoneColor);
            DrawZoneInfo(zone, isLocalPlayer, playerInZone);
        }

        private void DrawZoneInfo(AsteroidZone zone, bool isLocalPlayer, bool playerInZone) {
            Vector3D textPosition = zone.Center + new Vector3D(0, zone.Radius + 100, 0);

            // Create info string including new states
            string zoneInfo = $"Asteroids: {zone.ContainedAsteroids?.Count ?? 0}\n" +
                              $"Active: {!zone.IsMarkedForRemoval}\n" +
                              $"Last Active: {(DateTime.UtcNow - zone.LastActiveTime).TotalSeconds:F1}s ago";

            // Draw zone status text
            MyTransparentGeometry.AddLineBillboard(
                MyStringId.GetOrCompute("Square"),
                Color.White,
                textPosition,
                Vector3.Right,
                (float)zone.Radius * 0.1f,
                0.5f,
                MyBillboard.BlendTypeEnum.Standard
            );
        }

        private Color DetermineZoneColor(bool isLocalPlayer, bool playerInZone, bool isMerged) {
            // Handle high-speed check through the AsteroidZone properties instead
            if (isLocalPlayer && MyAPIGateway.Session?.Player != null) {
                AsteroidZone currentZone;
                if (_clientZones.TryGetValue(MyAPIGateway.Session.Player.IdentityId, out currentZone)) {
                    if (currentZone.IsMarkedForRemoval)
                        return Color.Red;
                }
            }

            // Use zone state for coloring
            if (isLocalPlayer)
                return playerInZone ? Color.Green : Color.Yellow;

            if (isMerged)
                return Color.Purple;

            return Color.Blue;
        }

        private void DrawZoneSphere(AsteroidZone zone, Color color) {
            MatrixD worldMatrix = MatrixD.CreateTranslation(zone.Center);
            MySimpleObjectDraw.DrawTransparentSphere(
                ref worldMatrix,
                (float)zone.Radius,
                ref color,
                MySimpleObjectRasterizer.Wireframe,
                20,
                null,
                MyStringId.GetOrCompute("Square"),
                5f
            );
        }

        private void DrawZoneMergeConnections(AsteroidZone sourceZone) {
            foreach (var targetZone in _clientZones.Values) {
                if (targetZone.IsMerged && targetZone != sourceZone) {
                    double distance = Vector3D.Distance(sourceZone.Center, targetZone.Center);
                    if (distance <= sourceZone.Radius + targetZone.Radius) {
                        // Red lines for connections involving marked-for-removal zones
                        Color connectionColor = (sourceZone.IsMarkedForRemoval || targetZone.IsMarkedForRemoval)
                            ? Color.Red
                            : Color.Purple;

                        Vector4 mergeLineColor = connectionColor.ToVector4();
                        MySimpleObjectDraw.DrawLine(
                            sourceZone.Center,
                            targetZone.Center,
                            MyStringId.GetOrCompute("Square"),
                            ref mergeLineColor,
                            2f
                        );
                    }
                }
            }
        }

        private void DrawNearestAsteroidDebug(Vector3D characterPosition) {
            AsteroidEntity nearestAsteroid = FindNearestAsteroid(characterPosition);
            if (nearestAsteroid == null) return;

            DrawAsteroidClientPosition(nearestAsteroid);
            DrawAsteroidServerComparison(nearestAsteroid);
        }

        private void DrawAsteroidClientPosition(AsteroidEntity asteroid) {
            Vector3D clientPosition = asteroid.PositionComp.GetPosition();
            MatrixD clientWorldMatrix = MatrixD.CreateTranslation(clientPosition);
            Color clientColor = Color.Red;
            MySimpleObjectDraw.DrawTransparentSphere(
                ref clientWorldMatrix,
                asteroid.Properties.Radius,
                ref clientColor,
                MySimpleObjectRasterizer.Wireframe,
                20);
        }
        //TODO: this doesnt work lmao gotta send a packet from server where it thinks the roid is
        private void DrawAsteroidServerComparison(AsteroidEntity asteroid) {
            Vector3D serverPosition;
            Quaternion serverRotation;

            if (!_serverPositions.TryGetValue(asteroid.EntityId, out serverPosition) ||
                !_serverRotations.TryGetValue(asteroid.EntityId, out serverRotation))
                return;

            DrawServerPositionSphere(asteroid, serverPosition);
            DrawPositionComparisonLine(asteroid.PositionComp.GetPosition(), serverPosition);
            DrawRotationComparison(asteroid, serverPosition, serverRotation);
            DisplayAsteroidDebugInfo(asteroid, serverPosition, serverRotation);
        }

        private void DrawServerPositionSphere(AsteroidEntity asteroid, Vector3D serverPosition) {
            MatrixD serverWorldMatrix = MatrixD.CreateTranslation(serverPosition);
            Color serverColor = Color.Blue;
            MySimpleObjectDraw.DrawTransparentSphere(
                ref serverWorldMatrix,
                asteroid.Properties.Radius,
                ref serverColor,
                MySimpleObjectRasterizer.Wireframe,
                20);
        }

        private void DrawPositionComparisonLine(Vector3D clientPosition, Vector3D serverPosition) {
            Vector4 lineColor = Color.Yellow.ToVector4();
            MySimpleObjectDraw.DrawLine(
                clientPosition,
                serverPosition,
                MyStringId.GetOrCompute("Square"),
                ref lineColor,
                0.1f);
        }

        private void DrawRotationComparison(AsteroidEntity asteroid, Vector3D serverPosition, Quaternion serverRotation) {
            Vector3D clientForward = asteroid.WorldMatrix.Forward;
            Vector3D serverForward = MatrixD.CreateFromQuaternion(serverRotation).Forward;
            Vector3D clientPosition = asteroid.PositionComp.GetPosition();

            Vector4 clientDirColor = Color.Red.ToVector4();
            Vector4 serverDirColor = Color.Blue.ToVector4();

            MySimpleObjectDraw.DrawLine(
                clientPosition,
                clientPosition + clientForward * asteroid.Properties.Radius * 2,
                MyStringId.GetOrCompute("Square"),
                ref clientDirColor,
                0.1f);

            MySimpleObjectDraw.DrawLine(
                serverPosition,
                serverPosition + serverForward * asteroid.Properties.Radius * 2,
                MyStringId.GetOrCompute("Square"),
                ref serverDirColor,
                0.1f);
        }

        private void DisplayAsteroidDebugInfo(AsteroidEntity asteroid, Vector3D serverPosition, Quaternion serverRotation) {
            float angleDifference = GetQuaternionAngleDifference(
                Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix),
                serverRotation);

            MyAPIGateway.Utilities.ShowNotification(
                $"Asteroid {asteroid.EntityId}:\n" +
                $"Position diff: {Vector3D.Distance(asteroid.PositionComp.GetPosition(), serverPosition):F2}m\n" +
                $"Rotation diff: {MathHelper.ToDegrees(angleDifference):F1}°",
                16);
        }

// File: DynamicAsteroids/DebugDrawTools.cs

        private void UpdateOrphanedAsteroidsList()
        {
            // This method runs on the client to identify asteroids
            // that *appear* orphaned for debug drawing purposes.
            // Actual cleanup is handled by the server in AsteroidSpawner.
            try
            {
                _orphanedAsteroids.Clear();
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities); // Gets entities visible/relevant to the client

                foreach (var entity in entities)
                {
                    var asteroid = entity as AsteroidEntity;
                    if (asteroid == null || asteroid.MarkedForClose) continue;

                    Vector3D asteroidPosition = asteroid.PositionComp.GetPosition();
                    bool isInAnyZone = false;

                    // Check against the client's understanding of active zones
                    foreach (var zoneKvp in _clientZones)
                    {
                        // Consider only active zones for this check
                        if (!zoneKvp.Value.IsMarkedForRemoval && zoneKvp.Value.IsPointInZone(asteroidPosition))
                        {
                            isInAnyZone = true;
                            break;
                        }
                    }

                    if (!isInAnyZone)
                    {
                        // Found a potential orphan on the client side, add for drawing
                        _orphanedAsteroids.Add(asteroid);
                        // Log.Info($"Client identified potential orphan {asteroid.EntityId} for drawing."); // Optional logging
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception without crashing the draw loop
                Log.Exception(ex, typeof(MainSession), "Error updating orphaned asteroids list for drawing");
            }
        }
        private void DrawOrphanedAsteroids() {
            foreach (var asteroid in _orphanedAsteroids) {
                if (asteroid == null || asteroid.MarkedForClose)
                    continue;

                try {
                    Vector3D asteroidPosition = asteroid.PositionComp.GetPosition();
                    MatrixD worldMatrix = MatrixD.CreateTranslation(asteroidPosition);
                    Color orphanColor = new Color(255, 0, 0, 128);

                    MySimpleObjectDraw.DrawTransparentSphere(
                        ref worldMatrix,
                        asteroid.Properties.Radius * 1.5f,
                        ref orphanColor,
                        MySimpleObjectRasterizer.Wireframe,
                        4,
                        null,
                        MyStringId.GetOrCompute("Square"),
                        2f
                    );

                    Vector3D textPosition = asteroidPosition + new Vector3D(0, asteroid.Properties.Radius * 2, 0);
                    string orphanInfo = $"Orphaned Asteroid {asteroid.EntityId}\nType: {asteroid.Type}";
                    MyTransparentGeometry.AddLineBillboard(
                        MyStringId.GetOrCompute("Square"),
                        Color.Red,
                        textPosition,
                        Vector3.Right,
                        asteroid.Properties.Radius * 0.2f,
                        0.5f,
                        MyBillboard.BlendTypeEnum.Standard
                    );
                }
                catch (Exception ex) {
                    _orphanedAsteroids.Remove(asteroid);
                    Log.Warning($"Error drawing orphaned asteroid {asteroid.EntityId}: {ex.Message}");
                }
            }
        }
    }
}
