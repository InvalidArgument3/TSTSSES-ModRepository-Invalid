using System;
using ProtoBuf;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace DeltaVEquipment.ProjectileBases
{
    /// <summary>
    /// Standard serializable projectile definition.
    /// </summary>
    [ProtoContract]
    public class ProjectileDefinitionBase
    {
        public ProjectileDefinitionBase() { }

        [ProtoMember(1)] public string Name = "";
        [ProtoMember(2)] public Ungrouped Ungrouped;
        [ProtoMember(3)] public Damage Damage;
        [ProtoMember(4)] public PhysicalProjectile PhysicalProjectile;
        [ProtoMember(5)] public Visual Visual;
        [ProtoMember(6)] public Audio Audio;
        [ProtoMember(7)] public Guidance[] Guidance = new Guidance[0];
        [ProtoMember(8)] public Networking Networking;
        [ProtoIgnore] public LiveMethods LiveMethods = new LiveMethods();
    }

    [ProtoContract]
    public struct Ungrouped
    {
        /// <summary>
        /// Power draw during reload, in MW
        /// </summary>
        [ProtoMember(1)] public float ReloadPowerUsage; // TODO
        /// <summary>
        /// Recoil of projectile, in Newtons
        /// </summary>
        [ProtoMember(2)] public int Recoil;
        /// <summary>
        /// Impulse of projectile, in Newtons
        /// </summary>
        [ProtoMember(3)] public int Impulse;
        /// <summary>
        /// Number of shots in single reload.
        /// </summary>
        [ProtoMember(4)] public int ShotsPerMagazine;
        /// <summary>
        /// The item that needs to get consumed for the magazine to reload. Leave blank to not consume anything. The weapon model should probably have a conveyor port.
        /// </summary>
        [ProtoMember(5)] public string MagazineItemToConsume;
        /// <summary>
        /// The order in which projectiles are synced.
        /// </summary>
        [ProtoMember(6)] public ushort SyncPriority;
    }

    [ProtoContract]
    public struct Networking
    {
        /// <summary>
        /// The networking mode of the projectile.
        /// </summary>
        [ProtoMember(1)] public NetworkingModeEnum NetworkingMode;
        /// <summary>
        /// Set this to true if the projectile should constantly be updated over the network.
        /// </summary>
        [ProtoMember(2)] public bool DoConstantSync;
        /// <summary>
        /// Higher numbers take precedence over lower ones.
        /// </summary>
        [ProtoMember(3)] public ushort NetworkPriority;

        public enum NetworkingModeEnum
        {
            /// <summary>
            /// Projectiles are not synced between server and client. Use this for hitscans.
            /// </summary>
            NoNetworking,
            /// <summary>
            /// Projectiles are synced in a 'light' manner. This should be your default.
            /// </summary>
            FireEvent,
            /// <summary>
            /// Projectiles are hard-synced between server and client. Use this for projectiles that *have* to be accurate.
            /// </summary>
            FullSync
        }
    }

    [ProtoContract]
    public struct Damage
    {
        [ProtoMember(1)] public float SlimBlockDamageMod;
        [ProtoMember(2)] public float FatBlockDamageMod;
        [ProtoMember(3)] public float BaseDamage;
        [ProtoMember(4)] public float AreaDamage;
        [ProtoMember(5)] public float DamageToProjectiles;
        [ProtoMember(6)] public int MaxImpacts;
        [ProtoMember(7)] public float AreaRadius;
        [ProtoMember(8)] public float DamageToProjectilesRadius;
    }

    /// <summary>
    /// Projectile information for non-hitscan projectiles.
    /// </summary>
    [ProtoContract]
    public struct PhysicalProjectile
    {
        [ProtoMember(1)] public float Velocity;
        [ProtoMember(2)] public float Acceleration;
        [ProtoMember(3)] public float Health; // TODO // <= 0 for un-targetable
        /// <summary>
        /// Max range of projectile, relative to first firing. For hitscans, max hitscan length.
        /// </summary>
        [ProtoMember(4)] public float MaxTrajectory;
        [ProtoMember(5)] public float MaxLifetime;
        /// <summary>
        /// Disables velocity updates, and changes several behaviors. Call (Projectile).UpdateBeam() to recycle and lower performance impact.
        /// </summary>
        [ProtoMember(6)] public bool IsHitscan;
        /// <summary>
        /// The size of the projectile in meters. Used for point defense hit checking.
        /// </summary>
        [ProtoMember(7)] public float ProjectileSize;
        [ProtoMember(8)] public float VelocityVariance;
        /// <summary>
        /// How much the weapon's ShotInaccuracy will be multiplied by for this ammo. 0 to ignore.
        /// </summary>
        [ProtoMember(9)] public float AccuracyVarianceMultiplier;
        [ProtoMember(10)] public float GravityInfluenceMultiplier;
    }

    [ProtoContract]
    public struct Visual
    {
        [ProtoMember(1)] public string Model;
        [ProtoMember(2)] public MyStringId TrailTexture;
        [ProtoMember(7)] public float TrailLength;
        [ProtoMember(9)] public float TrailWidth;
        [ProtoMember(8)] public Vector4 TrailColor;
        [ProtoMember(3)] public float TrailFadeTime;
        [ProtoMember(4)] public string AttachedParticle;
        [ProtoMember(5)] public string ImpactParticle;
        [ProtoMember(6)] public float VisibleChance;
        public bool HasModel => !Model?.Equals("") ?? false;
        public bool HasTrail => TrailTexture != null && TrailLength > 0 && TrailWidth > 0 && TrailColor != null && TrailColor != Vector4.Zero;
        public bool HasAttachedParticle => !AttachedParticle?.Equals("") ?? false;
        public bool HasImpactParticle => !ImpactParticle?.Equals("") ?? false;
    }

    [ProtoContract]
    public struct Audio
    {
        [ProtoMember(1)] public string TravelSound;
        [ProtoMember(2)] public float TravelMaxDistance;
        [ProtoMember(3)] public float TravelVolume;
        [ProtoMember(4)] public string ImpactSound;
        [ProtoMember(5)] public float SoundChance;

        public bool HasTravelSound => !TravelSound?.Equals("") ?? false && SoundChance > 0 && TravelMaxDistance > 0 && TravelVolume > 0;
        public bool HasImpactSound => !ImpactSound?.Equals("") ?? false && SoundChance > 0;
        public MySoundPair TravelSoundPair => new MySoundPair(TravelSound);
        public MySoundPair ImpactSoundPair => new MySoundPair(ImpactSound);
    }

    [ProtoContract]
    public struct Guidance
    {
        [ProtoMember(1)] public float TriggerTime;
        [ProtoMember(2)] public float ActiveDuration; // Ignore if -1 or greater than next
        [ProtoMember(3)] public bool UseAimPrediction;
        [ProtoMember(4)] public float MaxTurnRate;
        [ProtoMember(6)] public IffEnum Iff; // 1 is TargetSelf, 2 is TargetEnemies, 4 is TargetFriendlies
        [ProtoMember(7)] public bool DoRaycast;
        [ProtoMember(8)] public float CastCone;
        [ProtoMember(9)] public float CastDistance;
        [ProtoMember(10)] public float Velocity;
        /// <summary>
        /// Random offset from target, in meters.
        /// </summary>
        [ProtoMember(11)] public float Inaccuracy;
        /// <summary>
        /// Maximum G-force the projectile can sustain.
        /// </summary>
        [ProtoMember(12)] public float MaxGs;
        [ProtoMember(13)] public DefinitionPid? Pid;
    }

    [ProtoContract]
    public struct DefinitionPid
    {
        /// <summary>
        /// Direct response to error
        /// </summary>
        [ProtoMember(1)] public float KProportional;
        /// <summary>
        /// Response to historical error
        /// </summary>
        [ProtoMember(2)] public float KIntegral;
        /// <summary>
        /// Damping factor
        /// </summary>
        [ProtoMember(3)] public float KDerivative;
    }

    public class LiveMethods // TODO: OnGuidanceStage && DistanceToTarget
    {
        public void RegisterMethods(string definitionName)
        {
            if (!HeartApi.Inited)
                throw new Exception("HeartAPI has not inited yet!");
            HeartApi.AddOnProjectileSpawn(definitionName, OnSpawn);
            HeartApi.AddOnEndOfLife(definitionName, OnEndOfLife);
            HeartApi.AddOnImpact(definitionName, OnImpact);
            //HeartApi.AddOnGuidanceStage(definitionName, OnGuidanceStage);
            HeartApi.LogWriteLine("Registered LiveMethods on projectile " + definitionName);
        }

        public Action<uint, MyEntity> OnSpawn;
        public Action<uint, Vector3D, Vector3D, MyEntity> OnImpact;
        public Action<uint> OnEndOfLife;
        //public Action<uint, Guidance?> OnGuidanceStage;
    }
}