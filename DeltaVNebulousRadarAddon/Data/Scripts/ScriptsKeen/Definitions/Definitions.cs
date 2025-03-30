using ProtoBuf;
using System;
using System.Collections.Generic;
using ShipClass = VRage.MyTuple<int, string>;

namespace NerdRadar.Definitions
{
    public static class DefConstants
    {
        public const long MessageHandlerId = 3244679803;
    }
    /// <summary>
    /// Parent class, ignore.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(600, typeof(ClassConfig))]
    [ProtoInclude(601, typeof(BlockConfig))]
    public class EWARDefinition
    {
    
        public EWARDefinition()
        {
        }
    }
    /// <summary>
    /// Config for classifications.
    /// </summary>
    [ProtoContract]
    public class ClassConfig : EWARDefinition
    {
        [ProtoMember(1)] public bool ReplaceClasses;
        [ProtoMember(2)] public List<ShipClass> StationClasses;
        [ProtoMember(3)] public List<ShipClass> LargeGridClasses;
        [ProtoMember(4)] public List<ShipClass> SmallGridClasses;
    }
    /// <summary>
    /// Config for actual blocks
    /// </summary>
    [ProtoContract]
    public class BlockConfig : EWARDefinition
    {
        [ProtoMember(1)] public Dictionary<string, RadarStat> RadarStats;
        [ProtoMember(2)] public Dictionary<string, JammerStat> JammerStats;
        [ProtoMember(3)] public Dictionary<string, UpgradeBlockStat> UpgradeBlockStats;
        [ProtoMember(4)] public Dictionary<string, IFFBlockStat> IFFBlockStats;
    }
    /// <summary>
    /// Class containing various multipliers and addons to all radars, and the grid RCS its mounted on.
    /// </summary>
    [ProtoContract]
    public class UpgradeBlockStat
    {
        /// <summary>
        /// Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float SensitivityMultiplier = 1;
        /// <summary>
        /// Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Decibels
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float SensitivityAddon = 0;

        /// <summary>
        /// Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float NoiseFilterMultiplier = 1;
        /// <summary>
        /// Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Decibels
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public float NoiseFilterAddon = 0;

        /// <summary>
        /// Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)] public float PositionalErrorMultiplier = 1;
        /// <summary>
        /// Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float PositionalErrorAddon = 0;

        /// <summary>
        /// Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public float VelocityErrorMultiplier = 1;
        /// <summary>
        /// Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters per second
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float VelocityErrorAddon = 0;

        /// <summary>
        /// RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(9)] public float RCSMultiplier = 1;
        /// <summary>
        /// RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters per second
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(10)] public float RCSAddon = 0;

        /// <summary>
        /// Make multipliers apply only if the block is functional and on.
        /// <br>Mutually exclusive with ApplyOnlyWhenFiring</br>
        /// </summary>
        [ProtoMember(11)] public bool ApplyOnlyWhenOn = false;

        /// <summary>
        /// Make multipliers apply only if the block is a weapon and is firing. :)
        /// <br>Mutually exclusive with ApplyOnlywhenOn</br>
        /// </summary>
        [ProtoMember(12)] public bool ApplyOnlyWhenFiring = false;
    }
    /// <summary>
    /// Class containing all the stats for radar. Most stats can be found here: http://nebfltcom.wikidot.com/mechanics:radar
    /// </summary>
    [ProtoContract]
    public class RadarStat
    {
        /// <summary>
        /// Maximum radiated power of the radar.
        /// <para>
        /// Units: Kilowatts (kW)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float MaxRadiatedPower;
        /// <summary>
        /// Gain of the radar system.
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float Gain;
        /// <summary>
        /// Maximum possible search range of the radar. Radar will not detect any targets past this range, even if they could otherwise.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float MaxSearchRange;
        /// <summary>
        /// Aperture Size of the radar.
        /// <para>
        /// Units: Meters Squared (m^2)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public float ApertureSize;
        /// <summary>
        /// Noise filtering of the radar system. lower is better.<list type="bullet">
        /// </list>
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public double NoiseFilter;
        /// <summary>
        /// Minimum required ratio of returned signal to noise; 
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float SignalToNoiseRatio;
        /// <summary>
        /// Sensitivity of the radar
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public double Sensitivity;

        /// <summary>
        /// Position error of the radar. Note, position and velocity error of 0 designates locked targets, regardless of whether the target is actually locked.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float PositionError;
        /// <summary>
        /// Velocity error of the radar. Does nothing currently, come back later when I figure out how to draw arbitrary lines on screen. Note, position and velocity error of 0 designates locked targets, regardless of whether the target is actually locked.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(9)] public float VelocityError;

        /// <summary>
        /// Determines whether or not the radar system can target lock
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(10)] public bool CanTargetLock;
        /// <summary>
        /// Determines whether or not the radar system needs to be externally mounted by making the LOS check fail if the parent grid is in the way.
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(11)] public bool LOSCheckIncludesParentGrid;

        /// <summary>
        /// Multiplier for the RCS of a target grid in stealth from the mod https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069 (Stealth Drive by Ash Like Snow)
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(12)] public float StealthMultiplier;

        /// <summary>
        /// Determines if the radar can detect and show any jumps caused by any track visible to the radar.
        /// <br>Mutually exclusive with CanDetectLockedJumps</br>
        /// </summary>
        [ProtoMember(13)] public bool CanDetectAllJumps;

        /// <summary>
        /// Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.
        /// <br>Mutually exclusive with CanDetectAllJumps</br>
        /// </summary>
        [ProtoMember(14)] public bool CanDetectLockedJumps;

        public RadarStat()
        {
        }
    }
    /// <summary>
    /// Class containing all the stats for jammers.
    /// </summary>
    [ProtoContract]
    public class JammerStat
    {
        /// <summary>
        /// Maximum radiated power of the jammer turret.
        /// <para>
        /// Units: Kilowatts (kW)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float MaxRadiatedPower;
        /// <summary>
        /// Gain of the jammer turret.
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float Gain;
        /// <summary>
        /// Maximum possible search range of the jammer. The jammer will only affect radars within this range.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float MaxSearchRange;
        /// <summary>
        /// Determines whether or not the jammer system needs to be externally mounted by failing if the parent grid is in the way.
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public bool LOSCheckIncludesParentGrid;
        /// <summary>
        /// Determines whether or not the jammer system needs to be externally mounted by failing if the parent grid is in the way.
        /// <para>
        /// Units: Radians (Use <c>MathHelperD.ToRadians([value]),</c> in place of a number to set value in degrees)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)] public double AngleRadians;
        /// <summary>
        /// Area effect ratio of the jammer. Best explained here http://nebfltcom.wikidot.com/mechanics:electronic-warfare, although here it is a cylinder rather than a rectanglular prism.
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float AreaEffectRatio;
        /// <summary>
        /// Maximum heat buildup before the jammer automatically turns off. Set to -1 to disable. Jammers will gain 1 heat per tick no matter what.
        /// <para>
        /// Units: Proprietary Heat Measurement Unit™
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c> or <c>-1</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public float MaxHeat;
        /// <summary>
        /// Amount of heat dissapated every tick the jammer is off. 
        /// <para>
        /// Units: Proprietary Heat Measurement Unit™ per tick
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float HeatDrainPerTick;

        public JammerStat()
        {
        }
    }
    /// <summary>
    /// Class for containing all the stats for an IFF block - a beacon which will overwrite the default name listed on its radar track.
    /// </summary>
    [ProtoContract]
    public class IFFBlockStat
    {
        /// <summary>
        /// Maximum characters the IFF beacon is allowed to render on the radar track name. Set to zero to disable.
        /// </summary>
        [ProtoMember(1)] public int MaxCharacters;
        /// <summary>
        /// Whether or not to include the classification on the IFF name. 
        /// </summary>
        [ProtoMember(2)] public bool ShowClass;
    }
}