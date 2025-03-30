using System;
using DeltaVEquipment.WeaponBases;

namespace DeltaVEquipment
{
    internal partial class HeartDefinitions
    {
        private WeaponDefinitionBase DeltaVRailgunFixed => new WeaponDefinitionBase
        {
            Targeting = new Targeting
            {
                MinTargetingRange = 0,
                MaxTargetingRange = 4000,
                CanAutoShoot = true,
                RetargetTime = -1,
                AimTolerance = 0.0175f,
            },
            Assignments = new Assignments
            {
                BlockSubtype = "FixedRailgun",
                MuzzleSubpart = "",
                ElevationSubpart = "",
                AzimuthSubpart = "",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new[]
                {
                    "muzzle_projectile_1"
                }
            },
            Hardpoint = new Hardpoint
            {
                AzimuthRate = 0.01f,
                ElevationRate = 0.01f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI / 4,
                MinElevation = (float)-Math.PI / 4,
                IdlePower = 0,
                ShotInaccuracy = 0f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading
            {
                Ammos = new[]
                {
                    DeltaVAmmoRailgun.Name
                },

                RateOfFire = 1,
                RateOfFireVariance = 0f,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 6,
                DelayUntilFire = 0,
                MagazinesToLoad = 3,

                MaxReloads = -1,

                //Resources = new[]
                //{
                //    new Loading.Resource
                //    {
                //        ResourceType = "Heat",
                //        ResourceGeneration = 5,     //fug this doesn't work yet
                //        ResourceStorage = 100, 
                //        ResourcePerShot = 1,
                //        MinResourceBeforeFire = 10 
                //    }
                //},
            },
            Audio = new Audio
            {
                PreShootSound = "",
                ShootSound = "",
                ReloadSound = "PunisherNewReload",
                RotationSound = "WepTurretGatlingRotate"
            },
            Visuals = new Visuals
            {
                ShootParticle = "Muzzle_Flash_Autocannon",
                ContinuousShootParticle = false,
                ReloadParticle = ""
            }
        };
    }
}