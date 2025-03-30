using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDebrisFramework.Utils {
    public class DebrisConfig {
        public static DebrisConfig Instance { get; private set; }

        // Debug Settings
        public static bool EnableLogging { get; private set; } = false;
        public bool EnableDebugLogging { get; private set; } = false;

        // Network Settings
        public int NetworkUpdateInterval { get; private set; } = 60;
        public struct NetworkChannels {
            public const ushort EntitySync = 32000;
            public const ushort ZoneSync = 32001;
            public const ushort ConfigSync = 32002;
        }

        // Spawn Settings
        public int MaxDebrisCount { get; private set; } = 1000;
        public int MaxDebrisPerZone { get; private set; } = 100;
        public int SpawnInterval { get; private set; } = 60;
        public double MinSpawnDistance { get; private set; } = 100;
        public double MaxSpawnDistance { get; private set; } = 1000;

        // Zone Settings
        public double ZoneRadius { get; private set; } = 5000;
        public int MaxZonesPerPlayer { get; private set; } = 1;
        public double ZoneCleanupTimeout { get; private set; } = 300;

        // Physics Settings
        public double MaxSpeed { get; private set; } = 100;
        public double MaxAngularVelocity { get; private set; } = 0.5;
        public float DefaultMass { get; private set; } = 1000;

        // Core Settings
        public int Seed { get; private set; }

        public DebrisConfig() {
            Instance = this;
            Seed = new Random().Next();
        }

        public void LoadConfig() {
            try {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage("DebrisConfig.cfg", typeof(DebrisConfig))) {
                    SaveConfig();
                    return;
                }

                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("DebrisConfig.cfg", typeof(DebrisConfig))) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split('=');
                        if (parts.Length != 2)
                            continue;

                        ParseConfigLine(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }
            catch (Exception ex) {
                DebrisLogger.Exception(ex, typeof(DebrisConfig), "Error loading config: ");
            }
        }

        private void ParseConfigLine(string key, string value) {
            try {
                switch (key.ToLower()) {
                    case "enablelogging":
                        EnableLogging = bool.Parse(value);
                        break;
                    case "enabledebuglogging":
                        EnableDebugLogging = bool.Parse(value);
                        break;
                    case "networkupdateinterval":
                        NetworkUpdateInterval = int.Parse(value);
                        break;
                        // Add more config options here
                }
            }
            catch (Exception ex) {
                DebrisLogger.Warning($"Failed to parse config line: {key}={value}. Error: {ex.Message}");
            }
        }

        public void SaveConfig() {
            try {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("DebrisConfig.cfg", typeof(DebrisConfig))) {
                    writer.WriteLine("# Dynamic Debris Framework Configuration");
                    writer.WriteLine();
                    writer.WriteLine("# Debug Settings");
                    writer.WriteLine($"EnableLogging={EnableLogging}");
                    writer.WriteLine($"EnableDebugLogging={EnableDebugLogging}");
                    writer.WriteLine();
                    writer.WriteLine("# Network Settings");
                    writer.WriteLine($"NetworkUpdateInterval={NetworkUpdateInterval}");
                    // Add more config options here
                }
            }
            catch (Exception ex) {
                DebrisLogger.Exception(ex, typeof(DebrisConfig), "Error saving config: ");
            }
        }
    }
}
