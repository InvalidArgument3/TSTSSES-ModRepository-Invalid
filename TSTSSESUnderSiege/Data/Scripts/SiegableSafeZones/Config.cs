using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.GameServices;
using VRageMath;
using static VRage.Game.MyObjectBuilder_EnvironmentDefinition;

namespace SiegableSafeZones
{
    [ProtoContract]
    public class Config
    {
        [ProtoMember(1)] [XmlElement("SafeZoneRadius")] public float _safeZoneRadius;
        [ProtoMember(2)] [XmlElement("SafeZoneStartupTimeSeconds")] public int _safeZoneStartupTime;
        [ProtoMember(3)] [XmlElement("SafeZoneUpKeepConfig")] public SafeZoneUpKeep _safeZoneUpKeep;
        [ProtoMember(4)] [XmlElement("ActivePowerDrainMW")] public float _activePowerDrain;
        [ProtoMember(5)] [XmlElement("RechargeRatePerMinute")] public float _rechargeRate;
        [ProtoMember(6)] [XmlElement("DrainRatePerMinute")] public float _drainRate;
        [ProtoMember(7)] [XmlElement("ActivationInitialCharge")] public float _initCharge;
        [ProtoMember(8)] [XmlElement("DiscordRoleId")] public long _discordRoleId;
        [ProtoMember(9)] [XmlElement("DiscordRoleName")] public string _discordRoleName;
        [ProtoMember(10)] [XmlElement("AllowSiegingOffline")] public bool _allowingSiegingOffline;
        [ProtoMember(11)] [XmlElement("EnemyCheckBeforeStartup")] public EnemyCheck _enemyCheckStartup;
        [ProtoMember(12)] [XmlElement("SiegingConfig")] public SiegingConfig _siegeConfig;
        
        public static Config LoadConfig()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("SiegableSafeZoneConfig.xml", typeof(Config)) == true)
            {
                try
                {
                    Config defaults = new Config();
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("SiegableSafeZoneConfig.xml", typeof(Config));
                    string content = reader.ReadToEnd();

                    reader.Close();
                    defaults = MyAPIGateway.Utilities.SerializeFromXML<Config>(content);
                    if (defaults == null)
                        return CreateNewFile();

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
            UnsiegableAreas area = new UnsiegableAreas()
            {
                _enableArea = false,
                _areaCenter = new Vector3D(0, 0, 0),
                _areaRadius = 20000
            };

            Config file = new Config
            {
                _safeZoneRadius = 200,
                _safeZoneStartupTime = 60,
                _activePowerDrain = 300,
                _rechargeRate = 1f,
                _drainRate = 3.3f,
                _allowingSiegingOffline = false,
                _initCharge = 1,

                _enemyCheckStartup = new EnemyCheck()
                {
                    _enableEnemyChecks = true,
                    _enemyCheckDistance = 1000,
                    _omitNPCs = true,
                    _alliesFriendly = true
                },

                _safeZoneUpKeep = new SafeZoneUpKeep()
                {
                    _upKeepItemDef = "MyObjectBuilder_Component/ZoneChip",
                    _upKeepAmt = 1,
                    _upKeepTime = 60
                },

                _siegeConfig = new SiegingConfig()
                {
                    _siegeConsumptionItem = "MyObjectBuilder_Component/ZoneChip",
                    _siegeConsumptionAmt = -1,
                    _siegeRange = 3000,
                    _siegeAllies = false,
                    _unsiegableAreas = new UnsiegableAreas[] { area }
                },

                _discordRoleId = 000000000,
                _discordRoleName = "NAME"
            };

            SaveConfig(file);
            return file;
        }

        public static void SaveConfig(Config config)
        {
            if (config == null) return;
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("SiegableSafeZoneConfig.xml", typeof(Config)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(config));
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                VRage.Utils.MyLog.Default.WriteLineAndConsole($"SiegableSafeZone: Error trying to save config!\n {ex.ToString()}");
            }
        }

        [ProtoContract]
        public struct SafeZoneUpKeep
        {
            [ProtoMember(1)] [XmlElement("UpKeepItem")] public string _upKeepItemDef;
            [ProtoMember(3)] [XmlElement("SafeZoneUpKeepAmt")] public int _upKeepAmt;
            [ProtoMember(4)] [XmlElement("SafeZoneUpKeepTimeSeconds")] public int _upKeepTime; 
        }

        [ProtoContract]
        public struct SiegingConfig
        {
            [ProtoMember(1)] [XmlElement("SiegeConsumptionItem")] public string _siegeConsumptionItem;
            [ProtoMember(2)] [XmlElement("SiegeConsumptionAmt")] public int _siegeConsumptionAmt;
            [ProtoMember(3)] [XmlElement("SiegeMaxRange")] public int _siegeRange;
            [ProtoMember(4)] [XmlElement("CanAlliesBeSieged")] public bool _siegeAllies;
            [ProtoMember(5)] [XmlElement("UnsiegableAreas")] public UnsiegableAreas[] _unsiegableAreas;
        }

        [ProtoContract]
        public struct EnemyCheck
        {
            [ProtoMember(1)] [XmlElement("EnableEnemyChecks")] public bool _enableEnemyChecks;
            [ProtoMember(2)] [XmlElement("EnemyCheckDistance")] public float _enemyCheckDistance;
            [ProtoMember(3)] [XmlElement("OmitNPCs")] public bool _omitNPCs;
            [ProtoMember(4)] [XmlElement("AlliesAreFriendly")] public bool _alliesFriendly;
        }

        [ProtoContract]
        public struct UnsiegableAreas
        {
            [ProtoMember(1)][XmlElement("EnableArea")] public bool _enableArea;
            [ProtoMember(2)][XmlElement("AreaCenter")] public Vector3D _areaCenter;
            [ProtoMember(3)][XmlElement("AreaRadius")] public float _areaRadius;
        }
    }
}
