using DeltaVEquipment.ProjectileBases;
using VRage.Utils;
using VRageMath;

namespace DeltaVEquipment
{
    partial class HeartDefinitions
    {
        ProjectileDefinitionBase DeltaVAmmoRailgun => new ProjectileDefinitionBase
        {
            Name = "DeltaV Railgun Ammo",
            Ungrouped = new Ungrouped
            {
                ReloadPowerUsage = 10,
                Recoil = 1000000,
                Impulse = 1000000,
                ShotsPerMagazine = 1,
                MagazineItemToConsume = "",
            },
            Networking = new Networking
            {
                NetworkingMode = Networking.NetworkingModeEnum.FireEvent,
                DoConstantSync = false,
                NetworkPriority = 0,
            },
            Damage = new Damage
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 50000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 3,
                DamageToProjectiles = 0f,
                DamageToProjectilesRadius = 0f,
            },
            PhysicalProjectile = new PhysicalProjectile
            {
                Velocity = 800,
                VelocityVariance = 0,
                Acceleration = 0,
                Health = 0,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                GravityInfluenceMultiplier = 0.01f,
                ProjectileSize = 0.5f,
            },
            Visual = new Visual
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
                TrailColor = new Vector4(61, 24, 24, 200),
                AttachedParticle = "DeltaV_RailgunParticle",
                ImpactParticle = "MaterialHit_Metal",
                VisibleChance = 1f,
            },
            Audio = new Audio
            {
                TravelSound = "",
                TravelVolume = 100,
                TravelMaxDistance = 1000,
                ImpactSound = "WepSmallWarheadExpl",
                SoundChance = 0.1f,
            },
            Guidance = new Guidance[]
            {
            },
            LiveMethods = new LiveMethods
            {
               // OnImpact = (projectileInfo, hitPosition, hitNormal, hitEntity) =>
               // {
               //     if (hitEntity == null)
               //         return;
               //     HeartApi.SpawnProjectilesInCone(HeartApi.GetProjectileDefinitionId(ExampleAmmoMissile.Name), hitPosition - hitNormal * 50, hitNormal, 10, 0.1f);
               // }
            }
        };
    }
}
