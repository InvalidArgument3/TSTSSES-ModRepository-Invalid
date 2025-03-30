using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace DynamicAsteroids.Data.Scripts.DynamicAsteroids.AsteroidEntities {
    public class AsteroidPhysicalProperties {
        public float Mass { get; private set; }
        public float Volume { get; private set; }
        public float Radius { get; private set; }
        public float Diameter { get; private set; }
        public float Density { get; private set; }
        public float MaximumIntegrity { get; private set; }
        public float CurrentIntegrity { get; private set; }
        public float MaxInstability { get; private set; }
        public float CurrentInstability { get; private set; }
        public float InstabilityThreshold { get; private set; }

        private const float CHUNK_THRESHOLD = 0.1f; // 10% intervals
        private float _lastChunkThreshold = 0f;

        public const float DEFAULT_DENSITY = 917.0f; // kg/m³

        private AsteroidEntity ParentEntity { get; set; }


        public AsteroidPhysicalProperties(float diameter, float density = DEFAULT_DENSITY, AsteroidEntity parentEntity = null) {
            ParentEntity = parentEntity;
            Diameter = diameter;
            Radius = diameter / 2.0f;
            Density = density;

            // Calculate volume and mass from diameter
            Volume = (4.0f / 3.0f) * MathHelper.Pi * (float)Math.Pow(Radius, 3);
            Mass = Volume * Density;

            MaxInstability = Mass * AsteroidSettings.InstabilityPerMass;
            InstabilityThreshold = MaxInstability * AsteroidSettings.InstabilityThresholdPercent;
            CurrentInstability = 0;

            Log.Info($"Created asteroid with:" +
                     $"\n - Diameter: {Diameter:F2}m" +
                     $"\n - Mass: {Mass:N0}kg" +
                     $"\n - Volume: {Volume:N0}m³");
        }

        public void AddInstability(float amount) {
            CurrentInstability = Math.Min(MaxInstability, CurrentInstability + amount);
        }

        public float GetIntegrityPercentage() => (CurrentIntegrity / MaximumIntegrity) * 100f;
        public float GetInstabilityPercentage() => (CurrentInstability / MaxInstability) * 100f;
        public bool IsDestroyed() => Mass <= 0;
        public bool IsUnstable() => CurrentInstability >= InstabilityThreshold;

        public void UpdateInstability() {
            float previousInstability = CurrentInstability;
            CurrentInstability = Math.Max(0, CurrentInstability -
                                             (AsteroidSettings.InstabilityDecayRate * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS));

            if (Math.Abs(previousInstability - CurrentInstability) > 0.01f) {
                Log.Info($"Instability decay: {previousInstability:F2} -> {CurrentInstability:F2} " +
                         $"(-{previousInstability - CurrentInstability:F2})");
            }
        }

        public void ReduceMass(float damageAmount) {
            float massToRemove = damageAmount * AsteroidSettings.KgLossPerDamage;
            Mass = Math.Max(0, Mass - massToRemove);
        }

        public static AsteroidPhysicalProperties CreateFromMass(float targetMass, float density = DEFAULT_DENSITY, AsteroidEntity parentEntity = null) {
            // Calculate volume from mass and density
            float volume = targetMass / density;

            // Calculate radius from volume (V = 4/3 * π * r³)
            float radius = (float)Math.Pow((3.0f * volume) / (4.0f * MathHelper.Pi), 1.0f / 3.0f);

            // Create new properties with calculated diameter
            float diameter = radius * 2.0f;

            Log.Info($"Creating asteroid from mass:" +
                     $"\n - Target Mass: {targetMass:N0}kg" +
                     $"\n - Calculated Diameter: {diameter:F2}m" +
                     $"\n - Calculated Volume: {volume:N0}m³");

            return new AsteroidPhysicalProperties(diameter, density, parentEntity);
        }

        public bool ShouldSpawnChunk() {
            float currentInstabilityPercent = CurrentInstability / MaxInstability;
            float currentThreshold = (float)Math.Floor(currentInstabilityPercent / CHUNK_THRESHOLD) * CHUNK_THRESHOLD;

            if (currentThreshold > _lastChunkThreshold) {
                _lastChunkThreshold = currentThreshold;
                return true;
            }
            return false;
        }

        public void ResetInstability() {
            CurrentInstability = 0f;
            _lastChunkThreshold = 0f;
        }

    }

}

