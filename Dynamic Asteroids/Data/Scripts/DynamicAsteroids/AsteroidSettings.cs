using System;
using System.IO;
using DynamicAsteroids.AsteroidEntities;
using Invalid.DynamicRoids;
using Sandbox.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System.Linq;

namespace DynamicAsteroids
{
    public static class AsteroidSettings
    {
        public static bool EnableLogging = false;
        public static bool EnablePersistence = false;
        public static bool EnableMiddleMouseAsteroidSpawn = false;
        public static bool EnableVanillaAsteroidSpawnLatching = false;
        public static double VanillaAsteroidSpawnLatchingRadius = 10000;
        public static bool DisableZoneWhileMovingFast = true;
        public static double ZoneSpeedThreshold = 2000.0;
        public static int SaveStateInterval = 600;
        public static int NetworkMessageInterval = 120;
        public static int SpawnInterval = 6;
        public static int UpdateInterval = 120;
        public static int MaxAsteroidCount = 20000;
        public static int MaxAsteroidsPerZone = 100;
        public static int MaxTotalAttempts = 100;
        public static int MaxZoneAttempts = 50;
        public static double ZoneRadius = 10000.0;
        public static int AsteroidVelocityBase = 0;
        public static double VelocityVariability = 0;
        public static double AngularVelocityVariability = 0;
        public static double MinDistanceFromVanillaAsteroids = 1000;
        public static double MinDistanceFromPlayer = 3000;
        public static int Seed = 69420;
        public static bool IgnorePlanets = true;
        public static double IceWeight = 99;
        public static double StoneWeight = 0.5;
        public static double IronWeight = 0.25;
        public static double NickelWeight = 0.05;
        public static double CobaltWeight = 0.05;
        public static double MagnesiumWeight = 0.05;
        public static double SiliconWeight = 0.05;
        public static double SilverWeight = 0.05;
        public static double GoldWeight = 0.05;
        public static double PlatinumWeight = 0.05;
        public static double UraniniteWeight = 0.05;
        public static float BaseIntegrity = 1f;
        public static float MinAsteroidSize = 50f;
        public static float MaxAsteroidSize = 250f;
        public static float MinSubChunkSize = 5f;
        public static double SubChunkVelocityMin = 1.0;
        public static double SubChunkVelocityMax = 5.0;
        public static double SubChunkAngularVelocityMin = 0.01;
        public static double SubChunkAngularVelocityMax = 0.1;
        public static int[] IceDropRange = { 1000, 10000 };
        public static int[] StoneDropRange = { 1000, 10000 };
        public static int[] IronDropRange = { 500, 2500 };
        public static int[] NickelDropRange = { 500, 2500 };
        public static int[] CobaltDropRange = { 500, 2500 };
        public static int[] MagnesiumDropRange = { 500, 2500 };
        public static int[] SiliconDropRange = { 500, 2500 };
        public static int[] SilverDropRange = { 500, 2500 };
        public static int[] GoldDropRange = { 500, 2500 };
        public static int[] PlatinumDropRange = { 500, 2500 };
        public static int[] UraniniteDropRange = { 500, 2500 };

        public static List<SpawnableArea> ValidSpawnLocations = new List<SpawnableArea>();

        public static bool CanSpawnAsteroidAtPoint(Vector3D point, out Vector3D velocity)
        {
            foreach (var area in ValidSpawnLocations)
            {
                if (area.ContainsPoint(point))
                {
                    velocity = area.VelocityAtPoint(point);
                    return true;
                }
            }
            velocity = Vector3D.Zero;
            return false;
        }

        private static Random rand = new Random(Seed);

        public static AsteroidType GetAsteroidType(Vector3D position)
        {
            double totalWeight = IceWeight + StoneWeight + IronWeight + NickelWeight + CobaltWeight + MagnesiumWeight + SiliconWeight + SilverWeight + GoldWeight + PlatinumWeight + UraniniteWeight;
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

        public static double GetRandomSubChunkVelocity(Random rand)
        {
            return SubChunkVelocityMin + rand.NextDouble() * (SubChunkVelocityMax - SubChunkVelocityMin);
        }

        public static double GetRandomSubChunkAngularVelocity(Random rand)
        {
            return SubChunkAngularVelocityMin + rand.NextDouble() * (SubChunkAngularVelocityMax - SubChunkAngularVelocityMin);
        }

        public static void SaveSettings()
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("AsteroidSettings.cfg", typeof(AsteroidSettings)))
                {
                    writer.WriteLine("[General]");
                    writer.WriteLine($"EnableLogging={EnableLogging}");
                    writer.WriteLine($"EnablePersistence={EnablePersistence}");
                    writer.WriteLine($"EnableMiddleMouseAsteroidSpawn={EnableMiddleMouseAsteroidSpawn}");
                    writer.WriteLine($"EnableVanillaAsteroidSpawnLatching={EnableVanillaAsteroidSpawnLatching}");
                    writer.WriteLine($"VanillaAsteroidSpawnLatchingRadius={VanillaAsteroidSpawnLatchingRadius}");
                    writer.WriteLine($"DisableZoneWhileMovingFast={DisableZoneWhileMovingFast}");
                    writer.WriteLine($"ZoneSpeedThreshold={ZoneSpeedThreshold}");
                    writer.WriteLine($"SaveStateInterval={SaveStateInterval}");
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
                    writer.WriteLine($"BaseIntegrity={BaseIntegrity}");
                    writer.WriteLine($"MinAsteroidSize={MinAsteroidSize}");
                    writer.WriteLine($"MaxAsteroidSize={MaxAsteroidSize}");
                    writer.WriteLine($"MinSubChunkSize={MinSubChunkSize}");

                    writer.WriteLine("[SubChunkVelocity]");
                    writer.WriteLine($"SubChunkVelocityMin={SubChunkVelocityMin}");
                    writer.WriteLine($"SubChunkVelocityMax={SubChunkVelocityMax}");
                    writer.WriteLine($"SubChunkAngularVelocityMin={SubChunkAngularVelocityMin}");
                    writer.WriteLine($"SubChunkAngularVelocityMax={SubChunkAngularVelocityMax}");

                    writer.WriteLine("[DropRanges]");
                    WriteIntArray(writer, "IceDropRange", IceDropRange);
                    WriteIntArray(writer, "StoneDropRange", StoneDropRange);
                    WriteIntArray(writer, "IronDropRange", IronDropRange);
                    WriteIntArray(writer, "NickelDropRange", NickelDropRange);
                    WriteIntArray(writer, "CobaltDropRange", CobaltDropRange);
                    WriteIntArray(writer, "MagnesiumDropRange", MagnesiumDropRange);
                    WriteIntArray(writer, "SiliconDropRange", SiliconDropRange);
                    WriteIntArray(writer, "SilverDropRange", SilverDropRange);
                    WriteIntArray(writer, "GoldDropRange", GoldDropRange);
                    WriteIntArray(writer, "PlatinumDropRange", PlatinumDropRange);
                    WriteIntArray(writer, "UraniniteDropRange", UraniniteDropRange);

                    writer.WriteLine("[SpawnableAreas]");
                    foreach (var area in ValidSpawnLocations)
                    {
                        writer.WriteLine($"Name={area.Name}");
                        writer.WriteLine($"CenterPosition={area.CenterPosition.X},{area.CenterPosition.Y},{area.CenterPosition.Z}");
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
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("AsteroidSettings.cfg", typeof(AsteroidSettings)))
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
                                case "EnablePersistence":
                                    EnablePersistence = bool.Parse(value);
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
                                case "DisableZoneWhileMovingFast":
                                    DisableZoneWhileMovingFast = bool.Parse(value);
                                    break;
                                case "ZoneSpeedThreshold":
                                    ZoneSpeedThreshold = double.Parse(value);
                                    break;
                                case "SaveStateInterval":
                                    SaveStateInterval = int.Parse(value);
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
                                case "BaseIntegrity":
                                    BaseIntegrity = float.Parse(value);
                                    break;
                                case "MinAsteroidSize":
                                    MinAsteroidSize = float.Parse(value);
                                    break;
                                case "MaxAsteroidSize":
                                    MaxAsteroidSize = float.Parse(value);
                                    break;
                                case "MinSubChunkSize":
                                    MinSubChunkSize = float.Parse(value);
                                    break;
                                case "SubChunkVelocityMin":
                                    SubChunkVelocityMin = double.Parse(value);
                                    break;
                                case "SubChunkVelocityMax":
                                    SubChunkVelocityMax = double.Parse(value);
                                    break;
                                case "SubChunkAngularVelocityMin":
                                    SubChunkAngularVelocityMin = double.Parse(value);
                                    break;
                                case "SubChunkAngularVelocityMax":
                                    SubChunkAngularVelocityMax = double.Parse(value);
                                    break;
                                case "IceDropRange":
                                    IceDropRange = ReadIntArray(value);
                                    break;
                                case "StoneDropRange":
                                    StoneDropRange = ReadIntArray(value);
                                    break;
                                case "IronDropRange":
                                    IronDropRange = ReadIntArray(value);
                                    break;
                                case "NickelDropRange":
                                    NickelDropRange = ReadIntArray(value);
                                    break;
                                case "CobaltDropRange":
                                    CobaltDropRange = ReadIntArray(value);
                                    break;
                                case "MagnesiumDropRange":
                                    MagnesiumDropRange = ReadIntArray(value);
                                    break;
                                case "SiliconDropRange":
                                    SiliconDropRange = ReadIntArray(value);
                                    break;
                                case "SilverDropRange":
                                    SilverDropRange = ReadIntArray(value);
                                    break;
                                case "GoldDropRange":
                                    GoldDropRange = ReadIntArray(value);
                                    break;
                                case "PlatinumDropRange":
                                    PlatinumDropRange = ReadIntArray(value);
                                    break;
                                case "UraniniteDropRange":
                                    UraniniteDropRange = ReadIntArray(value);
                                    break;
                                case "Name":
                                    if (currentArea != null) ValidSpawnLocations.Add(currentArea);
                                    currentArea = new SpawnableArea { Name = value };
                                    break;
                                case "CenterPosition":
                                    var coords = value.Split(',');
                                    currentArea.CenterPosition = new Vector3D(double.Parse(coords[0]), double.Parse(coords[1]), double.Parse(coords[2]));
                                    break;
                                case "Radius":
                                    currentArea.Radius = double.Parse(value);
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
            var area = ValidSpawnLocations.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (area != null)
            {
                ValidSpawnLocations.Remove(area);
                SaveSettings();
            }
        }

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
