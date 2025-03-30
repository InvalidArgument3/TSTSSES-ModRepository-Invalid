using RealGasGiants;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRageMath;


namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities
{
    public static class AsteroidSettings
    {
        public static bool EnableLogging = false;
        public static bool EnableMiddleMouseAsteroidSpawn = false;
        public static bool EnableVanillaAsteroidSpawnLatching = false;
        public static bool EnableGasGiantRingSpawning = false;
        public static float MinimumRingInfluenceForSpawn = 0.1f;
        public static double RingAsteroidVelocityBase = 50.0; // Adjust as needed
        public static float MaxRingAsteroidDensityMultiplier = 1f; // Adjust this value as needed
        public static double VanillaAsteroidSpawnLatchingRadius = 10000;
        public static bool DisableZoneWhileMovingFast = true;
        public static double ZoneSpeedThreshold = 2000.0;
        public static int SaveStateInterval = 600;
        public static int NetworkMessageInterval = 60;
        public static int SpawnInterval = 6;
        public static int UpdateInterval = 60;
        public static int NetworkUpdateInterval = 6; //this is the network metal pipe noise
        public static int MaxAsteroidCount = 20000;
        public static int MaxAsteroidsPerZone = 100;
        public static int MaxTotalAttempts = 100;
        public static int MaxZoneAttempts = 50;
        public static double ZoneRadius = 10000.0;
        public static int AsteroidVelocityBase = 0;
        public static double VelocityVariability = 0;
        public static double AngularVelocityVariability = 0; //TODO: this gets corrected during runtime and doesnt actually work
        public static double MinDistanceFromVanillaAsteroids = 1000;
        public static double MinDistanceFromPlayer = 3000; //TODO: this might only be checking form the center of a zone. lets not make zones move with players yet
        public static int Seed = 69420;
        public static bool IgnorePlanets = true;
        public static double IceWeight = 97.4;
        public static double StoneWeight = 1.0;
        public static double IronWeight = 0.4;
        public static double NickelWeight = 0.2;
        public static double CobaltWeight = 0.2;   //TODO: spawning weights might be way to proportionally out of whack for videogames gamplay
        public static double MagnesiumWeight = 0.2;
        public static double SiliconWeight = 0.2;
        public static double SilverWeight = 0.1;
        public static double GoldWeight = 0.1;
        public static double PlatinumWeight = 0.1;
        public static double UraniniteWeight = 0.1;
        public static float MinAsteroidSize = 50f;   //TODO: depreciate min and max since we have reasonable volumes (sphere)
        public static float MaxAsteroidSize = 250f;
        public static float InstabilityPerMass = 0.1f;
        public static float InstabilityThresholdPercent = 0.8f;
        public static float InstabilityDecayRate = 0.1f;
        public static float InstabilityFromDamage = 1.0f;
        public static float KgLossPerDamage = 0.01f; // 1 damage = 1 kg lost
        public static int MaxPlayersPerZone = 64; // splits zones if more than this number is in same zone. YMMV
        public static float ChunkMassPercent = 0.1f; // 10% of mass per chunk
        public static float ChunkEjectionVelocity = 5.0f; // Base velocity for ejected chunks
        public static float ChunkVelocityRandomization = 2.0f; // Random velocity added to chunks
        public static float InstabilityPerDamage = 0.1f; // How much instability is added per damage point

        public struct MassRange
        {
            public float MinMass;
            public float MaxMass;

            public MassRange(float minMass, float maxMass)
            {
                MinMass = minMass;
                MaxMass = maxMass;
            }
        }

        public static readonly Dictionary<AsteroidType, MassRange> MinMaxMassByType =
            new Dictionary<AsteroidType, MassRange>
            {
                //TODO: think about how you'd make clusters of this stuff. maybe conglomerate rocks first
                { AsteroidType.Ice, new MassRange(10000f, 5000000000f) },
                { AsteroidType.Stone, new MassRange(8000f, 4000000000f) },
                { AsteroidType.Iron, new MassRange(5000f, 3000000000f) },
                { AsteroidType.Nickel, new MassRange(4000f, 2500000000f) },
                { AsteroidType.Cobalt, new MassRange(3000f, 2000000000f) },
                { AsteroidType.Magnesium, new MassRange(2000f, 1500000000f) },
                { AsteroidType.Silicon, new MassRange(5000f, 3500000000f) },
                { AsteroidType.Silver, new MassRange(2000f, 1000000000f) },
                { AsteroidType.Gold, new MassRange(1000f, 800000000f) },
                { AsteroidType.Platinum, new MassRange(500f, 500000000f) },
                { AsteroidType.Uraninite, new MassRange(300f, 200000000f) }
            };

        public static List<SpawnableArea> ValidSpawnLocations = new List<SpawnableArea>();

        public static bool CanSpawnAsteroidAtPoint(Vector3D point, out Vector3D velocity, bool isInRing = false)
        {
            if (isInRing)
            {
                velocity = Vector3D.Zero; // You might want to calculate an appropriate orbital velocity here
                return true;
            }

            foreach (SpawnableArea area in ValidSpawnLocations)
            {
                if (!area.ContainsPoint(point)) continue;
                velocity = area.VelocityAtPoint(point);
                return true;
            }

            velocity = Vector3D.Zero;
            return false;
        }

        private static Random rand = new Random(Seed);

        //  public static int MaxPlayersPerZone { get; internal set; }

        public static AsteroidType GetAsteroidType(Vector3D position)
        {
            double totalWeight = IceWeight + StoneWeight + IronWeight + NickelWeight + CobaltWeight + MagnesiumWeight +
                                 SiliconWeight + SilverWeight + GoldWeight + PlatinumWeight + UraniniteWeight;
            double randomValue = rand.NextDouble() * totalWeight;
            if (randomValue < IceWeight) return AsteroidType.Ice;
            randomValue -= IceWeight;
            if (randomValue < StoneWeight) return AsteroidType.Stone;
            randomValue -= StoneWeight;
            if (randomValue < IronWeight) return AsteroidType.Iron;
            randomValue -= IronWeight;
            if (randomValue < NickelWeight) return AsteroidType.Nickel;
            randomValue -= NickelWeight;
            if (randomValue < CobaltWeight) return AsteroidType.Cobalt;
            randomValue -= CobaltWeight;
            if (randomValue < MagnesiumWeight) return AsteroidType.Magnesium;
            randomValue -= MagnesiumWeight;
            if (randomValue < SiliconWeight) return AsteroidType.Silicon;
            randomValue -= SiliconWeight;
            if (randomValue < SilverWeight) return AsteroidType.Silver;
            randomValue -= SilverWeight;
            if (randomValue < GoldWeight) return AsteroidType.Gold;
            randomValue -= GoldWeight;
            if (randomValue < PlatinumWeight) return AsteroidType.Platinum;
            return AsteroidType.Uraninite;
        }

        public static float GetAsteroidSize(Vector3D position)
        {
            Random rand = new Random(Seed + position.GetHashCode());
            return MinAsteroidSize + (float)rand.NextDouble() * (MaxAsteroidSize - MinAsteroidSize);
        }

        public static double GetRandomAngularVelocity(Random rand)
        {
            return AngularVelocityVariability * rand.NextDouble();
        }

        public static void SaveSettings()
        {
            try
            {
                using (TextWriter writer =
                       MyAPIGateway.Utilities.WriteFileInWorldStorage("AsteroidSettings.cfg", typeof(AsteroidSettings)))
                {
                    writer.WriteLine("[General]");
                    writer.WriteLine($"EnableLogging={EnableLogging}");
                    writer.WriteLine($"EnableMiddleMouseAsteroidSpawn={EnableMiddleMouseAsteroidSpawn}");
                    writer.WriteLine($"EnableVanillaAsteroidSpawnLatching={EnableVanillaAsteroidSpawnLatching}");
                    writer.WriteLine($"VanillaAsteroidSpawnLatchingRadius={VanillaAsteroidSpawnLatchingRadius}");
                    writer.WriteLine("[GasGiantIntegration]");
                    writer.WriteLine($"EnableGasGiantRingSpawning={EnableGasGiantRingSpawning}");
                    writer.WriteLine($"DisableZoneWhileMovingFast={DisableZoneWhileMovingFast}");
                    writer.WriteLine($"ZoneSpeedThreshold={ZoneSpeedThreshold}");
                    writer.WriteLine($"NetworkMessageInterval={NetworkMessageInterval}");
                    writer.WriteLine($"SpawnInterval={SpawnInterval}");
                    writer.WriteLine($"UpdateInterval={UpdateInterval}");
                    writer.WriteLine($"MaxAsteroidCount={MaxAsteroidCount}");
                    writer.WriteLine($"MaxAsteroidsPerZone={MaxAsteroidsPerZone}");
                    writer.WriteLine($"MaxTotalAttempts={MaxTotalAttempts}");
                    writer.WriteLine($"MaxZoneAttempts={MaxZoneAttempts}");
                    writer.WriteLine($"ZoneRadius={ZoneRadius}");
                    writer.WriteLine($"AsteroidVelocityBase={AsteroidVelocityBase}");
                    writer.WriteLine($"VelocityVariability={VelocityVariability}");
                    writer.WriteLine($"AngularVelocityVariability={AngularVelocityVariability}");
                    writer.WriteLine($"MinDistanceFromVanillaAsteroids={MinDistanceFromVanillaAsteroids}");
                    writer.WriteLine($"MinDistanceFromPlayer={MinDistanceFromPlayer}");
                    writer.WriteLine($"Seed={Seed}");
                    writer.WriteLine($"IgnorePlanets={IgnorePlanets}");

                    writer.WriteLine("[Weights]");
                    writer.WriteLine($"IceWeight={IceWeight}");
                    writer.WriteLine($"StoneWeight={StoneWeight}");
                    writer.WriteLine($"IronWeight={IronWeight}");
                    writer.WriteLine($"NickelWeight={NickelWeight}");
                    writer.WriteLine($"CobaltWeight={CobaltWeight}");
                    writer.WriteLine($"MagnesiumWeight={MagnesiumWeight}");
                    writer.WriteLine($"SiliconWeight={SiliconWeight}");
                    writer.WriteLine($"SilverWeight={SilverWeight}");
                    writer.WriteLine($"GoldWeight={GoldWeight}");
                    writer.WriteLine($"PlatinumWeight={PlatinumWeight}");
                    writer.WriteLine($"UraniniteWeight={UraniniteWeight}");

                    writer.WriteLine("[AsteroidSize]");
                    writer.WriteLine($"MinAsteroidSize={MinAsteroidSize}");
                    writer.WriteLine($"MaxAsteroidSize={MaxAsteroidSize}");

                    writer.WriteLine("[Instability]");
                    writer.WriteLine($"InstabilityPerMass={InstabilityPerMass}");
                    writer.WriteLine($"InstabilityThresholdPercent={InstabilityThresholdPercent}");
                    writer.WriteLine($"InstabilityDecayRate={InstabilityDecayRate}");
                    writer.WriteLine($"InstabilityFromDamage={InstabilityFromDamage}");
                    writer.WriteLine($"KgLossPerDamage={KgLossPerDamage}");

                    writer.WriteLine("[SpawnableAreas]");
                    foreach (SpawnableArea area in ValidSpawnLocations)
                    {
                        writer.WriteLine($"Name={area.Name}");
                        writer.WriteLine(
                            $"CenterPosition={area.CenterPosition.X},{area.CenterPosition.Y},{area.CenterPosition.Z}");
                        writer.WriteLine($"Radius={area.Radius}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidSettings), "Failed to save asteroid settings");
            }
        }

        public static void LoadSettings()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage("AsteroidSettings.cfg", typeof(AsteroidSettings)))
                {
                    using (TextReader reader =
                           MyAPIGateway.Utilities.ReadFileInWorldStorage("AsteroidSettings.cfg",
                               typeof(AsteroidSettings)))
                    {
                        string line;
                        SpawnableArea currentArea = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("[") || string.IsNullOrWhiteSpace(line))
                                continue;

                            var parts = line.Split('=');
                            if (parts.Length != 2)
                                continue;

                            var key = parts[0].Trim();
                            var value = parts[1].Trim();

                            switch (key)
                            {
                                case "EnableLogging":
                                    EnableLogging = bool.Parse(value);
                                    break;
                                case "EnableMiddleMouseAsteroidSpawn":
                                    EnableMiddleMouseAsteroidSpawn = bool.Parse(value);
                                    break;
                                case "EnableVanillaAsteroidSpawnLatching":
                                    EnableVanillaAsteroidSpawnLatching = bool.Parse(value);
                                    break;
                                case "VanillaAsteroidSpawnLatchingRadius":
                                    VanillaAsteroidSpawnLatchingRadius = double.Parse(value);
                                    break;
                                case "EnableGasGiantRingSpawning":
                                    EnableGasGiantRingSpawning = bool.Parse(value);
                                    break;
                                case "DisableZoneWhileMovingFast":
                                    DisableZoneWhileMovingFast = bool.Parse(value);
                                    break;
                                case "ZoneSpeedThreshold":
                                    ZoneSpeedThreshold = double.Parse(value);
                                    break;
                                case "NetworkMessageInterval":
                                    NetworkMessageInterval = int.Parse(value);
                                    break;
                                case "SpawnInterval":
                                    SpawnInterval = int.Parse(value);
                                    break;
                                case "UpdateInterval":
                                    UpdateInterval = int.Parse(value);
                                    break;
                                case "MaxAsteroidCount":
                                    MaxAsteroidCount = int.Parse(value);
                                    break;
                                case "MaxAsteroidsPerZone":
                                    MaxAsteroidsPerZone = int.Parse(value);
                                    break;
                                case "MaxTotalAttempts":
                                    MaxTotalAttempts = int.Parse(value);
                                    break;
                                case "MaxZoneAttempts":
                                    MaxZoneAttempts = int.Parse(value);
                                    break;
                                case "ZoneRadius":
                                    ZoneRadius = double.Parse(value);
                                    break;
                                case "AsteroidVelocityBase":
                                    AsteroidVelocityBase = int.Parse(value);
                                    break;
                                case "VelocityVariability":
                                    VelocityVariability = double.Parse(value);
                                    break;
                                case "AngularVelocityVariability":
                                    AngularVelocityVariability = double.Parse(value);
                                    break;
                                case "MinDistanceFromVanillaAsteroids":
                                    MinDistanceFromVanillaAsteroids = double.Parse(value);
                                    break;
                                case "MinDistanceFromPlayer":
                                    MinDistanceFromPlayer = double.Parse(value);
                                    break;
                                case "Seed":
                                    Seed = int.Parse(value);
                                    break;
                                case "IgnorePlanets":
                                    IgnorePlanets = bool.Parse(value);
                                    break;
                                case "IceWeight":
                                    IceWeight = double.Parse(value);
                                    break;
                                case "StoneWeight":
                                    StoneWeight = double.Parse(value);
                                    break;
                                case "IronWeight":
                                    IronWeight = double.Parse(value);
                                    break;
                                case "NickelWeight":
                                    NickelWeight = double.Parse(value);
                                    break;
                                case "CobaltWeight":
                                    CobaltWeight = double.Parse(value);
                                    break;
                                case "MagnesiumWeight":
                                    MagnesiumWeight = double.Parse(value);
                                    break;
                                case "SiliconWeight":
                                    SiliconWeight = double.Parse(value);
                                    break;
                                case "SilverWeight":
                                    SilverWeight = double.Parse(value);
                                    break;
                                case "GoldWeight":
                                    GoldWeight = double.Parse(value);
                                    break;
                                case "PlatinumWeight":
                                    PlatinumWeight = double.Parse(value);
                                    break;
                                case "UraniniteWeight":
                                    UraniniteWeight = double.Parse(value);
                                    break;
                                case "MinAsteroidSize":
                                    MinAsteroidSize = float.Parse(value);
                                    break;
                                case "MaxAsteroidSize":
                                    MaxAsteroidSize = float.Parse(value);
                                    break;
                                case "Name":
                                    if (currentArea != null) ValidSpawnLocations.Add(currentArea);
                                    currentArea = new SpawnableArea { Name = value };
                                    break;
                                case "CenterPosition":
                                    var coords = value.Split(',');
                                    currentArea.CenterPosition = new Vector3D(double.Parse(coords[0]),
                                        double.Parse(coords[1]), double.Parse(coords[2]));
                                    break;
                                case "Radius":
                                    currentArea.Radius = double.Parse(value);
                                    break;
                                case "InstabilityPerMass":
                                    InstabilityPerMass = float.Parse(value);
                                    break;
                                case "InstabilityThresholdPercent":
                                    InstabilityThresholdPercent = float.Parse(value);
                                    break;
                                case "InstabilityDecayRate":
                                    InstabilityDecayRate = float.Parse(value);
                                    break;
                                case "InstabilityFromDamage":
                                    InstabilityFromDamage = float.Parse(value);
                                    break;
                                case "KgLossPerDamage":
                                    KgLossPerDamage = float.Parse(value);
                                    break;
                            }
                        }

                        if (currentArea != null) ValidSpawnLocations.Add(currentArea);
                    }
                }
                else
                {
                    // Create default configuration if it doesn't exist
                    ValidSpawnLocations.Add(new SpawnableArea
                    {
                        Name = "DefaultArea",
                        CenterPosition = new Vector3D(0.0, 0.0, 0.0),
                        Radius = 0
                    });
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(AsteroidSettings), "Failed to load asteroid settings");
            }
        }

        private static void WriteIntArray(TextWriter writer, string key, int[] array)
        {
            writer.WriteLine($"{key}={string.Join(",", array)}");
        }

        private static int[] ReadIntArray(string value)
        {
            var parts = value.Split(',');
            var array = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                array[i] = int.Parse(parts[i]);
            }

            return array;
        }

        public static void AddSpawnableArea(string name, Vector3D center, double radius)
        {
            ValidSpawnLocations.Add(new SpawnableArea
            {
                Name = name,
                CenterPosition = center,
                Radius = radius
            });
            SaveSettings();
        }

        public static void RemoveSpawnableArea(string name)
        {
            SpawnableArea area =
                ValidSpawnLocations.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (area == null) return;
            ValidSpawnLocations.Remove(area);
            SaveSettings();
        }

        public static float CalculateMassScaleByDistance(Vector3D position, RealGasGiantsApi gasGiantsApi, out string debugInfo) {
            // TODO: This tolerance is a workaround because the RealGasGiants API reports rings ending sooner than they should and there's little bands of the outer edge i want
            const float SPAWN_TOLERANCE = 1.25f; // Allow spawning up to 25% beyond configured ring

            debugInfo = "";
            if (gasGiantsApi == null || !gasGiantsApi.IsReady) {
                debugInfo = "GasGiants API not ready";
                return 0f;
            }

            var gasGiants = gasGiantsApi.GetAtmoGasGiantsAtPosition(Vector3D.Zero);

            if (!gasGiants.Any()) {
                debugInfo = "No gas giants found at origin";
                return 0f;
            }

            var nearestGasGiant = gasGiants.First();
            var basicInfo = gasGiantsApi.GetGasGiantConfig_BasicInfo_Base(nearestGasGiant);
            if (!basicInfo.Item1) {
                debugInfo = "Failed to get gas giant info";
                return 0f;
            }

            float gasGiantRadiusMeters = basicInfo.Item2 * 1000f;
            var gasGiantCenter = nearestGasGiant.PositionComp.GetPosition();

            var ringInfo = gasGiantsApi.GetGasGiantConfig_RingInfo_Size(nearestGasGiant);
            if (!ringInfo.Item1) {
                debugInfo = "Failed to get ring info";
                return 0f;
            }

            float innerRingRadiusMeters = gasGiantRadiusMeters * ringInfo.Item3;
            float outerRingRadiusMeters = gasGiantRadiusMeters * ringInfo.Item4;
            float extendedSpawnRadiusMeters = outerRingRadiusMeters * SPAWN_TOLERANCE;

            double distanceFromCenterMeters = Vector3D.Distance(position, gasGiantCenter);

            debugInfo = $"Ring metrics:" +
                        $"\n - Gas Giant: {basicInfo.Item4} at {gasGiantCenter}" +
                        $"\n - Gas Giant Radius: {gasGiantRadiusMeters / 1000f:N0}km" +
                        $"\n - Inner Ring: {innerRingRadiusMeters / 1000f:N0}km" +
                        $"\n - Outer Ring: {outerRingRadiusMeters / 1000f:N0}km" +
                        $"\n - Extended Spawn Range: {extendedSpawnRadiusMeters / 1000f:N0}km" +
                        $"\n - Distance: {distanceFromCenterMeters / 1000f:N0}km";

            // Allow spawning up to the extended range
            if (distanceFromCenterMeters >= extendedSpawnRadiusMeters || distanceFromCenterMeters <= gasGiantRadiusMeters) {
                debugInfo += "\n - Outside valid spawn range";
                return 0f;
            }

            // But scale mass only within the configured ring dimensions
            if (distanceFromCenterMeters <= outerRingRadiusMeters) {
                float scale = MathHelper.Lerp(
                    1.0f, // Inner ring = maximum mass
                    0.0f, // Outer ring = minimum mass
                    (float)((distanceFromCenterMeters - innerRingRadiusMeters) / (outerRingRadiusMeters - innerRingRadiusMeters))
                );
                debugInfo += $"\n - Scale Factor: {scale:F3}";
                return scale;
            }

            // Beyond configured ring but within spawn tolerance - minimum mass
            debugInfo += "\n - Scale Factor: 0.000 (Extended region)";
            return 0f;
        }

        public class SpawnableArea
        {
            public string Name { get; set; }
            public Vector3D CenterPosition { get; set; }
            public double Radius { get; set; }

            public bool ContainsPoint(Vector3D point)
            {
                double distanceSquared = (point - CenterPosition).LengthSquared();
                return distanceSquared <= Radius * Radius;
            }

            public Vector3D VelocityAtPoint(Vector3D point)
            {
                return (point - CenterPosition).Normalized() * AsteroidSettings.AsteroidVelocityBase;
            }
        }

    }
}