using DynamicAsteroids.AsteroidEntities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using System.Linq;
using System;
using DynamicAsteroids;
using Invalid.DynamicRoids;
using Sandbox.Game.Entities;

public class AsteroidZone
{
    public Vector3D Center { get; set; }
    public double Radius { get; set; }
    public int AsteroidCount { get; set; }

    public AsteroidZone(Vector3D center, double radius)
    {
        Center = center;
        Radius = radius;
        AsteroidCount = 0;
    }

    public bool IsPointInZone(Vector3D point)
    {
        return Vector3D.DistanceSquared(Center, point) <= Radius * Radius;
    }
}

public class AsteroidSpawner
{
    public List<AsteroidEntity> _asteroids;
    private bool _canSpawnAsteroids = false;
    private DateTime _worldLoadTime;
    private Random rand;
    private List<AsteroidState> _despawnedAsteroids = new List<AsteroidState>();
    private List<AsteroidNetworkMessage> _networkMessages = new List<AsteroidNetworkMessage>();
    private Dictionary<long, AsteroidZone> playerZones = new Dictionary<long, AsteroidZone>();
    private Dictionary<long, PlayerMovementData> playerMovementData = new Dictionary<long, PlayerMovementData>();
    private Queue<AsteroidEntity> gravityCheckQueue = new Queue<AsteroidEntity>();
    private const int GravityChecksPerTick = 1;

    private Queue<AsteroidEntity> _updateQueue = new Queue<AsteroidEntity>();
    private const int UpdatesPerTick = 50; // Adjust this number based on performance needs

    private class PlayerMovementData
    {
        public Vector3D LastPosition { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public double Speed { get; set; }
    }

    private int _spawnIntervalTimer = 0;
    private int _updateIntervalTimer = 0;
    private int _logUpdateTimer = 0;
    private int totalSpawnAttempts = 0;

    public void Init(int seed)
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        Log.Info("Initializing AsteroidSpawner");
        _asteroids = new List<AsteroidEntity>(AsteroidSettings.MaxAsteroidCount == -1 ? 0 : AsteroidSettings.MaxAsteroidCount);
        _worldLoadTime = DateTime.UtcNow;
        rand = new Random(seed);
        AsteroidSettings.Seed = seed;

        // Add all asteroids to the update queue
        foreach (var asteroid in _asteroids)
        {
            _updateQueue.Enqueue(asteroid);
        }
    }

    public void SaveAsteroidState()
    {
        if (!MyAPIGateway.Session.IsServer || !AsteroidSettings.EnablePersistence)
            return;

        var asteroidStates = _asteroids.Select(asteroid => new AsteroidState
        {
            Position = asteroid.PositionComp.GetPosition(),
            Size = asteroid.Size,
            Type = asteroid.Type,
            EntityId = asteroid.EntityId
        }).ToList();

        asteroidStates.AddRange(_despawnedAsteroids);

        var stateBytes = MyAPIGateway.Utilities.SerializeToBinary(asteroidStates);
        using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
        {
            writer.Write(stateBytes, 0, stateBytes.Length);
        }

        // Ensure the update queue is saved as well
        _updateQueue.Clear();
        foreach (var asteroid in _asteroids)
        {
            _updateQueue.Enqueue(asteroid);
        }
    }

    public void LoadAsteroidState()
    {
        if (!MyAPIGateway.Session.IsServer || !AsteroidSettings.EnablePersistence)
            return;

        _asteroids.Clear();

        if (MyAPIGateway.Utilities.FileExistsInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
        {
            byte[] stateBytes;
            using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage("asteroid_states.dat", typeof(AsteroidSpawner)))
            {
                stateBytes = reader.ReadBytes((int)reader.BaseStream.Length);
            }

            var asteroidStates = MyAPIGateway.Utilities.SerializeFromBinary<List<AsteroidState>>(stateBytes);

            foreach (var state in asteroidStates)
            {
                if (_asteroids.Any(a => a.EntityId == state.EntityId))
                {
                    Log.Info($"Skipping duplicate asteroid with ID {state.EntityId}");
                    continue;
                }

                var asteroid = AsteroidEntity.CreateAsteroid(state.Position, state.Size, Vector3D.Zero, state.Type);
                asteroid.EntityId = state.EntityId;
                _asteroids.Add(asteroid);
                MyEntities.Add(asteroid);

                // Add to gravity check queue
                gravityCheckQueue.Enqueue(asteroid);

                // Add to update queue
                _updateQueue.Enqueue(asteroid);
            }
        }
    }

    private void LoadAsteroidsInRange(Vector3D playerPosition, AsteroidZone zone)
    {
        int skippedCount = 0;
        int respawnedCount = 0;
        List<Vector3D> skippedPositions = new List<Vector3D>();
        List<Vector3D> respawnedPositions = new List<Vector3D>();

        foreach (var state in _despawnedAsteroids.ToArray())
        {
            if (zone.IsPointInZone(state.Position))
            {
                bool tooClose = _asteroids.Any(a => Vector3D.DistanceSquared(a.PositionComp.GetPosition(), state.Position) < AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer);

                if (tooClose)
                {
                    skippedCount++;
                    skippedPositions.Add(state.Position);
                    continue;
                }

                respawnedCount++;
                respawnedPositions.Add(state.Position);

                var asteroid = AsteroidEntity.CreateAsteroid(state.Position, state.Size, Vector3D.Zero, state.Type);
                asteroid.EntityId = state.EntityId;
                _asteroids.Add(asteroid);

                var message = new AsteroidNetworkMessage(state.Position, state.Size, Vector3D.Zero, Vector3D.Zero, state.Type, false, asteroid.EntityId, false, true, Quaternion.Identity);
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);

                _despawnedAsteroids.Remove(state);

                // Add to gravity check queue
                gravityCheckQueue.Enqueue(asteroid);
            }
        }

        if (skippedCount > 0)
        {
            Log.Info($"Skipped respawn of {skippedCount} asteroids due to proximity to other asteroids or duplicate ID.");
        }

        if (respawnedCount > 0)
        {
            Log.Info($"Respawned {respawnedCount} asteroids at positions: {string.Join(", ", respawnedPositions.Select(p => p.ToString()))}");
        }
    }

    public void Close()
    {
        if (!MyAPIGateway.Session.IsServer)
            return;

        SaveAsteroidState();
        Log.Info("Closing AsteroidSpawner");
        _asteroids?.Clear();
    }

    public void AssignZonesToPlayers()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        Dictionary<long, AsteroidZone> updatedZones = new Dictionary<long, AsteroidZone>();

        foreach (var player in players)
        {
            Vector3D playerPosition = player.GetPosition();

            PlayerMovementData data;
            if (playerMovementData.TryGetValue(player.IdentityId, out data))
            {
                if (AsteroidSettings.DisableZoneWhileMovingFast && data.Speed > AsteroidSettings.ZoneSpeedThreshold)
                {
                    Log.Info($"Skipping zone creation for player {player.DisplayName} due to high speed: {data.Speed} m/s.");
                    continue;
                }
            }

            AsteroidZone existingZone;
            if (playerZones.TryGetValue(player.IdentityId, out existingZone))
            {
                if (existingZone.IsPointInZone(playerPosition))
                {
                    updatedZones[player.IdentityId] = existingZone;
                }
                else
                {
                    AsteroidZone newZone = new AsteroidZone(playerPosition, AsteroidSettings.ZoneRadius);
                    updatedZones[player.IdentityId] = newZone;
                }
            }
            else
            {
                AsteroidZone newZone = new AsteroidZone(playerPosition, AsteroidSettings.ZoneRadius);
                updatedZones[player.IdentityId] = newZone;
            }
        }

        playerZones = updatedZones;
    }

    public void MergeZones()
    {
        List<AsteroidZone> mergedZones = new List<AsteroidZone>();

        foreach (var zone in playerZones.Values)
        {
            bool merged = false;

            foreach (var mergedZone in mergedZones)
            {
                double distance = Vector3D.Distance(zone.Center, mergedZone.Center);
                double combinedRadius = zone.Radius + mergedZone.Radius;

                if (distance <= combinedRadius)
                {
                    Vector3D newCenter = (zone.Center + mergedZone.Center) / 2;
                    double newRadius = Math.Max(zone.Radius, mergedZone.Radius) + distance / 2;
                    mergedZone.Center = newCenter;
                    mergedZone.Radius = newRadius;
                    mergedZone.AsteroidCount += zone.AsteroidCount;
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                mergedZones.Add(new AsteroidZone(zone.Center, zone.Radius) { AsteroidCount = zone.AsteroidCount });
            }
        }

        playerZones.Clear();
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var mergedZone in mergedZones)
        {
            foreach (var player in players)
            {
                if (mergedZone.IsPointInZone(player.GetPosition()))
                {
                    playerZones[player.IdentityId] = mergedZone;
                    break;
                }
            }
        }
    }

    public void UpdateZones()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        Dictionary<long, AsteroidZone> updatedZones = new Dictionary<long, AsteroidZone>();

        foreach (var player in players)
        {
            Vector3D playerPosition = player.GetPosition();

            PlayerMovementData data;
            if (playerMovementData.TryGetValue(player.IdentityId, out data))
            {
                if (AsteroidSettings.DisableZoneWhileMovingFast && data.Speed > AsteroidSettings.ZoneSpeedThreshold)
                {
                    Log.Info($"Skipping zone update for player {player.DisplayName} due to high speed: {data.Speed} m/s.");
                    continue;
                }
            }

            bool playerInZone = false;
            foreach (var zone in playerZones.Values)
            {
                if (zone.IsPointInZone(playerPosition))
                {
                    playerInZone = true;
                    break;
                }
            }

            if (!playerInZone)
            {
                AsteroidZone newZone = new AsteroidZone(playerPosition, AsteroidSettings.ZoneRadius);
                updatedZones[player.IdentityId] = newZone;
            }
        }

        foreach (var kvp in playerZones)
        {
            if (players.Any(p => kvp.Value.IsPointInZone(p.GetPosition())))
            {
                updatedZones[kvp.Key] = kvp.Value;
            }
        }

        playerZones = updatedZones;
    }

    public void UpdateTick()
    {
        if (!MyAPIGateway.Session.IsServer) return;

        AssignZonesToPlayers();
        MergeZones();
        UpdateZones();

        try
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            if (_updateIntervalTimer > 0)
            {
                _updateIntervalTimer--;
            }
            else
            {
                UpdateAsteroids(playerZones.Values.ToList());
                ProcessAsteroidUpdates();
                _updateIntervalTimer = AsteroidSettings.UpdateInterval;
            }

            if (_spawnIntervalTimer > 0)
            {
                _spawnIntervalTimer--;
            }
            else
            {
                SpawnAsteroids(playerZones.Values.ToList());
                _spawnIntervalTimer = AsteroidSettings.SpawnInterval;
            }

            if (_logUpdateTimer > 0)
            {
                _logUpdateTimer--;
            }
            else
            {
                Log.Info($"All zones spawn attempt complete. Total spawn attempts: {totalSpawnAttempts}, New total asteroid count: {_asteroids.Count}");
                _logUpdateTimer = 5; // Log every 5 updates, adjust as necessary
            }

            foreach (var player in players)
            {
                Vector3D playerPosition = player.GetPosition();
                AsteroidZone zone;
                if (playerZones.TryGetValue(player.IdentityId, out zone))
                {
                    LoadAsteroidsInRange(playerPosition, zone);
                }
            }

            ProcessGravityCheckQueue();

            if (AsteroidSettings.EnableLogging)
                MyAPIGateway.Utilities.ShowNotification($"Active Asteroids: {_asteroids.Count}", 1000 / 60);
        }
        catch (Exception ex)
        {
            Log.Exception(ex, typeof(AsteroidSpawner));
        }
    }

    private void UpdateAsteroids(List<AsteroidZone> zones)
    {
        Log.Info($"Updating asteroids. Total asteroids: {_asteroids.Count}, Total zones: {zones.Count}");
        int removedCount = 0;

        foreach (var asteroid in _asteroids.ToArray())
        {
            bool inAnyZone = false;
            AsteroidZone currentZone = null;

            foreach (var zone in zones)
            {
                if (zone.IsPointInZone(asteroid.PositionComp.GetPosition()))
                {
                    inAnyZone = true;
                    currentZone = zone;
                    break;
                }
            }

            if (!inAnyZone)
            {
                Log.Info($"Removing asteroid at {asteroid.PositionComp.GetPosition()} due to distance from all player zones");
                RemoveAsteroid(asteroid);
                removedCount++;
            }
            else if (currentZone != null)
            {
                foreach (var zone in zones)
                {
                    if (zone != currentZone && zone.IsPointInZone(asteroid.PositionComp.GetPosition()))
                    {
                        zone.AsteroidCount--;
                    }
                }
                currentZone.AsteroidCount++;
            }

            // Add to gravity check queue
            gravityCheckQueue.Enqueue(asteroid);
        }

        Log.Info($"Update complete. Removed asteroids: {removedCount}, Remaining asteroids: {_asteroids.Count}");
        foreach (var zone in zones)
        {
            Log.Info($"Zone center: {zone.Center}, Radius: {zone.Radius}, Asteroid count: {zone.AsteroidCount}");
        }
    }

    private void ProcessAsteroidUpdates()
    {
        int updatesProcessed = 0;

        while (updatesProcessed < UpdatesPerTick && _updateQueue.Count > 0)
        {
            var asteroid = _updateQueue.Dequeue();

            // Perform the update logic for the asteroid here
            UpdateAsteroid(asteroid);

            // Re-enqueue the asteroid for future updates
            _updateQueue.Enqueue(asteroid);

            updatesProcessed++;
        }
    }

    private void UpdateAsteroid(AsteroidEntity asteroid)
    {
        // Implement the actual update logic for an individual asteroid here
        Vector3D currentPosition = asteroid.PositionComp.GetPosition();
        bool inAnyZone = false;
        AsteroidZone currentZone = null;

        foreach (var zone in playerZones.Values)
        {
            if (zone.IsPointInZone(currentPosition))
            {
                inAnyZone = true;
                currentZone = zone;
                break;
            }
        }

        if (!inAnyZone)
        {
            Log.Info($"Removing asteroid at {currentPosition} due to being out of any player zone");
            RemoveAsteroid(asteroid);
        }
        else if (currentZone != null)
        {
            currentZone.AsteroidCount++;
        }
    }

    private void ProcessGravityCheckQueue()
    {
        for (int i = 0; i < GravityChecksPerTick && gravityCheckQueue.Count > 0; i++)
        {
            var asteroid = gravityCheckQueue.Dequeue();
            if (IsInNaturalGravity(asteroid.PositionComp.GetPosition()))
            {
                RemoveAsteroid(asteroid);
            }
            else
            {
                // Re-enqueue if still valid
                gravityCheckQueue.Enqueue(asteroid);
            }
        }
    }

    public void SpawnAsteroids(List<AsteroidZone> zones)
    {
        if (AsteroidSettings.MaxAsteroidCount == 0)
        {
            Log.Info("Asteroid spawning is disabled.");
            return;
        }

        int totalAsteroidsSpawned = 0;
        int totalZoneSpawnAttempts = 0;
        List<Vector3D> skippedPositions = new List<Vector3D>();
        List<Vector3D> spawnedPositions = new List<Vector3D>();

        UpdatePlayerMovementData();

        foreach (var zone in zones)
        {
            int asteroidsSpawned = 0;
            int zoneSpawnAttempts = 0;

            if (zone.AsteroidCount >= AsteroidSettings.MaxAsteroidsPerZone)
            {
                Log.Info($"Zone at {zone.Center} has reached its maximum asteroid count ({AsteroidSettings.MaxAsteroidsPerZone}). Skipping further spawning in this zone.");
                continue;
            }

            bool skipSpawning = false;
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);

            foreach (var player in players)
            {
                Vector3D playerPosition = player.GetPosition();
                if (zone.IsPointInZone(playerPosition))
                {
                    PlayerMovementData data;
                    if (playerMovementData.TryGetValue(player.IdentityId, out data))
                    {
                        if (data.Speed > 1000)
                        {
                            Log.Info($"Skipping asteroid spawning for player {player.DisplayName} due to high speed: {data.Speed} m/s.");
                            skipSpawning = true;
                            break;
                        }
                    }
                }
            }

            if (skipSpawning)
            {
                continue;
            }

            while (zone.AsteroidCount < AsteroidSettings.MaxAsteroidsPerZone && asteroidsSpawned < 10 &&
                   zoneSpawnAttempts < AsteroidSettings.MaxZoneAttempts && totalSpawnAttempts < AsteroidSettings.MaxTotalAttempts)
            {
                Vector3D newPosition;
                do
                {
                    newPosition = zone.Center + RandVector() * AsteroidSettings.ZoneRadius;
                    zoneSpawnAttempts++;
                    totalSpawnAttempts++;
                    Log.Info($"Attempting to spawn asteroid at {newPosition} (attempt {totalSpawnAttempts})");
                } while (!IsValidSpawnPosition(newPosition, zones) && zoneSpawnAttempts < AsteroidSettings.MaxZoneAttempts &&
                         totalSpawnAttempts < AsteroidSettings.MaxTotalAttempts);

                if (zoneSpawnAttempts >= AsteroidSettings.MaxZoneAttempts || totalSpawnAttempts >= AsteroidSettings.MaxTotalAttempts)
                    break;

                Vector3D newVelocity;
                if (!AsteroidSettings.CanSpawnAsteroidAtPoint(newPosition, out newVelocity))
                {
                    Log.Info($"Cannot spawn asteroid at {newPosition}, skipping.");
                    continue;
                }

                if (IsNearVanillaAsteroid(newPosition))
                {
                    Log.Info($"Position {newPosition} is near a vanilla asteroid, skipping.");
                    skippedPositions.Add(newPosition);
                    continue;
                }

                if (AsteroidSettings.MaxAsteroidCount != -1 && _asteroids.Count >= AsteroidSettings.MaxAsteroidCount)
                {
                    Log.Warning($"Maximum asteroid count of {AsteroidSettings.MaxAsteroidCount} reached. No more asteroids will be spawned until existing ones are removed.");
                    return;
                }

                if (zone.AsteroidCount >= AsteroidSettings.MaxAsteroidsPerZone)
                {
                    Log.Info($"Zone at {zone.Center} has reached its maximum asteroid count ({AsteroidSettings.MaxAsteroidsPerZone}). Skipping further spawning in this zone.");
                    break;
                }

                AsteroidType type = AsteroidSettings.GetAsteroidType(newPosition);
                float size = AsteroidSettings.GetAsteroidSize(newPosition);
                Quaternion rotation = Quaternion.CreateFromYawPitchRoll((float)rand.NextDouble() * MathHelper.TwoPi,
                                                                        (float)rand.NextDouble() * MathHelper.TwoPi,
                                                                        (float)rand.NextDouble() * MathHelper.TwoPi);

                var asteroid = AsteroidEntity.CreateAsteroid(newPosition, size, newVelocity, type, rotation);

                if (asteroid != null)
                {
                    _asteroids.Add(asteroid);
                    zone.AsteroidCount++;
                    spawnedPositions.Add(newPosition);

                    var message = new AsteroidNetworkMessage(newPosition, size, newVelocity, Vector3D.Zero, type, false, asteroid.EntityId, false, true, rotation);
                    _networkMessages.Add(message);
                    asteroidsSpawned++;

                    // Add to gravity check queue
                    gravityCheckQueue.Enqueue(asteroid);
                }
            }

            totalAsteroidsSpawned += asteroidsSpawned;
            totalZoneSpawnAttempts += zoneSpawnAttempts;
        }

        if (AsteroidSettings.EnableLogging)
        {
            Log.Info($"All zones spawn attempt complete. Total spawn attempts: {totalSpawnAttempts}, New total asteroid count: {_asteroids.Count}");
            Log.Info($"Total asteroids spawned: {totalAsteroidsSpawned}, Total zone spawn attempts: {totalZoneSpawnAttempts}");
            if (skippedPositions.Count > 0)
            {
                Log.Info($"Skipped spawning asteroids due to proximity to vanilla asteroids. Positions: {string.Join(", ", skippedPositions.Select(p => p.ToString()))}");
            }
            if (spawnedPositions.Count > 0)
            {
                Log.Info($"Spawned asteroids at positions: {string.Join(", ", spawnedPositions.Select(p => p.ToString()))}");
            }
        }
    }

    private void UpdatePlayerMovementData()
    {
        List<IMyPlayer> players = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(players);

        foreach (var player in players)
        {
            Vector3D currentPosition = player.GetPosition();
            DateTime currentTime = DateTime.UtcNow;

            PlayerMovementData data;
            if (playerMovementData.TryGetValue(player.IdentityId, out data))
            {
                double distance = Vector3D.Distance(currentPosition, data.LastPosition);
                double timeElapsed = (currentTime - data.LastUpdateTime).TotalSeconds;

                double speed = distance / timeElapsed;
                data.Speed = speed;

                playerMovementData[player.IdentityId].LastPosition = currentPosition;
                playerMovementData[player.IdentityId].LastUpdateTime = currentTime;
            }
            else
            {
                playerMovementData[player.IdentityId] = new PlayerMovementData
                {
                    LastPosition = currentPosition,
                    LastUpdateTime = currentTime,
                    Speed = 0
                };
            }
        }
    }

    private bool IsValidSpawnPosition(Vector3D position, List<AsteroidZone> zones)
    {
        if (AsteroidSettings.IgnorePlanets && IsInNaturalGravity(position))
        {
            return false;
        }

        foreach (var zone in zones)
        {
            if (zone.IsPointInZone(position) &&
                Vector3D.DistanceSquared(position, zone.Center) >= AsteroidSettings.MinDistanceFromPlayer * AsteroidSettings.MinDistanceFromPlayer)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsInNaturalGravity(Vector3D position)
    {
        float naturalGravityInterference;
        Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(position, out naturalGravityInterference);
        return gravity.LengthSquared() > 0;
    }

    public void SendNetworkMessages()
    {
        if (_networkMessages.Count == 0) return;
        try
        {
            Log.Info($"Server: Preparing to send {_networkMessages.Count} network messages");

            foreach (var message in _networkMessages)
            {
                var messageBytes = MyAPIGateway.Utilities.SerializeToBinary(message);
                Log.Info($"Server: Serialized message size: {messageBytes.Length} bytes");
                MyAPIGateway.Multiplayer.SendMessageToOthers(32000, messageBytes);
                Log.Info($"Server: Sent message for asteroid ID {message.EntityId}");
            }

            _networkMessages.Clear();
        }
        catch (Exception ex)
        {
            Log.Exception(ex, typeof(AsteroidSpawner), "Failed to send network messages");
        }
    }

    private void RemoveAsteroid(AsteroidEntity asteroid)
    {
        if (_asteroids.Any(a => a.EntityId == asteroid.EntityId))
        {
            _despawnedAsteroids.Add(new AsteroidState
            {
                Position = asteroid.PositionComp.GetPosition(),
                Size = asteroid.Size,
                Type = asteroid.Type,
                EntityId = asteroid.EntityId
            });

            var removalMessage = new AsteroidNetworkMessage(asteroid.PositionComp.GetPosition(), asteroid.Size, Vector3D.Zero, Vector3D.Zero, asteroid.Type, false, asteroid.EntityId, true, false, Quaternion.Identity);
            var removalMessageBytes = MyAPIGateway.Utilities.SerializeToBinary(removalMessage);
            MyAPIGateway.Multiplayer.SendMessageToOthers(32000, removalMessageBytes);

            _asteroids.Remove(asteroid);
            MyEntities.Remove(asteroid);
            asteroid.Close();
            Log.Info($"Server: Removed asteroid with ID {asteroid.EntityId} from _asteroids list and MyEntities");
        }
    }

    private bool IsNearVanillaAsteroid(Vector3D position)
    {
        List<IMyVoxelBase> voxelMaps = new List<IMyVoxelBase>();
        MyAPIGateway.Session.VoxelMaps.GetInstances(voxelMaps, v => v is IMyVoxelMap && !v.StorageName.StartsWith("mod_"));

        foreach (var voxelMap in voxelMaps)
        {
            if (Vector3D.DistanceSquared(position, voxelMap.GetPosition()) < AsteroidSettings.MinDistanceFromVanillaAsteroids * AsteroidSettings.MinDistanceFromVanillaAsteroids)
            {
                Log.Info($"Position {position} is near vanilla asteroid {voxelMap.StorageName}");
                return true;
            }
        }

        return false;
    }

    private Vector3D RandVector()
    {
        var theta = rand.NextDouble() * 2.0 * Math.PI;
        var phi = Math.Acos(2.0 * rand.NextDouble() - 1.0);
        var sinPhi = Math.Sin(phi);
        return Math.Pow(rand.NextDouble(), 1 / 3d) * new Vector3D(sinPhi * Math.Cos(theta), sinPhi * Math.Sin(theta), Math.Cos(phi));
    }
}
