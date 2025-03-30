namespace DeltaVEquipment
{
    partial class HeartDefinitions
    {
        internal HeartDefinitions()
        {
            LoadWeaponDefinitions(DeltaVMiningLaserTurret, DeltaVRailgunFixed, DeltaVMiningMetalStorm);         //todo tell the user that they forgot to add stuff here when they get an error
            LoadAmmoDefinitions(DeltaVMiningLaserAmmoBeam, DeltaVAmmoRailgun, DeltaVMiningMetalStormProjectile);
        }
    }
}
