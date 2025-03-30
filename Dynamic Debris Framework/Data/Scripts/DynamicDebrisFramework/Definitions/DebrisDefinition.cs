using System;
using System.Collections.Generic;
using ProtoBuf;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using VRageMath;
using Sandbox.Definitions;

namespace DynamicDebrisFramework.Definitions {
    [ProtoContract]
    public class DebrisDefinition {
        [ProtoMember(1)]
        public string TypeId { get; set; }

        [ProtoMember(2)]
        public string SubtypeId { get; set; }

        // Component Configuration
        [ProtoMember(3)]
        public List<DebrisComponentDefinition> Components { get; set; }

        // Physical Properties (now influenced by components)
        [ProtoMember(4)]
        public float DensityMultiplier { get; set; } // Multiplier for final density calculation

        [ProtoMember(5)]
        public Vector2 SizeRange { get; set; }  // Min/Max size range in meters

        // Damage Properties
        [ProtoMember(6)]
        public float IntegrityMultiplier { get; set; } // Multiplier for total integrity

        [ProtoMember(7)]
        public float DamageMultiplier { get; set; }

        // Visual Properties
        [ProtoMember(8)]
        public string[] ModelPaths { get; set; }

        [ProtoMember(9)]
        public string DestructionEffect { get; set; }

        // Spawn Properties
        [ProtoMember(10)]
        public float SpawnWeight { get; set; }

        [ProtoMember(11)]
        public Vector2 SpawnVelocityRange { get; set; }

        [ProtoMember(12)]
        public Vector2 SpawnAngularVelocityRange { get; set; }

        public DebrisDefinition() {
            TypeId = "Debris";
            SubtypeId = "Generic";
            Components = new List<DebrisComponentDefinition>();
            DensityMultiplier = 1f;
            SizeRange = new Vector2(1f, 5f);
            IntegrityMultiplier = 1f;
            DamageMultiplier = 1f;
            ModelPaths = new string[0];
            DestructionEffect = "Explosion_Warhead_30";
            SpawnWeight = 1f;
            SpawnVelocityRange = new Vector2(0, 10);
            SpawnAngularVelocityRange = new Vector2(0, 0.5f);
        }
    }

    [ProtoContract]
    public class DebrisComponentDefinition {
        [ProtoMember(1)]
        public string ComponentTypeId { get; set; } // e.g., "SteelPlate"

        [ProtoMember(2)]
        public Vector2 CountRange { get; set; } // Min/Max number of components

        [ProtoMember(3)]
        public float DropChance { get; set; } // Chance to drop when destroyed

        [ProtoMember(4)]
        public float DropRatio { get; set; } // Percentage of components that drop
    }

    // Example implementations:
    public class ShipWreckageDebris : DebrisDefinition {
        public ShipWreckageDebris() {
            TypeId = "Debris";
            SubtypeId = "ShipWreckage";

            Components = new List<DebrisComponentDefinition>
            {
                new DebrisComponentDefinition
                {
                    ComponentTypeId = "SteelPlate",
                    CountRange = new Vector2(50, 200),
                    DropChance = 0.8f,
                    DropRatio = 0.6f
                },
                new DebrisComponentDefinition
                {
                    ComponentTypeId = "Construction",
                    CountRange = new Vector2(20, 80),
                    DropChance = 0.7f,
                    DropRatio = 0.5f
                },
                new DebrisComponentDefinition
                {
                    ComponentTypeId = "MetalGrid",
                    CountRange = new Vector2(10, 40),
                    DropChance = 0.6f,
                    DropRatio = 0.4f
                }
            };

            DensityMultiplier = 0.8f; // Accounting for hollow spaces
            SizeRange = new Vector2(5f, 15f);
            IntegrityMultiplier = 1.2f;
            ModelPaths = new[] { "Models/Debris/ShipWreckage1.mwm", "Models/Debris/ShipWreckage2.mwm" };
        }
    }

    public class IronAsteroidDebris : DebrisDefinition {
        public IronAsteroidDebris() {
            TypeId = "Debris";
            SubtypeId = "IronAsteroid";

            Components = new List<DebrisComponentDefinition>
            {
                new DebrisComponentDefinition
                {
                    ComponentTypeId = "Ore/Iron",
                    CountRange = new Vector2(5000, 20000), // In kg
                    DropChance = 1f,
                    DropRatio = 0.9f
                }
            };

            DensityMultiplier = 1f;
            SizeRange = new Vector2(3f, 8f);
            IntegrityMultiplier = 0.8f;
            ModelPaths = new[] { "Models/Debris/IronAsteroid1.mwm" };
        }
    }

    // Helper class for component calculations
    public class DebrisComponentCalculator {
        public static float CalculateTotalMass(List<DebrisComponentDefinition> components, float sizeMultiplier) {
            float totalMass = 0f;

            foreach (var component in components) {
                var componentDef = GetComponentDefinition(component.ComponentTypeId);
                if (componentDef != null) {
                    float avgCount = (component.CountRange.X + component.CountRange.Y) / 2f;
                    totalMass += componentDef.Mass * avgCount * sizeMultiplier;
                }
            }

            return totalMass;
        }

        public static float CalculateTotalIntegrity(List<DebrisComponentDefinition> components, float sizeMultiplier) {
            float totalIntegrity = 0f;

            foreach (var component in components) {
                var componentDef = GetComponentDefinition(component.ComponentTypeId);
                if (componentDef != null) {
                    float avgCount = (component.CountRange.X + component.CountRange.Y) / 2f;
                    totalIntegrity += componentDef.MaxIntegrity * avgCount * sizeMultiplier;
                }
            }

            return totalIntegrity;
        }

        private static MyComponentDefinition GetComponentDefinition(string componentTypeId) {
            return MyDefinitionManager.Static.GetComponentDefinition(
                new MyDefinitionId(typeof(MyObjectBuilder_Component), componentTypeId));
        }
    }
}