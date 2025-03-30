using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Definitions.SafeZone;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace SiegableSafeZones
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public static Session Instance;
        public readonly ushort NetworkId = 8237;
        public bool controlsInitZone;
        public bool controlsInitJumpDrive;
        public Config config;
        private Guid cpmID = new Guid("C2336615-5126-21E3-2E32-31D631F3C4A2");
        public int ticks;
        public bool isServer;
        public bool isDedicated;
        public readonly string ConfigToSandboxVariable = "SiegableSafeZoneConfig";


        // TODO
        // Save settings to block
        // Save configs to xml
        // Consume items on sieging
        // Add notifications
        // TSS

        public Dictionary<long, ZoneBlockSettings> zoneBlockSettingsCache = new Dictionary<long, ZoneBlockSettings>();

        public override void LoadData()
        {
            Instance = this;
            isServer = MyAPIGateway.Session.IsServer;
            isDedicated = MyAPIGateway.Utilities.IsDedicated;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(NetworkId, Comms.MessageHandler);

            if (isServer)
            {
                config = Config.LoadConfig();
                Utils.SaveConfigToSandbox(config);
                MySessionComponentSafeZones.OnSafeZoneUpdated += OnSafeZoneUpdated;
                //MyAPIGateway.Entities.OnEntityAdd += EntityAdd;
                MyAPIGateway.Entities.OnEntityRemove += EntityRemoved;
            }
            else
                //Comms.RequestConfig(MyAPIGateway.Multiplayer.MyId);
                config = Utils.LoadConfigFromSandbox();
        }

        public override void BeforeStart()
        {
            if (!isServer)
                Comms.ClientRequestBlockSettings(MyAPIGateway.Multiplayer.MyId);

            if (config == null)
            {
                //MyVisualScriptLogicProvider.ShowNotification("Config is null", 30000);
                MyLog.Default.WriteLineAndConsole("SiegableSafeZones: BeforeStart BlockSettings is null!!");
                return;
            }

            var allDefs = MyDefinitionManager.Static.GetAllDefinitions();
            foreach (var def in allDefs.Where(x => x as MySafeZoneBlockDefinition != null))
            {
                var zone = def as MySafeZoneBlockDefinition;
                zone.DefaultSafeZoneRadius = config._safeZoneRadius;
                zone.MaxSafeZoneRadius = config._safeZoneRadius;
                zone.MaxSafeZonePowerDrainkW = config._activePowerDrain * 1000;
                zone.SafeZoneUpkeep = (uint)config._safeZoneUpKeep._upKeepAmt;
                zone.SafeZoneUpkeepTimeM = (uint)config._safeZoneUpKeep._upKeepTime;
                zone.SafeZoneActivationTimeS = (uint)config._safeZoneStartupTime;

                break;
            }


        }

        public override void UpdateBeforeSimulation()
        {
            ticks++;
            // Runs every tick
            RunParticles();

            // Runs every 5 ticks
            CheckSync();

            // Runs every 60 ticks(1 sec)
            RunTimers();
        }

        private void RunTimers()
        {
            if (ticks % 60 != 0) return;
            ticks = 0;
            if (!isServer) return;

            foreach (var settings in zoneBlockSettingsCache.Values)
            {
                if (settings.IsActive && !settings.IsSieging) Utils.ChargeShield(settings);
                if (settings.IsActive && settings.IsSieging) Utils.DrainShield(settings);
            }
        }

        private void CheckSync()
        {
            if (ticks % 5 != 0) return;
            foreach (var settings in zoneBlockSettingsCache.Values)
            {
                if (settings.Sync)
                {
                    Comms.SyncSettings(settings);
                    settings.Sync = false;

                    if (isServer)
                        SaveSafeZoneSettings(settings);
                }
            }
        }

        private void RunParticles()
        {
            if (isServer && isDedicated) return;

            foreach (var item in zoneBlockSettingsCache.Values)
            {
                if (item.IsSieging) DrawLine(item, new Vector4(1, 0, 0, 1));
            }
        }

        private void DrawLine(ZoneBlockSettings settings, Vector4 color)
        {
            //if (settings.Timer <= 2) return;
            IMyPlayer client = MyAPIGateway.Session.LocalHumanPlayer;
            if (Vector3D.Distance(client.GetPosition(), settings.ZoneBlockPos) > config._siegeConfig._siegeRange + 2000) return;

            if (settings.IsSieging && (settings.JDBlock == null || settings.JDBlock.MarkedForClose))
            {
                IMyEntity entitySiege = null;
                if (settings.JDSiegingId != 0 && MyAPIGateway.Entities.TryGetEntityById(settings.JDSiegingId, out entitySiege))
                    settings.JDBlock = entitySiege as IMyTerminalBlock;
                else
                    return;
            }

            IMyEntity fromEntity = settings.JDBlock;
            if (fromEntity == null) return;

            IMyTerminalBlock jd = fromEntity as IMyTerminalBlock;
            if (jd == null) return;
            if (!jd.IsFunctional) return;
            

            float beamRadius = Utils.RandomFloat(0.1f, 0.8f);
            Vector4 beamColor = color;
            Vector3D fromCoords = fromEntity.GetPosition();
            Vector3D toCoords = settings.ZoneBlockPos;
            MySimpleObjectDraw.DrawLine(fromCoords, toCoords, MyStringId.GetOrCompute("WeaponLaser"), ref beamColor, beamRadius);

            /*if (ticks != 0)
            {
                if (ticks % 140 != 0 || settings.Timer < 3) return;
            }*/

            /*MatrixD hitParticleMatrix = MatrixD.CreateWorld(toCoords, Vector3.Forward, Vector3.Up);

            MyParticleEffect effect = null;
            if (settings.IsSieging)
                MyParticlesManager.TryCreateParticleEffect("Sieging", ref hitParticleMatrix, ref toCoords, uint.MaxValue, out effect);

            if (effect == null) return;
            effect.UserScale = 10f;*/
        }

        public void LoadSafeZoneSettings(IMySafeZoneBlock block, bool _isServer)
        {
            try
            {
                
                if (_isServer)
                {
                    ZoneBlockSettings data = new ZoneBlockSettings(block.EntityId);
                    if (block.Storage != null)
                    {
                        byte[] byteData;

                        string storage = block.Storage[cpmID];
                        byteData = Convert.FromBase64String(storage);
                        data = MyAPIGateway.Utilities.SerializeFromBinary<ZoneBlockSettings>(byteData);
                    }

                    IMyEntity jd;
                    data.Block = block;
                    //data.ZoneBlockEntityId = block.EntityId;

                    if (data.JDSiegingId != 0)
                    {
                        if (MyAPIGateway.Entities.TryGetEntityById(data.JDSiegingId, out jd))
                            data.JDBlock = jd as IMyTerminalBlock;
                    }

                    zoneBlockSettingsCache.Add(block.EntityId, data);
                    //if (isServer && !isDedicated)
                        //block.AppendingCustomInfo += UpdateCustomInfo;

                    Comms.SendBlockSettingsToClients(data);
                    return;
                }

                //block.AppendingCustomInfo += UpdateCustomInfo;
                ZoneBlockSettings settings = new ZoneBlockSettings();
                if (!zoneBlockSettingsCache.TryGetValue(block.EntityId, out settings)) return;

                IMyEntity jd2;
                if (settings.JDSiegingId != 0)
                {
                    if (MyAPIGateway.Entities.TryGetEntityById(settings.JDSiegingId, out jd2))
                        settings.JDBlock = jd2 as IMyTerminalBlock;
                }

                settings.Block = block;
            }
            catch (Exception ex)
            {
                ZoneBlockSettings data = new ZoneBlockSettings(block.EntityId)
                {
                    Block = block
                };

                if (!zoneBlockSettingsCache.ContainsKey(block.EntityId))
                    zoneBlockSettingsCache.Add(block.EntityId, data);
            }
        }

        public void SaveSafeZoneSettings(ZoneBlockSettings settings)
        {
            if (!isServer) return;

            IMyEntity entity = settings.Block;
            if (entity == null) return;

            if (entity.Storage != null)
            {
                var newByteData = MyAPIGateway.Utilities.SerializeToBinary(settings);
                var base64string = Convert.ToBase64String(newByteData);
                entity.Storage[cpmID] = base64string;
            }
            else
            {
                entity.Storage = new MyModStorageComponent();
                var newByteData = MyAPIGateway.Utilities.SerializeToBinary(settings);
                var base64string = Convert.ToBase64String(newByteData);
                entity.Storage[cpmID] = base64string;
            }
        }

        public void UpdateCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            if (block as IMySafeZoneBlock != null)
            {
                ZoneBlockSettings data;
                zoneBlockSettingsCache.TryGetValue(block.EntityId, out data);
                if (data == null) return;

                //sb.Clear();
                sb.Append(data.DetailInfo);
            }

            if (block as IMyJumpDrive != null)
            {
                //ZoneBlockSettings data2;
                //zoneBlockSettingsCache.TryGetValue(ActionControls.selectedSafeZoneId, out data2);
                //if (data2 == null) return;
                //sb.Clear();
                sb.Append(Controls.sb.ToString());
                //MyVisualScriptLogicProvider.ShowNotification("Got JD Detail Info", 10000);
                //sb.Append(new StringBuilder(Controls.text));
            }
        }

        public void InitControls(IMyTerminalBlock block)
        {
            IMySafeZoneBlock zone = block as IMySafeZoneBlock;
            IMyJumpDrive drive = block as IMyJumpDrive;

            block.AppendingCustomInfo += UpdateCustomInfo;
            if (zone != null && !controlsInitZone)
            {
                MyAPIGateway.TerminalControls.CustomControlGetter += Controls.CreateZoneControls;
                MyAPIGateway.TerminalControls.CustomActionGetter += Controls.CreateZoneActions;
                controlsInitZone = true;
                return;
            }

            if (drive != null && !controlsInitJumpDrive)
            {
                MyAPIGateway.TerminalControls.CustomControlGetter += Controls.CreateJumpDriveControls;
                controlsInitJumpDrive = true;
                return;
            }  
        }

        private void OnSafeZoneUpdated(MySafeZone zone)
        {
            if (zone.SafeZoneBlockId == 0) return;
            if (zone.Enabled)
                Utils.ActivateCharge(zone.SafeZoneBlockId);
            else
                Utils.DeactiveCharge(zone.SafeZoneBlockId);

            //MyVisualScriptLogicProvider.ShowNotification($"Event Fired: Zone={zone.Enabled}", 10000);
        }

        private void EntityAdd(IMyEntity ent)
        {
            /*MySafeZone zone = ent as MySafeZone;
            if (zone == null) return;

            long block = zone.SafeZoneBlockId;
            if (block == 0) return;*/

            //MyVisualScriptLogicProvider.ShowNotification("Found safe zone", 10000);
        }

        private void EntityRemoved(IMyEntity ent)
        {
            MySafeZone zone = ent as MySafeZone;
            if (zone == null) return;

            if (zone.SafeZoneBlockId == 0) return;
            Utils.DeactiveCharge(zone.SafeZoneBlockId);
        }

        public override void SaveData()
        {
            /*if (!isServer) return;

            foreach(var settings in zoneBlockSettingsCache.Values)
                SaveSafeZoneSettings(settings);*/
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(NetworkId, Comms.MessageHandler);

            if (isServer)
            {
                MySessionComponentSafeZones.OnSafeZoneUpdated -= OnSafeZoneUpdated;
                //MyAPIGateway.Entities.OnEntityAdd -= EntityAdd;
                MyAPIGateway.Entities.OnEntityRemove -= EntityRemoved;
            }

            Instance = null;
        }
    }
}
