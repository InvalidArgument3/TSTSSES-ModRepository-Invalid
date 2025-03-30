using System.Collections.Generic;
using DeltaVEquipment.ProjectileBases;
using DeltaVEquipment.WeaponBases;
using ProtoBuf;
using Sandbox.ModAPI;

namespace DeltaVEquipment
{
    partial class HeartDefinitions
    {
        internal DefinitionContainer Container = new DefinitionContainer();

        internal void LoadWeaponDefinitions(params WeaponDefinitionBase[] defs)
        {
            List<byte[]> serialWeapons = new List<byte[]>();
            foreach (var weapon in defs)
                serialWeapons.Add(MyAPIGateway.Utilities.SerializeToBinary(weapon));
            Container.SerializedWeaponDefs = serialWeapons.ToArray();
            Container.WeaponDefs = defs;
        }
        internal void LoadAmmoDefinitions(params ProjectileDefinitionBase[] defs)
        {
            List<byte[]> serialAmmos = new List<byte[]>();
            foreach (var ammo in defs)
                serialAmmos.Add(MyAPIGateway.Utilities.SerializeToBinary(ammo));
            Container.SerializedAmmoDefs = serialAmmos.ToArray();
            Container.AmmoDefs = defs;
        }

        /// <summary>
        /// Load all definitions for DefinitionSender
        /// </summary>
        /// <param name="baseDefs"></param>
        internal static DefinitionContainer GetBaseDefinitions()
        {
            return new HeartDefinitions().Container;
        }
    }

    [ProtoContract]
    internal class DefinitionContainer
    {
        [ProtoMember(1)]
        public byte[][] SerializedWeaponDefs { get; set; }
        [ProtoMember(2)]
        public byte[][] SerializedAmmoDefs { get; set; }

        public WeaponDefinitionBase[] WeaponDefs { get; set; }
        public ProjectileDefinitionBase[] AmmoDefs { get; set; }
    }
}
