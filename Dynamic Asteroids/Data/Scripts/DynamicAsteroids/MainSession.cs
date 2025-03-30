using DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities;
using RealGasGiants;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;


namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids {
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class MainSession : MySessionComponentBase {
        public static MainSession I;
        public Random Rand;
        private int seed;
        public AsteroidSpawner _spawner;
        private int _saveStateTimer;
        private int _networkMessageTimer;
        public RealGasGiantsApi RealGasGiantsApi { get; private set; }
        private int _testTimer = 0;
        private KeenRicochetMissileBSWorkaroundHandler _missileHandler;
        private Dictionary<long, Vector3D> _serverPositions = new Dictionary<long, Vector3D>();
        private Dictionary<long, Quaternion> _serverRotations = new Dictionary<long, Quaternion>();
        private Dictionary<long, AsteroidZone> _clientZones = new Dictionary<long, AsteroidZone>();


        public override void LoadData() {
            I = this;
            Log.Init();
            Log.Info("Log initialized in LoadData method.");
            AsteroidSettings.LoadSettings();
            seed = AsteroidSettings.Seed;
            Rand = new Random(seed);

            // Load RealGasGiants API
            RealGasGiantsApi = new RealGasGiantsApi();
            RealGasGiantsApi.Load();
            Log.Info("RealGasGiants API loaded in LoadData");

            // Initialize damage handler
            AsteroidDamageHandler damageHandler = new AsteroidDamageHandler();
            _missileHandler = new KeenRicochetMissileBSWorkaroundHandler(damageHandler);

            // Server-only initialization
            if (MyAPIGateway.Session.IsServer) {
                _spawner = new AsteroidSpawner(RealGasGiantsApi);
                _spawner.Init(seed);
            }

            // Register network handlers for both client and server
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(32000, OnSecureMessageReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(32001, OnSecureMessageReceived);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(32002, OnSettingsSyncReceived);
            MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        }

        public override void BeforeStart() {
           
            MyVisualScriptLogicProvider.PlayerConnected += OnPlayerConnected;
            Log.Info($"RealGasGiants API IsReady: {RealGasGiantsApi.IsReady}");
            //MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1000, DamageHandler);

        }

        //private void DamageHandler(object target, ref MyDamageInformation info)
        //{
        //    // Apply damage if the target is an AsteroidEntity
        //    var asteroid = target as AsteroidEntity;
        //    if (asteroid != null)
        //    {
        //        // Check if this asteroid is managed by the current session's spawner (important to avoid unintended damage)
        //        if (_spawner._asteroids.Contains(asteroid))
        //        {
        //            Log.Info($"Applying {info.Amount} damage to Asteroid ID {asteroid.EntityId}");
        //
        //            // Apply the damage by reducing integrity
        //            asteroid.ReduceIntegrity(info.Amount);
        //        }
        //    }
        //}

        protected override void UnloadData() {
            try {
                Log.Info("Unloading data in MainSession");
                if (_spawner != null) {
                    if (MyAPIGateway.Session.IsServer) {
                        // Remove SaveAsteroidState call
                        var asteroidsToRemove = _spawner.GetAsteroids().ToList();
                        foreach (var asteroid in asteroidsToRemove) {
                            try {
                                MyEntities.Remove(asteroid);
                                asteroid.Close();
                            }
                            catch (Exception removeEx) {
                                Log.Exception(removeEx, typeof(MainSession), "Error removing asteroid during unload");
                            }
                        }

                        _spawner.Close();
                        _spawner = null;
                    }
                }

                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(32000, OnSecureMessageReceived);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(32001, OnSecureMessageReceived);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(32002, OnSettingsSyncReceived);
                MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
                MyVisualScriptLogicProvider.PlayerConnected -= OnPlayerConnected;

                if (RealGasGiantsApi != null) {
                    RealGasGiantsApi.Unload();
                    RealGasGiantsApi = null;
                }

                _missileHandler.Unload();
                AsteroidSettings.SaveSettings();
                Log.Close();
                I = null;
            }
            catch (Exception ex) {
                MyLog.Default.WriteLine($"Error in UnloadData: {ex}");
                try {
                    Log.Exception(ex, typeof(MainSession), "Error in UnloadData");
                }
                catch {
                }
            }
        }

        private void OnMessageEntered(string messageText, ref bool sendToOthers) {
            IMyPlayer player = MyAPIGateway.Session.Player;
            if (player == null || !IsPlayerAdmin(player)) return;

            if (!messageText.StartsWith("/dynamicasteroids") && !messageText.StartsWith("/dn")) return;
            var args = messageText.Split(' ');
            if (args.Length <= 1) return;
            switch (args[1].ToLower()) {
                case "createspawnarea":
                    double radius;
                    if (args.Length == 3 && double.TryParse(args[2], out radius)) {
                        CreateSpawnArea(radius);
                        sendToOthers = false;
                    }

                    break;

                case "removespawnarea":
                    if (args.Length == 3) {
                        RemoveSpawnArea(args[2]);
                        sendToOthers = false;
                    }

                    break;
            }
        }

        private bool IsPlayerAdmin(IMyPlayer player) {
            return MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                   MyAPIGateway.Session.IsUserAdmin(player.SteamUserId);
        }

        private void CreateSpawnArea(double radius) {
            IMyPlayer player = MyAPIGateway.Session.Player;
            if (player == null) return;

            Vector3D position = player.GetPosition();
            var name = $"Area_{position.GetHashCode()}";

            BoundingBoxD boundingBox =
                new BoundingBoxD(position - new Vector3D(radius), position + new Vector3D(radius));
            MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(ref boundingBox);

            if (closestPlanet != null) {
                Log.Info(
                    $"Cannot create spawn area '{name}' at {position} with radius {radius}: Intersects with a planet.");
                MyAPIGateway.Utilities.ShowMessage("DynamicAsteroids",
                    $"Cannot create spawn area '{name}' at {position} with radius {radius}: Intersects with a planet.");
                return;
            }

            AsteroidSettings.AddSpawnableArea(name, position, radius);
            Log.Info($"Created spawn area '{name}' at {position} with radius {radius}");
            MyAPIGateway.Utilities.ShowMessage("DynamicAsteroids",
                $"Created spawn area '{name}' at {position} with radius {radius}");
        }

        private void RemoveSpawnArea(string name) {
            AsteroidSettings.RemoveSpawnableArea(name);
            Log.Info($"Removed spawn area '{name}'");
            MyAPIGateway.Utilities.ShowMessage("DynamicAsteroids", $"Removed spawn area '{name}'");
        }

        public override void UpdateAfterSimulation() {
            try {
                // Server-only updates
                if (MyAPIGateway.Session.IsServer) {
                    _spawner?.UpdateTick();

                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);

                    foreach (IMyPlayer player in players) {
                        Vector3D playerPosition = player.GetPosition();
                        AsteroidEntity nearestAsteroid = FindNearestAsteroid(playerPosition);

                        if (nearestAsteroid != null) {
                            //Log.Info($"Server: Nearest asteroid to player at {playerPosition}: Asteroid ID: {nearestAsteroid.EntityId}, Position: {nearestAsteroid.PositionComp.GetPosition()}");
                        }
                    }

                    if (_saveStateTimer > 0)
                        _saveStateTimer--;

                    if (_networkMessageTimer > 0)
                        _networkMessageTimer--;
                    else {
                        _spawner?.SendNetworkMessages();
                        _networkMessageTimer = AsteroidSettings.NetworkMessageInterval;
                        Log.Info("Server: Sending network messages to clients.");
                    }
                }

                // Middle mouse spawn (handle for both client and server)
                if (AsteroidSettings.EnableMiddleMouseAsteroidSpawn &&
                    MyAPIGateway.Input.IsNewKeyPressed(MyKeys.MiddleButton) &&
                    MyAPIGateway.Session?.Player != null) // Check Player is not null
                {
                    // Ensure Player.Character is not null before accessing Physics
                    IMyCharacter character = MyAPIGateway.Session.Player.Character;
                    if (character != null)
                    {
                        Vector3D position = character.GetPosition(); // Spawn near character
                        Vector3D velocity = character.Physics?.LinearVelocity ?? Vector3D.Zero;
                        AsteroidType type = DetermineAsteroidType(); // Assuming this exists

                        if (MyAPIGateway.Session.IsServer)
                        {
                            // Server creates the asteroid directly
                            var asteroid = AsteroidEntity.CreateAsteroid(position, Rand.Next(50), velocity, type);
                            if (asteroid != null && _spawner != null)
                            {
                                _spawner.AddAsteroid(asteroid);

                                // --- Targeted Initial Spawn Message ---
                                var message = new AsteroidNetworkMessage(
                                    asteroid.PositionComp.GetPosition(),
                                    asteroid.Properties.Diameter,
                                    asteroid.Physics.LinearVelocity,
                                    asteroid.Physics.AngularVelocity,
                                    asteroid.Type,
                                    false,
                                    asteroid.EntityId,
                                    false,
                                    true, // Is initial creation
                                    Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix)
                                );

                                byte[] messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);

                                if (messageBytes != null && messageBytes.Length > 0)
                                {
                                    List<IMyPlayer> players = new List<IMyPlayer>();
                                    MyAPIGateway.Players.GetPlayers(players);

                                    Vector3D spawnPosition = asteroid.PositionComp.GetPosition();

                                    foreach (IMyPlayer player in players)
                                    {
                                        if (player.SteamUserId == MyAPIGateway.Multiplayer.ServerId) continue; // Don't send to self if host

                                        Vector3D playerPosition = player.GetPosition();
                                        // Use the same relevance distance
                                        if (Vector3D.DistanceSquared(spawnPosition, playerPosition) <= AsteroidSpawner.ASTEROID_UPDATE_RELEVANCE_DISTANCE * AsteroidSpawner.ASTEROID_UPDATE_RELEVANCE_DISTANCE)
                                        {
                                             MyAPIGateway.Multiplayer.SendMessageTo(32000, messageBytes, player.SteamUserId);
                                        }
                                    }
                                    Log.Info($"Sent targeted initial spawn message for asteroid {asteroid.EntityId}");
                                }
                                // --- End Targeted Message ---
                            }
                        }
                        else // Client side
                        {
                            // Client sends request to server (this part remains the same)
                            var request = new AsteroidNetworkMessage(
                                position,
                                50, // default size
                                velocity,
                                Vector3D.Zero,
                                type,
                                false,
                                0, // server will assign real ID
                                false,
                                true, // Is initial creation
                                Quaternion.Identity
                            );

                            byte[] messageBytes = MyAPIGateway.Utilities.SerializeToBinary(request);
                            MyAPIGateway.Multiplayer.SendMessageToServer(32000, messageBytes);
                            Log.Info($"Client requested asteroid spawn at {position}");
                        }
                    } else {
                         Log.Warning("Cannot middle-mouse spawn: Player character not found.");
                    }
                }
                
                // Client-only updates (debug visualization)
                if (MyAPIGateway.Session?.Player?.Character != null) {
                    Vector3D characterPosition = MyAPIGateway.Session.Player.Character.PositionComp.GetPosition();
                    AsteroidEntity nearestAsteroid = FindNearestAsteroid(characterPosition);

                    if (nearestAsteroid != null && AsteroidSettings.EnableLogging) {
                        Vector3D angularVelocity = nearestAsteroid.Physics.AngularVelocity;
                        string rotationString =
                            $"({angularVelocity.X:F2}, {angularVelocity.Y:F2}, {angularVelocity.Z:F2})";
                        string message =
                            $"Nearest Asteroid: {nearestAsteroid.EntityId} ({nearestAsteroid.Type})\nRotation: {rotationString}";
                        MyAPIGateway.Utilities.ShowNotification(message, 1000 / 60);
                        nearestAsteroid.DrawDebugSphere();
                    }

                    // Log the number of active asteroids (for debugging purposes)
                    if (AsteroidSettings.EnableLogging) {
                        if (!MyAPIGateway.Session.IsServer) {
                            var entities = new HashSet<IMyEntity>();
                            MyAPIGateway.Entities.GetEntities(entities);
                            int localAsteroidCount = entities.Count(e => e is AsteroidEntity);

                            if (AsteroidSettings.EnableLogging) {
                                MyAPIGateway.Utilities.ShowNotification($"Client Asteroids: {localAsteroidCount}",
                                    1000 / 60);
                            }
                        }
                    }

                    // Update orphaned asteroids list periodically
                    if (++_orphanCheckTimer >= ORPHAN_CHECK_INTERVAL) {
                        _orphanCheckTimer = 0;
                        UpdateOrphanedAsteroidsList();
                    }
                }

                // Shared updates
                if (++_testTimer >= 240) {
                    _testTimer = 0;
                    TestNearestGasGiant();
                }

                Log.Update();
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error in UpdateAfterSimulation: ");
            }
        }

        public void DebugMissiles() {
            if (!AsteroidSettings.EnableLogging)
                return;

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            int missileCount = 0;
            foreach (var entity in entities) {
                IMyMissile missile = entity as IMyMissile;
                if (missile != null) {
                    var ammoDef = missile.AmmoDefinition as MyMissileAmmoDefinition;
                    if (ammoDef != null) {
                        MyAPIGateway.Utilities.ShowNotification(
                            $"Missile detected:\n" +
                            $"Type: {ammoDef.Id.SubtypeName}\n", 1000 / 60);
                    }
                }
            }

            if (missileCount == 0) {
                MyAPIGateway.Utilities.ShowNotification("No missiles found in world", 1000 / 60);
            }
        }


        private void TestNearestGasGiant() {
            if (RealGasGiantsApi == null || !RealGasGiantsApi.IsReady || MyAPIGateway.Session?.Player == null)
                return;

            if (!AsteroidSettings.EnableLogging)
                return;

            Vector3D playerPosition = MyAPIGateway.Session.Player.GetPosition();
            MyPlanet nearestGasGiant = FindNearestGasGiant(playerPosition);

            // Get the global ring influence at the player's position
            float ringInfluence = RealGasGiantsApi.GetRingInfluenceAtPositionGlobal(playerPosition);

            string message;

            if (nearestGasGiant != null) {
                var basicInfo = RealGasGiantsApi.GetGasGiantConfig_BasicInfo_Base(nearestGasGiant);
                if (basicInfo.Item1) // If operation was successful
                {
                    double distance = Vector3D.Distance(playerPosition, nearestGasGiant.PositionComp.GetPosition()) -
                                      basicInfo.Item2;
                    message = $"Nearest Gas Giant:\n" +
                              $"Distance: {distance:N0}m\n" +
                              $"Radius: {basicInfo.Item2:N0}m\n" +
                              $"Color: {basicInfo.Item3}\n" +
                              $"Skin: {basicInfo.Item4}\n" +
                              $"Day Length: {basicInfo.Item5:F2}s\n" +
                              $"Current Ring Influence: {ringInfluence:F3}";
                }
                else {
                    message = "Failed to get gas giant info";
                }
            }
            else {
                message = $"Current Ring Influence: {ringInfluence:F3}";
            }

            if (AsteroidSettings.EnableLogging)
                MyAPIGateway.Utilities.ShowNotification(message, 4000, "White");
        }

        private MyPlanet FindNearestGasGiant(Vector3D position) {
            const double searchRadius = 1000000000; // 1 million km in meters
            MyPlanet nearestGasGiant = null;
            double nearestDistance = double.MaxValue;

            // Get all gas giants within the larger search sphere
            var gasGiants = RealGasGiantsApi.GetAtmoGasGiantsAtPosition(position);

            foreach (MyPlanet gasGiant in gasGiants) {
                var basicInfo = RealGasGiantsApi.GetGasGiantConfig_BasicInfo_Base(gasGiant);
                if (!basicInfo.Item1) continue; // Skip if we couldn't get the info

                float gasGiantRadius = basicInfo.Item2;
                Vector3D gasGiantCenter = gasGiant.PositionComp.GetPosition();

                // Calculate distance from player to the surface of the gas giant
                double distance = Vector3D.Distance(position, gasGiantCenter) - gasGiantRadius;

                if (!(distance < nearestDistance) || !(distance <= searchRadius)) continue;
                nearestDistance = distance;
                nearestGasGiant = gasGiant;
            }

            if (nearestGasGiant != null) {
                //Log.Info($"Found nearest gas giant at distance: {nearestDistance:N0} meters");
            }
            else {
                //Log.Info("No gas giants found within 1 million km");
            }

            return nearestGasGiant;
        }

        private void OnSecureMessageReceived(ushort handlerId, byte[] message, ulong steamId, bool isFromServer) {
            try {
                if (message == null || message.Length == 0) {
                    Log.Info("Received empty or null message, skipping processing.");
                    return;
                }

                // Handle zone updates
                if (handlerId == 32001) {
                    ProcessZoneMessage(message);
                    return;
                }

                if (handlerId == 32000) {
                    try {
                        // Try to process as batch update first
                        var batchPacket = MyAPIGateway.Utilities.SerializeFromBinary<AsteroidBatchUpdatePacket>(message);
                        if (batchPacket != null) {
                            ProcessBatchMessage(batchPacket);
                            return;
                        }
                    }
                    catch {
                        // If batch deserialization fails, try single message
                        var singleMessage = MyAPIGateway.Utilities.SerializeFromBinary<AsteroidNetworkMessage>(message);
                        if (singleMessage != null) {
                            if (!MyAPIGateway.Session.IsServer) {
                                ProcessClientMessage(singleMessage);
                            }
                            else {
                                ProcessServerMessage(singleMessage, steamId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), $"Error processing received message");
            }
        }

        public class NetworkMessageVerification {
            public static bool ValidateMessage(AsteroidNetworkMessage message) {
                // In C# 6, we need to do explicit null check
                if (ReferenceEquals(message, null))
                    return false;

                if (message.EntityId == 0 && !message.IsInitialCreation)
                    return false;

                if (double.IsNaN(message.PosX) || double.IsNaN(message.PosY) || double.IsNaN(message.PosZ))
                    return false;

                return true;
            }
        }


        private void ProcessServerMessage(AsteroidNetworkMessage message, ulong steamId) {
            if (!NetworkMessageVerification.ValidateMessage(message)) {
                Log.Warning($"Server received invalid message from client {steamId}");
                return;
            }

            if (message.IsInitialCreation && message.EntityId == 0) {
                // Handle client request to create new asteroid
                var asteroid = AsteroidEntity.CreateAsteroid(
                    message.GetPosition(),
                    message.Size,
                    message.GetVelocity(),
                    message.GetType()
                );

                if (asteroid != null && _spawner != null) {
                    _spawner.AddAsteroid(asteroid);
                    var response = new AsteroidNetworkMessage(
                        asteroid.PositionComp.GetPosition(),
                        asteroid.Properties.Diameter,
                        asteroid.Physics.LinearVelocity,
                        asteroid.Physics.AngularVelocity,
                        asteroid.Type,
                        false,
                        asteroid.EntityId,
                        false,
                        true,
                        Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix)
                    );

                    byte[] responseBytes = MyAPIGateway.Utilities.SerializeToBinary(response);
                    MyAPIGateway.Multiplayer.SendMessageToOthers(32000, responseBytes);
                }
            }
        }

        private void RemoveAsteroidOnClient(long entityId) {
            Log.Info($"Client: Removing asteroid with ID {entityId}");

            AsteroidEntity asteroid = MyEntities.GetEntityById(entityId) as AsteroidEntity;
            if (asteroid != null) {
                try {
                    MyEntities.Remove(asteroid);
                    asteroid.Close();
                    Log.Info($"Client: Successfully removed asteroid {entityId}");
                }
                catch (Exception ex) {
                    Log.Exception(ex, typeof(MainSession), $"Error removing asteroid {entityId} on client");
                }
            }
            else {
                Log.Warning($"Client: Could not find asteroid with ID {entityId} to remove");
            }
        }

        private void CreateNewAsteroidOnClient(AsteroidNetworkMessage message) {
            try {
                // Don't generate random rotation, use exactly what the server sent
                var asteroid = AsteroidEntity.CreateAsteroid(
                    message.GetPosition(),
                    message.Size,
                    message.GetVelocity(),
                    message.GetType(),
                    message.GetRotation(), // Use server's rotation
                    message.EntityId
                );

                if (asteroid != null) {
                    if (asteroid.Physics != null) {
                        asteroid.Physics.LinearVelocity = message.GetVelocity();
                        asteroid.Physics.AngularVelocity = message.GetAngularVelocity();
                    }

                    Log.Info($"Client: Successfully created asteroid {message.EntityId} with server rotation");
                }
                else {
                    Log.Warning($"Client: Failed to create asteroid {message.EntityId}");
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error creating asteroid on client");
            }
        }

        private void ProcessClientMessage(AsteroidNetworkMessage message) {
            try {
                if (!NetworkMessageVerification.ValidateMessage(message)) {
                    Log.Warning($"Client received invalid message - ID: {message.EntityId}");
                    return;
                }

                if (message.IsRemoval) {
                    RemoveAsteroidOnClient(message.EntityId);
                    return;
                }

                AsteroidEntity existingAsteroid = MyEntities.GetEntityById(message.EntityId) as AsteroidEntity;
                if (message.IsInitialCreation) {
                    if (existingAsteroid != null) {
                        Log.Warning($"Received creation message for existing asteroid {message.EntityId}");
                        return;
                    }

                    // On initial creation, don't generate random rotation, use server's
                    CreateNewAsteroidOnClient(message);
                }
                else if (existingAsteroid != null) {
                    UpdateExistingAsteroidOnClient(existingAsteroid, message);
                }
                else {
                    Log.Warning($"Received update for non-existent asteroid {message.EntityId}");
                    CreateNewAsteroidOnClient(new AsteroidNetworkMessage(
                        message.GetPosition(),
                        message.Size,
                        message.GetVelocity(),
                        message.GetAngularVelocity(),
                        message.GetType(),
                        false,
                        message.EntityId,
                        false,
                        true,
                        message.GetRotation() // Use server's rotation
                    ));
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), $"Error processing client message");
            }
        }

        // Add a separate method for batch updates
        private void ProcessBatchMessage(AsteroidBatchUpdatePacket packet) {
            try {
                if (packet.Removals != null && packet.Removals.Count > 0) {
                    foreach (long entityId in packet.Removals) {
                        RemoveAsteroidOnClient(entityId);
                    }
                }

                // Process other batch data as needed...
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error processing batch update");
            }
        }
        private float GetQuaternionAngleDifference(Quaternion a, Quaternion b) {
            // Get the dot product between the quaternions
            float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

            // Clamp to handle floating point imprecision
            dot = MathHelper.Clamp(dot, -1f, 1f);

            // Calculate the angle
            return 2f * (float)Math.Acos(Math.Abs(dot));
        }


        private void UpdateServerPosition(long entityId, Vector3D position) {
            _serverPositions[entityId] = position;
        }

        private const double DRIFT_TOLERANCE = 0.1; // meters
        private Dictionary<long, DateTime> _lastPhysicsResetTime = new Dictionary<long, DateTime>();
        private const double PHYSICS_RESET_COOLDOWN = 1.0; // seconds

        private Dictionary<long, DateTime> _lastServerUpdateTime = new Dictionary<long, DateTime>();
        private const double POSITION_SNAP_THRESHOLD = 5.0; // meters - snap if client is further than this from server state
        private static readonly double ROTATION_SNAP_THRESHOLD_RADIANS = MathHelper.ToRadians(10);


        private void UpdateExistingAsteroidOnClient(AsteroidEntity asteroid, AsteroidNetworkMessage message)
        {
            try
            {
                Vector3D serverPosition = message.GetPosition();
                Vector3D serverVelocity = message.GetVelocity();
                Quaternion serverRotation = message.GetRotation();
                Vector3D serverAngularVelocity = message.GetAngularVelocity();
                DateTime messageTime = DateTime.UtcNow; // Ideally, server should include a timestamp

                // Store server state for debug drawing or other purposes if needed
                _serverPositions[asteroid.EntityId] = serverPosition;
                _serverRotations[asteroid.EntityId] = serverRotation;
                _lastServerUpdateTime[asteroid.EntityId] = messageTime;


                // --- Physics Update ---
                if (asteroid.Physics != null)
                {
                    // Always apply the latest server velocities
                    asteroid.Physics.LinearVelocity = serverVelocity;
                    asteroid.Physics.AngularVelocity = serverAngularVelocity;

                    // --- Position/Rotation Correction (Simplified Snap Logic) ---
                    Vector3D clientPosition = asteroid.PositionComp.GetPosition();
                    Quaternion clientRotation = Quaternion.CreateFromRotationMatrix(asteroid.WorldMatrix);

                    double positionDifference = Vector3D.Distance(clientPosition, serverPosition);
                    float rotationDifference = GetQuaternionAngleDifference(clientRotation, serverRotation);

                    // Only force a position/rotation update if the client has drifted significantly
                    // or if the server indicates the asteroid should now be stopped/moving when client thinks otherwise.
                    bool serverIsMoving = serverVelocity.LengthSquared() > 0.01 || serverAngularVelocity.LengthSquared() > 0.01;
                    bool clientIsMoving = asteroid.Physics.LinearVelocity.LengthSquared() > 0.01 || asteroid.Physics.AngularVelocity.LengthSquared() > 0.01;
                    bool significantDrift = positionDifference > POSITION_SNAP_THRESHOLD || rotationDifference > ROTATION_SNAP_THRESHOLD_RADIANS;
                    bool stateMismatch = serverIsMoving != clientIsMoving;


                    if (significantDrift || stateMismatch)
                    {
                        Log.Info($"Client asteroid {asteroid.EntityId} snapping:" +
                                 $"\n - Drift: Pos={positionDifference:F2}m, Rot={MathHelper.ToDegrees(rotationDifference):F1}°" +
                                 $"\n - State mismatch: {stateMismatch} (ServerMoving: {serverIsMoving}, ClientMoving: {clientIsMoving})");

                        // Apply server state directly
                        MatrixD newWorldMatrix = MatrixD.CreateFromQuaternion(serverRotation);
                        newWorldMatrix.Translation = serverPosition;
                        asteroid.WorldMatrix = newWorldMatrix; // This sets both position and rotation

                        // Re-apply velocities after matrix change, Clear speeds might be needed if physics engine fights back
                        // asteroid.Physics.ClearSpeed(); // Uncomment if needed
                        asteroid.Physics.LinearVelocity = serverVelocity;
                        asteroid.Physics.AngularVelocity = serverAngularVelocity;
                    }
                    // --- Optional: Interpolation Logic ---
                    // For smoother visuals, instead of snapping, you could interpolate.
                    // This requires storing the previous server state and interpolating
                    // between previous and current server state based on time.
                    // Example placeholder:
                    // float timeSinceLastUpdate = (float)(DateTime.UtcNow - _lastServerUpdateTime.GetValueOrDefault(asteroid.EntityId, DateTime.UtcNow)).TotalSeconds;
                    // float interpolationFactor = MathHelper.Clamp(timeSinceLastUpdate / NETWORK_UPDATE_INTERVAL_SECONDS, 0f, 1f);
                    // Vector3D interpolatedPosition = Vector3D.Lerp(previousServerPosition, serverPosition, interpolationFactor);
                    // Quaternion interpolatedRotation = Quaternion.Slerp(previousServerRotation, serverRotation, interpolationFactor);
                    // asteroid.WorldMatrix = MatrixD.CreateFromQuaternion(interpolatedRotation) * MatrixD.CreateTranslation(interpolatedPosition);
                }
                else
                {
                    // If physics is somehow null, just set the matrix directly
                    MatrixD newWorldMatrix = MatrixD.CreateFromQuaternion(serverRotation);
                    newWorldMatrix.Translation = serverPosition;
                    asteroid.WorldMatrix = newWorldMatrix;
                }

            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MainSession), $"Error updating client asteroid {asteroid.EntityId}");
            }
        }
        
        private AsteroidEntity FindNearestAsteroid(Vector3D characterPosition) {
            if (characterPosition == null)
                return null;

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            AsteroidEntity nearestAsteroid = null;
            double minDistance = double.MaxValue;

            foreach (var entity in entities) {
                AsteroidEntity asteroid = entity as AsteroidEntity;
                if (asteroid != null) {
                    double distance = Vector3D.DistanceSquared(characterPosition, asteroid.PositionComp.GetPosition());
                    if (distance < minDistance) {
                        minDistance = distance;
                        nearestAsteroid = asteroid;
                    }
                }
            }

            return nearestAsteroid;
        }

        private AsteroidType DetermineAsteroidType() {
            int randValue = Rand.Next(0, 2);
            return (AsteroidType)randValue;
        }

        public void UpdateClientZones(Dictionary<long, AsteroidZone> serverZones) {
            if (!MyAPIGateway.Utilities.IsDedicated) {
                _clientZones.Clear();
                foreach (var kvp in serverZones) {
                    _clientZones[kvp.Key] = kvp.Value;
                }
            }
        }

        private void ProcessZoneMessage(byte[] message) {
            try {
                var zonePacket = MyAPIGateway.Utilities.SerializeFromBinary<ZoneUpdatePacket>(message);
                if (zonePacket?.Zones == null)
                    return;

                // In singleplayer, we should get zones directly from the spawner
                if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated) {
                    if (_spawner != null) {
                        UpdateClientZones(_spawner.playerZones.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                    }
                    return;
                }

                var previousZones = new Dictionary<long, AsteroidZone>(_clientZones);
                _clientZones.Clear();

                foreach (var zoneData in zonePacket.Zones) {
                    var newZone = new AsteroidZone(zoneData.Center, zoneData.Radius) {
                        IsMarkedForRemoval = !zoneData.IsActive,
                        IsMerged = zoneData.IsMerged,
                        CurrentSpeed = zoneData.CurrentSpeed
                    };
                    _clientZones[zoneData.PlayerId] = newZone;
                    previousZones.Remove(zoneData.PlayerId);
                }

                // Handle removed zones
                foreach (var removedZone in previousZones.Values) {
                    _lastRemovedZones.Enqueue(removedZone);
                    while (_lastRemovedZones.Count > 5)
                        _lastRemovedZones.Dequeue();

                    if (!MyAPIGateway.Session.IsServer) {
                        var entities = new HashSet<IMyEntity>();
                        MyAPIGateway.Entities.GetEntities(entities);

                        foreach (var entity in entities) {
                            var asteroid = entity as AsteroidEntity;
                            if (asteroid != null && removedZone.IsPointInZone(asteroid.PositionComp.GetPosition())) {
                                RemoveAsteroidOnClient(asteroid.EntityId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error processing zone packet");
            }
        }

        private void OnSettingsSyncReceived(ushort handlerId, byte[] data, ulong steamId, bool isFromServer) {
            if (!isFromServer) return;

            try {
                var settings = MyAPIGateway.Utilities.SerializeFromBinary<SettingsSyncMessage>(data);
                if (settings != null) {
                    AsteroidSettings.EnableLogging = settings.EnableLogging;
                    Log.Info($"Received settings from server. Debug logging: {settings.EnableLogging}");
                }
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error processing settings sync");
            }
        }

        private void OnPlayerConnected(long identityId) {
            MyAPIGateway.Utilities.InvokeOnGameThread(() => {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                var player = players.FirstOrDefault(p => p.IdentityId == identityId);

                if (player != null) {
                    Log.Info($"Syncing settings to player {player.DisplayName}");
                    SendSettingsToClient(player.SteamUserId);
                }
            }, "SyncSettings");
        }

        private void SendSettingsToClient(ulong steamId) {
            try {
                var settings = new SettingsSyncMessage {
                    EnableLogging = AsteroidSettings.EnableLogging
                };

                byte[] data = MyAPIGateway.Utilities.SerializeToBinary(settings);
                MyAPIGateway.Multiplayer.SendMessageTo(32002, data, steamId);
                Log.Info($"Sent settings to client {steamId}");
            }
            catch (Exception ex) {
                Log.Exception(ex, typeof(MainSession), "Error sending settings to client");
            }
        }
    }
}