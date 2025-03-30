using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRageMath;

namespace CustomHangar
{
    [ProtoContract]
    public class Config
    {
        [ProtoIgnore][XmlIgnore] public string version = "1.00";
        [ProtoMember(1)][XmlElement("AutoHangarConfig")] public AutoHangarConfig autoHangarConfig;
        [ProtoMember(2)][XmlElement("FactionHangarConfig")] public FactionHangarConfig factionHangarConfig;
        [ProtoMember(3)][XmlElement("PrivateHangarConfig")] public PrivateHangarConfig privateHangarConfig;
        [ProtoMember(4)][XmlElement("SpawningConfigLimits")] public SpawnConfigLimits spawnConfig;
        [ProtoMember(5)][XmlElement("SetSmallGridsStaticOnSpawn")] public bool spawnSGStatic;
        [ProtoMember(6)][XmlElement("SpawnNearbyConfig")] public SpawnNearbyConfig spawnNearbyConfig;
        [ProtoMember(7)][XmlElement("DynamicSpawningConfig")] public DynamicSpawning dynamicSpawningConfig;
        [ProtoMember(8)][XmlElement("OriginalSpawningConfig")] public SpawnOriginalConfig spawnOriginalConfig;
        [ProtoMember(9)][XmlElement("EnemyCheckSettings")] public EnemyCheckConfig enemyCheckConfig;
        [ProtoMember(10)][XmlElement("SpawnAreas")] public SpawnAreas[] spawnAreas;

        public static Config LoadConfig()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("!FactionHangarConfig.xml", typeof(Config)) == true)
            {
                try
                {
                    Config defaults = new Config();
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("!FactionHangarConfig.xml", typeof(Config));
                    string content = reader.ReadToEnd();

                    reader.Close();
                    defaults = MyAPIGateway.Utilities.SerializeFromXML<Config>(content);
                    if (defaults == null)
                        return CreateNewFile();

                    //if (settings.version == defaults.version)
                        //eturn settings;

                    //settings.version = defaults.version;
                    SaveConfig(defaults);
                    return defaults;
                }
                catch (Exception ex)
                {
                    return CreateNewFile();
                }
            }

            return CreateNewFile();
        }

        public static Config CreateNewFile()
        {
            Config defaults = new Config();

            BlockType blocktype = new BlockType()
            {
                blockType = "Type",
                blockSubtypes = new BlockSubtypes()
                {
                    subtype = new string[] { "Subtype", "Subtype" }
                }
            };
            defaults.autoHangarConfig = new AutoHangarConfig()
            {
                enableAutoHangar = false,
                daysFactionLogin = 8,
                autoBypassSpawnLimits = true,
                autoBypassSpawnCost = true,
                exclusions = new Exclusions()
                {
                    excludedBlockTypes = new BlockType[] { blocktype },
                    excludedFactions = new ExcludedFactions()
                    {
                        excludedFaction = new string[] { "FACTIONTAG" }
                    }
                }
            };
            var list = defaults.autoHangarConfig.exclusions.excludedBlockTypes.ToList();
            list.Add(blocktype);
            defaults.autoHangarConfig.exclusions.excludedBlockTypes = list.ToArray();

            var list2 = defaults.autoHangarConfig.exclusions.excludedFactions.excludedFaction.ToList();
            list2.Add("FACTIONTAG");
            defaults.autoHangarConfig.exclusions.excludedFactions.excludedFaction = list2.ToArray();

            defaults.factionHangarConfig = new FactionHangarConfig()
            {
                maxFactionSlots = 20,
                factionRetrievalCooldown = 60,
                factionHangarCooldown = 60,
                factionStoreDelay = 60,
                factionToPrivateTransfer = true,
                factionHangarEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = true,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                }
            };

            defaults.privateHangarConfig = new PrivateHangarConfig()
            {
                maxPrivateSlots = 5,
                privateRetrievalCooldown = 60,
                privateHangarCooldown = 60,
                privateStoreDelay = 60,
                privateToFactionTransfer = true,
                privateHangarEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = true,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                }   
            };

            defaults.spawnConfig = new SpawnConfigLimits()
            {
                removeUranium = false,
                removeIce = false,
                removeAmmo = false,
                h2Percentage = -1,
                batteryPercentage = -1,
            };

            defaults.spawnNearbyConfig = new SpawnNearbyConfig()
            {
                allowSpawnNearby = true,
                nearbyRadius = 50,
                nearbyEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = true,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                },

                nearbySpawnBypass = true,
                nearbySpawnCost = 0
            };

            defaults.dynamicSpawningConfig = new DynamicSpawning()
            {
                enableDynamicSpawning = true,
                costMultiplier = 0.0001f,
                dynamicEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = true,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                },

                dynamicSpawnBypass = true, 
            };

            defaults.spawnOriginalConfig = new SpawnOriginalConfig()
            {
                allowSpawnOriginal = true,
                originalEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = true,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                },

                originalSpawnBypass = true,
                originalSpawnCost = 0
            };

            BlockType blockConfig = new BlockType()
            {
                blockType = "Beacon",
                blockSubtypes = new BlockSubtypes()
                {
                    subtype = new string[] { "LargeBlockBeacon", "SmallBlockBeacon" }
                }
            };

            defaults.enemyCheckConfig = new EnemyCheckConfig()
            {
                enableBlockCheck = false,
                blockTypes = new BlockType[] { blockConfig },
            };
            var list3 = defaults.enemyCheckConfig.blockTypes.ToList();
            list3.Add(blockConfig);
            defaults.enemyCheckConfig.blockTypes = list3.ToArray();

            defaults.spawnSGStatic = false;

            SpawnAreas spawnArea = new SpawnAreas()
            {
                enableSpawnArea = false,
                areaCenter = new Vector3D(0, 0, 0),
                areaRadius = 7500,
                inverseArea = false,
                spawnAreaBypass = true,
                spawnAreaCost = 0,
                spawnAreasEnemyCheck = new EnemyChecks()
                {
                    checkEnemiesNearby = false,
                    enemyDistanceCheck = 1500,
                    alliesFriendly = true,
                    omitNPCs = true
                }
            };
            defaults.spawnAreas = new SpawnAreas[] { spawnArea };

            SaveConfig(defaults);
            return defaults;
        }

        public static void SaveConfig(Config config)
        {
            if (config == null) return;
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("!FactionHangarConfig.xml", typeof(Config)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(config));
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                VRage.Utils.MyLog.Default.WriteLineAndConsole($"FactionHangar: Error trying to save config!\n {ex.ToString()}");
            }
        }
    }

    [ProtoContract]
    public struct AutoHangarConfig
    {
        [ProtoMember(1)][XmlElement("EnableAutoHangar")] public bool enableAutoHangar;
        [ProtoMember(2)][XmlElement("DaysSinceFactionLogin")] public float daysFactionLogin;
        [ProtoMember(3)][XmlElement("BypassSpawnConfigLimits")] public bool autoBypassSpawnLimits;
        [ProtoMember(4)][XmlElement("BypassSpawningCost")] public bool autoBypassSpawnCost;
        [ProtoMember(5)][XmlElement("AutoHangarExclusions")] public Exclusions exclusions;

        //public AutoHangarConfig() { }
    }

    [ProtoContract]
    public struct FactionHangarConfig
    {
        [ProtoMember(1)][XmlElement("MaxFactionHangarSlots")] public int maxFactionSlots;
        [ProtoMember(2)][XmlElement("FactionRetrievalCooldown")] public int factionRetrievalCooldown;
        [ProtoMember(3)][XmlElement("FactionHangarCooldown")] public int factionHangarCooldown;
        [ProtoMember(4)][XmlElement("StorageDelayInSeconds")] public int factionStoreDelay;
        [ProtoMember(5)][XmlElement("AllowFactionToPrivateTransfer")] public bool factionToPrivateTransfer;
        [ProtoMember(6)][XmlElement("EnemyCheckConfig")] public EnemyChecks factionHangarEnemyCheck;
        //[ProtoMember(5)][XmlElement("FactionLeadersObtainAllOwnership")] public bool factionLeadersObtainAllOwnership = false;

        //public FactionHangarConfig() { }
    }

    [ProtoContract]
    public struct PrivateHangarConfig
    {
        [ProtoMember(1)][XmlElement("MaxPrivateHangarSlots")] public int maxPrivateSlots;
        [ProtoMember(2)][XmlElement("PrivateRetrievalCooldown")] public int privateRetrievalCooldown;
        [ProtoMember(3)][XmlElement("PrivateHangarCooldown")] public int privateHangarCooldown;
        [ProtoMember(4)][XmlElement("StorageDelayInSeconds")] public int privateStoreDelay;
        [ProtoMember(5)][XmlElement("AllowPrivateToFactionTransfer")] public bool privateToFactionTransfer;
        [ProtoMember(6)][XmlElement("EnemyCheckConfig")] public EnemyChecks privateHangarEnemyCheck;

        //public PrivateHangarConfig() { }
    }

    [ProtoContract]
    public struct DynamicSpawning
    {
        [ProtoMember(1)][XmlElement("AllowDynamicSpawning")] public bool enableDynamicSpawning;
        [ProtoMember(2)][XmlElement("CostMultiplier")] public float costMultiplier;
        [ProtoMember(3)][XmlElement("EnemyCheckConfig")] public EnemyChecks dynamicEnemyCheck;
        [ProtoMember(4)][XmlElement("BypassSpawnConfigLimits")] public bool dynamicSpawnBypass;
    }

    [ProtoContract]
    public struct SpawnConfigLimits
    {
        [ProtoMember(1)][XmlElement("RemoveUranium")] public bool removeUranium;
        [ProtoMember(2)][XmlElement("RemoveIce")] public bool removeIce;
        [ProtoMember(3)][XmlElement("RemoveAmmo")] public bool removeAmmo;
        [ProtoMember(4)][XmlElement("SetH2Percentage")] public float h2Percentage;
        [ProtoMember(5)][XmlElement("SetBatteryPercentage")] public float batteryPercentage;
    }

    [ProtoContract]

    public struct SpawnAreas
    {
        [ProtoMember(1)][XmlElement("EnableSpawnArea")] public bool enableSpawnArea;
        [ProtoMember(2)][XmlElement("AreaCenterLocation")] public Vector3D areaCenter;
        [ProtoMember(3)][XmlElement("AreaRadius")] public float areaRadius;
        [ProtoMember(4)][XmlElement("InverseArea")] public bool inverseArea;
        [ProtoMember(5)][XmlElement("EnemyCheckConfig")] public EnemyChecks spawnAreasEnemyCheck;
        [ProtoMember(6)][XmlElement("BypassSpawnConfigLimits")] public bool spawnAreaBypass;
        [ProtoMember(7)][XmlElement("CostToSpawn")] public long spawnAreaCost;

    }

    [ProtoContract]

    public struct SpawnNearbyConfig
    {
        [ProtoMember(1)][XmlElement("AllowSpawningNearbyOriginal")] public bool allowSpawnNearby;
        [ProtoMember(2)][XmlElement("NearbyRadius")] public float nearbyRadius;
        [ProtoMember(3)][XmlElement("EnemyCheckConfig")] public EnemyChecks nearbyEnemyCheck;
        [ProtoMember(4)][XmlElement("BypassSpawnConfigLimits")] public bool nearbySpawnBypass;
        [ProtoMember(5)][XmlElement("CostToSpawn")] public long nearbySpawnCost;
    }

    [ProtoContract]
    public struct SpawnOriginalConfig
    {
        [ProtoMember(1)][XmlElement("AllowSpawnAtOriginalLocation")] public bool allowSpawnOriginal;
        [ProtoMember(2)][XmlElement("EnemyCheckConfig")] public EnemyChecks originalEnemyCheck;
        [ProtoMember(3)][XmlElement("BypassSpawnConfigLimits")] public bool originalSpawnBypass;
        [ProtoMember(4)][XmlElement("CostToSpawn")] public long originalSpawnCost;
    }

    [ProtoContract]
    public struct EnemyChecks
    {
        [ProtoMember(1)][XmlElement("CheckEnemiesNearby")] public bool checkEnemiesNearby;
        [ProtoMember(2)][XmlElement("EnemyDistanceCheck")] public float enemyDistanceCheck;
        [ProtoMember(3)][XmlElement("AlliesAreFriendly")] public bool alliesFriendly;
        [ProtoMember(4)][XmlElement("OmitNPCs")] public bool omitNPCs;
    }

    [ProtoContract]
    public struct EnemyCheckConfig
    {
        [ProtoMember(1)][XmlElement("EnableBlockChecking")] public bool enableBlockCheck;
        [ProtoMember(2)][XmlElement("BlockTypes")] public BlockType[] blockTypes;
    }

    [ProtoContract]
    public struct BlockType
    {
        [ProtoMember(1)][XmlElement("BlockType")] public string blockType;
        [ProtoMember(2)][XmlElement("BlockSubtypes")] public BlockSubtypes blockSubtypes;
    }

    [ProtoContract]
    public struct Exclusions
    {
        [ProtoMember(1)][XmlElement("ExcludedBlockTypes")] public BlockType[] excludedBlockTypes;
        [ProtoMember(2)][XmlElement("ExcludedFactions")] public ExcludedFactions excludedFactions;
    }

    [ProtoContract]
    public struct ExcludedFactions
    {
        [ProtoMember(1)][XmlElement("ExcludedFaction")] public string[] excludedFaction;
    }

    [ProtoContract]
    public struct BlockSubtypes
    {
        [ProtoMember(1)][XmlElement("BlockSubtype")] public string[] subtype;
    }
}