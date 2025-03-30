using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace CargoTeleporter
{
    public static class OwnershipUtils
    {

        //Faction
        public static string GetFaction(IMyCubeGrid grid)
        {
            var blocks = grid.GetFatBlocks<IMyCubeBlock>();
            return !blocks.Any() ? "" : blocks.First().GetOwnerFactionTag();
        }

        public static bool IsSameFaction(IMyCubeBlock blockA, IMyCubeBlock blockB)
        {
            return blockA.GetOwnerFactionTag() == blockB.GetOwnerFactionTag();
        }

        public static bool IsSameFaction(IMyCubeGrid gridA, IMyCubeBlock blockB)
        {
            return GetFaction(gridA) == blockB.GetOwnerFactionTag();
        }
        
        public static bool IsSameFaction(IMyCubeGrid gridA, IMyCubeGrid gridB)
        {
            return GetFaction(gridA) == GetFaction(gridB);
        }

        //Ownership
        public static bool IsSameOwner(IMyCubeBlock blockA, IMyCubeBlock blockB)
        {
            return blockA.OwnerId == blockB.OwnerId;
        }

        public static bool IsSameOwner(long playerID, IMyCubeBlock blockB)
        {
            return playerID == blockB.OwnerId;
        }

        public static bool IsSameFactionOrOwner(IMyCubeBlock blockA, IMyCubeBlock blockB)
        {
            return IsSameOwner(blockA, blockB) || IsSameFaction(blockA, blockB);
        }

        internal static bool IsSameFactionOrOwner(IMyCubeGrid gridA, IMyCubeBlock blockB)
        {
            return IsSameFaction(gridA, blockB) || gridA.BigOwners.Any(x => IsSameOwner(x, blockB));
        }
        
        internal static bool IsSameFactionOrOwner(IMyCubeGrid gridA, IMyCubeGrid gridB)
        {
            return IsSameFaction(gridA, gridB) || gridA.BigOwners.Any(x => gridB.BigOwners.Contains(x));
        }
    }
}
