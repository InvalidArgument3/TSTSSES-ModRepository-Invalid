using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using VRage.Utils;
using VRage.ModAPI;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game;
using System;
using System.Security.Cryptography;
using Sandbox.Game.Entities.Cube;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using System.Reflection.Emit;
using Sandbox.Game;

namespace TeleportMechanisms {
    public static class TeleportCore {
        internal static Dictionary<string, List<long>> _TeleportLinks = new Dictionary<string, List<long>>();
        internal static Dictionary<long, TeleportGateway> _instances = new Dictionary<long, TeleportGateway>();
        internal static readonly object _lock = new object();

        public static void UpdateTeleportLinks() {
            lock (_lock) {
                _TeleportLinks.Clear();
                MyLogger.Log($"TPCore: UpdateTeleportLinks: Updating Teleport links. Total instances: {_instances.Count}");

                var gateways = new HashSet<IMyCollector>();
                foreach (var instance in _instances.Values) {
                    if (instance.RingwayBlock != null &&
                        instance.RingwayBlock.IsWorking &&
                        (instance.RingwayBlock.BlockDefinition.SubtypeName == "RingwayCore" ||
                         instance.RingwayBlock.BlockDefinition.SubtypeName == "SmallRingwayCore")) {
                        MyLogger.Log($"TPCore: UpdateTeleportLinks: Found instance gateway: {instance.RingwayBlock.CustomName}, EntityId: {instance.RingwayBlock.EntityId}, IsWorking: {instance.RingwayBlock.IsWorking}");
                        gateways.Add(instance.RingwayBlock);
                    }
                    else {
                        MyLogger.Log($"TPCore: UpdateTeleportLinks: Instance has null or invalid gateway");
                    }
                }

                MyLogger.Log($"TPCore: UpdateTeleportLinks: Total gateways found: {gateways.Count}");

                foreach (var gateway in gateways) {
                    var gatewayLogic = gateway.GameLogic.GetAs<TeleportGateway>();
                    var link = GetTeleportLink(gateway);
                    if (!string.IsNullOrEmpty(link)) {
                        if (!_TeleportLinks.ContainsKey(link)) {
                            _TeleportLinks[link] = new List<long>();
                        }
                        _TeleportLinks[link].Add(gateway.EntityId);
                        MyLogger.Log($"TPCore: UpdateTeleportLinks: Added gateway {gateway.CustomName} (EntityId: {gateway.EntityId}) to link {link}. AllowPlayers: {gatewayLogic.Settings.AllowPlayers}, AllowShips: {gatewayLogic.Settings.AllowShips}");
                    }
                    else {
                        MyLogger.Log($"TPCore: UpdateTeleportLinks: Gateway {gateway.CustomName} (EntityId: {gateway.EntityId}) does not have a valid teleport link");
                    }
                }

                MyLogger.Log($"TPCore: UpdateTeleportLinks: Total Teleport links: {_TeleportLinks.Count}");
                foreach (var kvp in _TeleportLinks) {
                    MyLogger.Log($"TPCore: UpdateTeleportLinks: Link {kvp.Key}: {string.Join(", ", kvp.Value)}");
                }
            }
        }

        public static string GetTeleportLink(IMyCollector gateway) {
            var gatewayLogic = gateway.GameLogic.GetAs<TeleportGateway>();
            if (gatewayLogic != null) {
                MyLogger.Log($"TPCore: GetTeleportLink: GatewayName: {gatewayLogic.Settings.GatewayName}, AllowPlayers: {gatewayLogic.Settings.AllowPlayers}, AllowShips: {gatewayLogic.Settings.AllowShips}");
                return gatewayLogic.Settings.GatewayName;
            }
            return null;
        }

        public static void RequestTeleport(long playerId, long sourceGatewayId, string link) {
            MyLogger.Log($"TPCore: RequestTeleport: Player {playerId}, Gateway {sourceGatewayId}, Link {link}");

            var message = new TeleportRequestMessage {
                PlayerId = (ulong)playerId,
                SourceGatewayId = sourceGatewayId,
                TeleportLink = link
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(message);
            MyLogger.Log($"TPCore: RequestTeleport: Sending teleport request to server for player {playerId}");
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkHandler.TeleportRequestId, data);
        }

        public static void ServerProcessTeleportRequest(TeleportRequestMessage message) {
            MyLogger.Log($"TPCore: ProcessTeleportRequest: Player {message.PlayerId}, Link {message.TeleportLink}");

            List<long> linkedGateways;
            lock (_lock) {
                if (!_TeleportLinks.TryGetValue(message.TeleportLink, out linkedGateways)) {
                    MyLogger.Log($"TPCore: ProcessTeleportRequest: No linked gateways found for link {message.TeleportLink}");
                    return;
                }
            }

            var sourceGateway = MyAPIGateway.Entities.GetEntityById(message.SourceGatewayId) as IMyCollector;
            if (sourceGateway == null) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Source gateway {message.SourceGatewayId} not found");
                return;
            }

            var sourcePosition = sourceGateway.GetPosition();
            long nearestGatewayId = 0;
            double nearestDistance = double.MaxValue;

            foreach (var gatewayId in linkedGateways) {
                if (gatewayId == message.SourceGatewayId) continue;

                var candidateGateway = MyAPIGateway.Entities.GetEntityById(gatewayId) as IMyCollector;
                if (candidateGateway == null) continue;

                var distance = Vector3D.Distance(sourcePosition, candidateGateway.GetPosition());
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestGatewayId = gatewayId;
                }
            }

            if (nearestGatewayId == 0) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: No valid destination gateway found for link {message.TeleportLink}");
                return;
            }

            var destGateway = MyAPIGateway.Entities.GetEntityById(nearestGatewayId) as IMyCollector;
            if (destGateway == null) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Destination gateway {nearestGatewayId} not found");
                return;
            }

            var player = GetPlayerById((long)message.PlayerId);
            if (player == null || player.Character == null) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Player {message.PlayerId} or their character not found");
                return;
            }

            var sourceGatewayLogic = sourceGateway.GameLogic.GetAs<TeleportGateway>();
            if (sourceGatewayLogic == null) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Could not retrieve TeleportGateway for source gateway {sourceGateway.EntityId}");
                return;
            }

            var sourceGatewaySettings = sourceGatewayLogic.Settings;
            if (!sourceGatewaySettings.AllowPlayers) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Player teleportation is not allowed for source gateway {sourceGateway.EntityId}");
                return;
            }

            var isShip = player.Controller.ControlledEntity is IMyCubeBlock;
            if (isShip && !sourceGatewaySettings.AllowShips) {
                MyLogger.Log($"TPCore: ProcessTeleportRequest: Ship teleportation is not allowed for source gateway {sourceGateway.EntityId}");
                return;
            }

            TeleportEntity(player.Character, sourceGateway, destGateway);

            var grid = player.Controller.ControlledEntity?.Entity.GetTopMostParent() as IMyCubeGrid;
            if (grid != null) {
                if (grid.IsStatic) {
                    MyLogger.Log($"TPCore: ProcessTeleportRequest: Grid {grid.DisplayName} is static, teleportation aborted");
                    return;
                }
                if (HasLockedLandingGear(grid)) {
                    MyLogger.Log($"TPCore: ProcessTeleportRequest: Grid {grid.DisplayName} has locked landing gear, teleportation aborted");
                    return;
                }
                TeleportEntity(grid, sourceGateway, destGateway);
            }
        }

        public static void TeleportEntity(IMyEntity entity, IMyCollector sourceGateway, IMyCollector destGateway)
        {
            var relativePosition = entity.GetPosition() - sourceGateway.GetPosition();
            var localPosition = Vector3D.TransformNormal(relativePosition, MatrixD.Invert(sourceGateway.WorldMatrix));
            var newPosition = Vector3D.TransformNormal(localPosition, destGateway.WorldMatrix) + destGateway.GetPosition();

            var entityOrientation = entity.WorldMatrix;
            var relativeOrientation = entityOrientation * MatrixD.Invert(sourceGateway.WorldMatrix);
            var newOrientation = relativeOrientation * destGateway.WorldMatrix;

            var character = entity as IMyCharacter;
            if (character != null)
            {
                character.Teleport(newOrientation);
                character.SetWorldMatrix(newOrientation);
            }
            else
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    TeleportGrid(grid, newOrientation, sourceGateway.WorldMatrix, destGateway.WorldMatrix);
                }
            }

            MyLogger.Log($"TPCore: TeleportEntity: Entity {entity.EntityId} teleported to {newPosition}");
        }

        private static void TeleportGrid(IMyCubeGrid mainGrid, MatrixD newOrientation, MatrixD sourceGatewayMatrix, MatrixD destinationGatewayMatrix)
        {
            MyLogger.Log($"TPGate: TeleportGrid: Starting teleport for main grid {mainGrid.DisplayName} (EntityId: {mainGrid.EntityId})");

            // Get all physically connected grids (including mechanical connections like landing gear)
            var allConnectedGrids = new HashSet<IMyCubeGrid>();
            var physicalGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Physical, mainGrid);
            var mechanicalGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Mechanical, mainGrid);

            if (physicalGroup != null)
            {
                MyAPIGateway.GridGroups.GetGroup(mainGrid, GridLinkTypeEnum.Physical, allConnectedGrids);
                MyLogger.Log($"TPGate: TeleportGrid: Found {allConnectedGrids.Count} physically connected grids");
            }

            if (mechanicalGroup != null)
            {
                MyAPIGateway.GridGroups.GetGroup(mainGrid, GridLinkTypeEnum.Mechanical, allConnectedGrids);
                MyLogger.Log($"TPGate: TeleportGrid: Added mechanical connections, total grids: {allConnectedGrids.Count}");
            }

            // Calculate relative positions before any teleporting
            var relativeMatrices = new Dictionary<IMyCubeGrid, MatrixD>();
            foreach (var grid in allConnectedGrids)
            {
                if (grid != mainGrid)
                {
                    MatrixD relativeMatrix = grid.WorldMatrix * MatrixD.Invert(mainGrid.WorldMatrix);
                    relativeMatrices[grid] = relativeMatrix;
                    MyLogger.Log($"TPGate: TeleportGrid: Calculated relative matrix for grid {grid.DisplayName} (EntityId: {grid.EntityId})");
                }
            }

            // First teleport the main grid
            MyLogger.Log($"TPGate: TeleportGrid: Teleporting main grid to new position");
            mainGrid.Teleport(newOrientation);
            mainGrid.WorldMatrix = newOrientation;

            // Update main grid physics
            var mainPhysics = mainGrid.Physics;
            if (mainPhysics != null)
            {
                mainPhysics.LinearVelocity = Vector3D.Zero;
                mainPhysics.AngularVelocity = Vector3D.Zero;
                float naturalGravityInterference;
                var naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(mainGrid.PositionComp.WorldAABB.Center, out naturalGravityInterference);
                mainPhysics.Gravity = naturalGravity;
                MyLogger.Log($"TPGate: TeleportGrid: Updated main grid physics - Gravity: {naturalGravity}");
            }

            // Now teleport all connected grids
            foreach (var grid in allConnectedGrids)
            {
                if (grid == mainGrid)
                    continue;

                try
                {
                    MatrixD newGridMatrix = relativeMatrices[grid] * mainGrid.WorldMatrix;
                    grid.WorldMatrix = newGridMatrix;

                    var physics = grid.Physics;
                    if (physics != null)
                    {
                        physics.LinearVelocity = Vector3D.Zero;
                        physics.AngularVelocity = Vector3D.Zero;
                        physics.Gravity = mainPhysics?.Gravity ?? Vector3.Zero;
                    }

                    MyLogger.Log($"TPGate: TeleportGrid: Teleported connected grid {grid.DisplayName} (EntityId: {grid.EntityId})");
                }
                catch (Exception ex)
                {
                    MyLogger.Log($"TPGate: TeleportGrid: Error teleporting grid {grid.DisplayName}: {ex.Message}");
                }
            }

            // Verify connections are maintained
            var finalPhysicalGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Physical, mainGrid);
            var finalMechanicalGroup = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Mechanical, mainGrid);

            if (finalPhysicalGroup != null && finalMechanicalGroup != null)
            {
                var finalConnectedGrids = new HashSet<IMyCubeGrid>();
                MyAPIGateway.GridGroups.GetGroup(mainGrid, GridLinkTypeEnum.Physical, finalConnectedGrids);
                MyAPIGateway.GridGroups.GetGroup(mainGrid, GridLinkTypeEnum.Mechanical, finalConnectedGrids);

                MyLogger.Log($"TPGate: TeleportGrid: Final connected grids count: {finalConnectedGrids.Count}");
                if (finalConnectedGrids.Count != allConnectedGrids.Count)
                {
                    MyLogger.Log($"TPGate: TeleportGrid: Warning - Grid connections may have been affected during teleport");
                }
            }
        }

        public static void ClientApplyTeleportResponse(TeleportResponseMessage message) {
            MyLogger.Log($"TPCore: ApplyTeleport: Player {message.PlayerId}, Success {message.Success}");
            if (!message.Success) {
                MyLogger.Log($"TPCore: ApplyTeleport: Teleport unsuccessful for player {message.PlayerId}");
                return;
            }

            var player = GetPlayerById((long)message.PlayerId);
            if (player == null || player.Character == null) {
                MyLogger.Log($"TPCore: ApplyTeleport: Player {message.PlayerId} or their character not found during teleport");
                return;
            }

            // Teleport the player's controlled grid, if any
            var controlledEntity = player.Controller.ControlledEntity;
            if (controlledEntity != null) {
                var topMostParent = controlledEntity.Entity.GetTopMostParent();
                var grid = topMostParent as IMyCubeGrid;
                if (grid != null) {
                    MyLogger.Log($"TPCore: ApplyTeleport: Attempting to teleport ship: {grid.DisplayName}");
                    var shipRelativeOrientation = grid.WorldMatrix * MatrixD.Invert(player.Character.WorldMatrix);
                    var newShipOrientation = shipRelativeOrientation * message.NewOrientation;

                    // Use the new TeleportGrid method with source and destination gateway orientations
                    TeleportGrid(grid, newShipOrientation, message.SourceGatewayMatrix, message.DestinationGatewayMatrix);

                    MyLogger.Log($"TPCore: ApplyTeleport: Ship {grid.DisplayName} teleported");

                }
            }
            else {
                // Teleport the player's character
                player.Character.Teleport(message.NewOrientation);
                player.Character.SetWorldMatrix(message.NewOrientation);
                MyLogger.Log($"TPCore: ApplyTeleport: Player {message.PlayerId} teleported to {message.NewPosition}");
            }
        }

        public static long GetDestinationGatewayId(string link, long sourceGatewayId) {
            List<long> linkedGateways;
            lock (_lock) {
                if (!_TeleportLinks.TryGetValue(link, out linkedGateways) || linkedGateways.Count < 2) {
                    MyLogger.Log($"TPCore: GetDestinationGatewayId: No valid linked gateways found for link {link}");
                    return 0;
                }
            }

            var sourceGateway = MyAPIGateway.Entities.GetEntityById(sourceGatewayId) as IMyCollector;
            if (sourceGateway == null) {
                MyLogger.Log($"TPCore: GetDestinationGatewayId: Source gateway {sourceGatewayId} not found");
                return 0;
            }

            var sourcePosition = sourceGateway.GetPosition();

            long nearestGatewayId = 0;
            double nearestDistance = double.MaxValue;

            foreach (var gatewayId in linkedGateways) {
                if (gatewayId == sourceGatewayId) continue;

                var destinationGateway = MyAPIGateway.Entities.GetEntityById(gatewayId) as IMyCollector;
                if (destinationGateway == null) continue;

                var distance = Vector3D.Distance(sourcePosition, destinationGateway.GetPosition());
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestGatewayId = gatewayId;
                }
            }

            if (nearestGatewayId == 0) {
                MyLogger.Log($"TPCore: GetDestinationGatewayId: No valid destination gateway found for link {link}");
            }

            return nearestGatewayId;
        }

        public static int TeleportNearbyShips(IMyCollector sourceGateway, IMyCollector destGateway) {
            if (!sourceGateway.IsWorking || !destGateway.IsWorking) {
                MyLogger.Log($"TPCore: TeleportNearbyShips: Source or destination gateway not functional");
                return 0;
            }

            var teleportGatewayLogic = sourceGateway.GameLogic.GetAs<TeleportGateway>();
            if (teleportGatewayLogic == null) {
                MyLogger.Log($"TPCore: TeleportNearbyShips: TeleportGateway logic not found for source gateway {sourceGateway.EntityId}");
                return 0;
            }

            float sphereDiameter = teleportGatewayLogic.Settings.SphereDiameter;
            float sphereRadius = sphereDiameter / 2.0f;
            Vector3D sphereCenter = sourceGateway.GetPosition() + sourceGateway.WorldMatrix.Forward * sphereRadius;

            MyLogger.Log($"TPCore: TeleportNearbyShips: Sphere Center: {sphereCenter}, Sphere Diameter: {sphereDiameter}, Sphere Radius: {sphereRadius}");

            // Ensure we're using the correct radius when creating the bounding sphere
            BoundingSphereD sphere = new BoundingSphereD(sphereCenter, sphereRadius);
            List<IMyEntity> potentialEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            MyLogger.Log($"TPCore: TeleportNearbyShips: Potential entities found: {potentialEntities.Count}");

            int teleportedShipsCount = 0;

            foreach (var entity in potentialEntities) {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.IsStatic || grid.EntityId == sourceGateway.CubeGrid.EntityId) {
                    continue;
                }

                // Calculate distance from grid center to sphere center
                double distanceToSphereCenter = Vector3D.Distance(grid.WorldVolume.Center, sphereCenter);

                MyLogger.Log($"TPCore: TeleportNearbyShips: Grid {grid.DisplayName} (EntityId: {grid.EntityId}):");
                MyLogger.Log($"  Distance to sphere center: {distanceToSphereCenter}");
                MyLogger.Log($"  Sphere radius: {sphereRadius}");

                // Only teleport if the grid's center is within the sphere
                if (distanceToSphereCenter > sphereRadius) {
                    MyLogger.Log($"  Grid is outside the teleport sphere, skipping");
                    continue;
                }

                if (IsControlledByPlayer(grid)) {
                    MyLogger.Log($"  Grid is controlled by a player, skipping");
                    continue;
                }

                if (IsSubgridOrConnectedToLargerGrid(grid)) {
                    MyLogger.Log($"  Grid is a subgrid or connected to a larger grid, skipping");
                    continue;
                }

                if (HasLockedLandingGear(grid)) {
                    MyLogger.Log($"  Grid has locked landing gear, skipping");
                    continue;
                }

                if (!teleportGatewayLogic.Settings.AllowShips) {
                    MyLogger.Log($"  Ship teleportation is not allowed for this gateway, skipping");
                    continue;
                }

                // Teleport the ship and play effects at its position
                TeleportEntity(grid, sourceGateway, destGateway);
                PlayEffectsAtPosition(grid.GetPosition()); // Play particle and sound effects at the ship's position
                MyLogger.Log($"  Teleported grid {grid.DisplayName}");
                teleportedShipsCount++;
            }

            MyLogger.Log($"TPCore: TeleportNearbyShips: Total teleported ships: {teleportedShipsCount}");
            return teleportedShipsCount;
        }

        // Separate method to play effects at a specific position
        private static void PlayEffectsAtPosition(Vector3D position) {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("TeleportEntityEffect", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("TeleportEntitySound", position);
        }

        private static bool IsControlledByPlayer(IMyCubeGrid grid) {
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);

            foreach (var block in blocks) {
                var controller = block.FatBlock as IMyShipController;
                if (controller != null && controller.Pilot != null) {
                    return true;
                }
            }
            return false;
        }

        private static bool IsSubgridOrConnectedToLargerGrid(IMyCubeGrid grid) {
            // Get the group of grids the current grid is part of
            var group = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Physical);

            // Find the largest grid in the group
            IMyCubeGrid largestGrid = null;
            int largestBlockCount = 0;

            foreach (var g in group) {
                var myGrid = g as MyCubeGrid;
                if (myGrid != null && myGrid.BlocksCount > largestBlockCount) {
                    largestGrid = myGrid;
                    largestBlockCount = myGrid.BlocksCount;
                }
            }

            // Check if the current grid is the largest in the group
            return largestGrid != null && largestGrid.EntityId != grid.EntityId;
        }

        private static bool HasLockedLandingGear(IMyCubeGrid grid) {
            List<IMySlimBlock> landingGears = new List<IMySlimBlock>();
            grid.GetBlocks(landingGears, b => b.FatBlock is SpaceEngineers.Game.ModAPI.Ingame.IMyLandingGear);

            foreach (var gear in landingGears) {
                var landingGear = gear.FatBlock as SpaceEngineers.Game.ModAPI.Ingame.IMyLandingGear;
                if (landingGear != null && landingGear.IsLocked) {
                    return true;
                }
            }

            return false;
        }

        private static IMyPlayer GetPlayerById(long playerId) {
            var playerList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(playerList);
            return playerList.Find(p => p.IdentityId == playerId);
        }
    }
}
