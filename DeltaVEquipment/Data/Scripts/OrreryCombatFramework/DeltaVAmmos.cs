using DeltaVEquipment.ProjectileBases;
using VRage.Utils;

namespace DeltaVEquipment
{
    partial class HeartDefinitions
    {
        ProjectileDefinitionBase ExampleAmmoProjectile => new ProjectileDefinitionBase
        {
            Name = "ExampleAmmoProjectile",
            Ungrouped = new Ungrouped
            {
                ReloadPowerUsage = 10,
                Recoil = 5000,
                Impulse = 5000,
                ShotsPerMagazine = 100,
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
                BaseDamage = 5000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
                DamageToProjectiles = 0.4f,
                DamageToProjectilesRadius = 0.2f,
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
                TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                //AttachedParticle = "Smoke_Missile",
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
        
        ProjectileDefinitionBase DeltaVMiningLaserAmmoBeam => new ProjectileDefinitionBase
        {
            Name = "DeltaVMiningLaserAmmoBeam",
            Ungrouped = new Ungrouped
            {
                ReloadPowerUsage = 0,
                Recoil = 0,
                Impulse = 0,
                ShotsPerMagazine = 360,
                MagazineItemToConsume = "",
            },
            Networking = new Networking
            {
                NetworkingMode = Networking.NetworkingModeEnum.NoNetworking,
                DoConstantSync = false,
                NetworkPriority = 0,
            },
            Damage = new Damage
            {
                SlimBlockDamageMod = 1,
                FatBlockDamageMod = 1,
                BaseDamage = 1000,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
            },
            PhysicalProjectile = new PhysicalProjectile
            {
                Velocity = 800,
                VelocityVariance = 0,
                Acceleration = 0,
                Health = 1,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = true,
            },
            Visual = new Visual
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                TrailFadeTime = 0f,
                TrailLength = 8,
                TrailWidth = 0.5f,
                TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                //AttachedParticle = "Smoke_Missile",
                ImpactParticle = "",
                VisibleChance = 1f,
            },
            Audio = new Audio
            {
                TravelSound = "",
                TravelVolume = 100,
                TravelMaxDistance = 1000,
                ImpactSound = "",
                SoundChance = 0.1f,
            },
            Guidance = new Guidance[]
            {
                //new Guidance()
                //{
                //    TriggerTime = 0,
                //    ActiveDuration = -1,
                //    UseAimPrediction = false,
                //    MaxTurnRate = -1.5f,
                //    IFF = 2,
                //    DoRaycast = false,
                //    CastCone = 0.5f,
                //    CastDistance = 1000,
                //    Velocity = 50f,
                //},
                //new Guidance()
                //{
                //    TriggerTime = 1f,
                //    ActiveDuration = -1f,
                //    UseAimPrediction = false,
                //    MaxTurnRate = 3.14f,
                //    IFF = 2,
                //    DoRaycast = false,
                //    CastCone = 0.5f,
                //    CastDistance = 1000,
                //    Velocity = -1f,
                //}
            },
            LiveMethods = new LiveMethods
            {

            }
        };

        ProjectileDefinitionBase DeltaVMiningMetalStormProjectile => new ProjectileDefinitionBase
        {
            Name = "DeltaVMiningMetalStormProjectile",
            Ungrouped = new Ungrouped
            {
                ReloadPowerUsage = 10,
                Recoil = 5000,
                Impulse = 5000,
                ShotsPerMagazine = 32,
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
                BaseDamage = 500,
                AreaDamage = 0,
                AreaRadius = 0,
                MaxImpacts = 1,
                DamageToProjectiles = 0.4f,
                DamageToProjectilesRadius = 0.2f,
            },
            PhysicalProjectile = new PhysicalProjectile
            {
                Velocity = 1000,
                VelocityVariance = 0,
                Acceleration = 0,
                Health = 0,
                MaxTrajectory = 4000,
                MaxLifetime = -1,
                IsHitscan = false,
                ProjectileSize = 0.5f,
            },
            Visual = new Visual
            {
                //Model = "Models\\Weapons\\Projectile_Missile.mwm",
                TrailTexture = MyStringId.GetOrCompute("ProjectileTrailLine"),
                TrailFadeTime = 0f,
                TrailLength = 5,
                TrailWidth = 0.1f,
                TrailColor = new VRageMath.Vector4(25, 2, 0, 1),
                //AttachedParticle = "Smoke_Missile",
                ImpactParticle = "MaterialHit_Metal",
                VisibleChance = 1f,
            },
            Audio = new Audio
            {
                TravelSound = "",
                TravelVolume = 100,
                TravelMaxDistance = 1000,
                ImpactSound = "cbar",
                SoundChance = 0.1f,
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
