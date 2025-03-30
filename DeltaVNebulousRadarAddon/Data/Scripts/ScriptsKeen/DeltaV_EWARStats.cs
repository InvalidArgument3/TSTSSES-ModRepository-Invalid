using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using NerdRadar.Definitions;
using Sandbox.ModAPI;
using VRageMath;

namespace NerdRadar.DeltaVAddon
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DeltaVEWARstats : MySessionComponentBase
    {
        BlockConfig cfg => new BlockConfig()
        {
            // Feel free to change the Example_EWARStats to something else, along with the namespace (NerdRadar.ExampleMod)
            // Aside from those, do not change anything above this line

            // Stats for all the radar blocks. How stats interact can be found here http://nebfltcom.wikidot.com/mechanics:radar
            // RCS is calculated via (LG blockcount * 0.25)^2*2.5+(SG blockcount * 0.25)^2*0.01+(Generated power in MW) * 0.5
            RadarStats = new Dictionary<string, RadarStat>()
            {
                ["DeltaV_Search_Radar_Large"] = new RadarStat()
                {
                    MaxRadiatedPower = 30000, // 30 MW
                    Gain = 500,
                    Sensitivity = -400,
                    MaxSearchRange = 10000000, // 10,000 km

                    ApertureSize = 300,
                    NoiseFilter = 2,
                    SignalToNoiseRatio = 1,

                    PositionError = 80,
                    VelocityError = 5,

                    CanTargetLock = false,
                    LOSCheckIncludesParentGrid = false,

                    StealthMultiplier = 1,

                    CanDetectAllJumps = true,
                    CanDetectLockedJumps = false,
                },

                ["DeltaV_Track_Radar_Large"] = new RadarStat()
                {
                    MaxRadiatedPower = 5000, // 5 MW
                    Gain = 500,
                    Sensitivity = -400,
                    MaxSearchRange = 250000, // 250 km

                    ApertureSize = 50,
                    NoiseFilter = 1,
                    SignalToNoiseRatio = 1,

                    PositionError = 2,
                    VelocityError = 1,

                    CanTargetLock = true,
                    LOSCheckIncludesParentGrid = false,

                    StealthMultiplier = 1,

                    CanDetectAllJumps = true,
                    CanDetectLockedJumps = true,
                },

                ["DeltaV_Search_Radar_Small"] = new RadarStat()
                {
                    MaxRadiatedPower = 1500, // 1.5 MW
                    Gain = 400,
                    Sensitivity = -300,
                    MaxSearchRange = 500000, // 500 km

                    ApertureSize = 15,
                    NoiseFilter = 2,
                    SignalToNoiseRatio = 1,

                    PositionError = 40,
                    VelocityError = 5,

                    CanTargetLock = false,
                    LOSCheckIncludesParentGrid = false,

                    StealthMultiplier = 1,

                    CanDetectAllJumps = true,
                    CanDetectLockedJumps = false,
                },

                ["DeltaV_Track_Radar_Small"] = new RadarStat()
                {
                    MaxRadiatedPower = 250, // 250 kW
                    Gain = 500,
                    Sensitivity = -400,
                    MaxSearchRange = 50000, // 50 km

                    ApertureSize = 5,
                    NoiseFilter = 1,
                    SignalToNoiseRatio = 1,

                    PositionError = 5,
                    VelocityError = 1,

                    CanTargetLock = true,
                    LOSCheckIncludesParentGrid = false,

                    StealthMultiplier = 1,

                    CanDetectAllJumps = true,
                    CanDetectLockedJumps = true,
                },

            },
            // stats for all the jammer blocks
            // stat interactions can be found here http://nebfltcom.wikidot.com/mechanics:electronic-warfare
            JammerStats = new Dictionary<string, JammerStat>()
            {
                ["DeltaV_LargeJammer"] = new JammerStat()
                {
                    MaxRadiatedPower = 15000, // 15 MW
                    Gain = 150,
                    MaxSearchRange = 500000, // 500 km

                    AreaEffectRatio = 0.4f, // Larger area of effect
                    AngleRadians = MathHelperD.ToRadians(25), // 30-degree cone

                    LOSCheckIncludesParentGrid = true, // Determines whether the jammer can jam through its own grid

                    MaxHeat = 90 * 60, // Maximum heat before shutdown (7.5 minutes)
                    HeatDrainPerTick = 1.5f, // Higher heat dissipation rate
                },

            },

            // IFF beacons are vanilla beacons which when placed on a grid, will replace the ship name given on its radar track with whatever its HUD name is set to.
            IFFBlockStats = new Dictionary<string, IFFBlockStat>()
            {
                ["LargeBlockIFFBeacon"] = new IFFBlockStat()
                {
                    MaxCharacters = 0,
                    ShowClass = false,
                },
                ["SmallBlockIFFBeacon"] = new IFFBlockStat()
                {
                    MaxCharacters = 0,
                    ShowClass = false,
                },
            }
        };
        // Do not touch below here
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(cfg);
            MyAPIGateway.Utilities.SendModMessage(DefConstants.MessageHandlerId, data);
        }
    }
}
