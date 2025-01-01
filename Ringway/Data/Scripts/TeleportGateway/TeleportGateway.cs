using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRage.Game;
using System;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using VRage.ModAPI;
using Sandbox.ModAPI.Ingame;
using IMyShipController = Sandbox.ModAPI.IMyShipController;
using IMyCollector = Sandbox.ModAPI.IMyCollector;
using Sandbox.Game.GameSystems.Electricity;
using VRage.Game.ObjectBuilders.Definitions;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using VRage.Noise.Patterns;
using System.Linq;

namespace TeleportMechanisms {
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false,
        "RingwayCore", "SmallRingwayCore")]
    public class TeleportGateway : MyGameLogicComponent {
        public TeleportGatewaySettings Settings { get; private set; } = new TeleportGatewaySettings();
        public IMyCollector RingwayBlock;

        private static bool _controlsCreated = false;
        private static readonly Guid StorageGuid = new Guid("7F995845-BCEF-4E37-9B47-A035AC2A8E0B");

        private const int SAVE_INTERVAL_FRAMES = 100;
        private int _frameCounter = 0;

        private int _linkUpdateCounter = 0;
        private const int LINK_UPDATE_INTERVAL = 1;

        private const float CHARGE_RATE = 1.0f; // 0.5 MWh per second when charging
        private MyResourceSinkComponent Sink = null;
        public bool _isTeleporting = false;
        public int _teleportCountdown = 0;
        public double _jumpDistance = 0;

        private const float POWER_THRESHOLD = 0.1f; // 10% power threshold for failure
        private const float BASE_COUNTDOWN_SECONDS = 5; // Minimum countdown time
        private const float SECONDS_PER_100KM = 0.01f; // Additional second per 100km
        private const float POWER_PER_100KM = 1.0f; // 1 MWh per 100km
        private const float MIN_TELEPORT_CHARGE_PERCENTAGE = 0.1f; // 10% charge threshold

        private const double MAX_TELEPORT_DISTANCE = 1000000.0 * 1000; // 1,000,000 km in meters
        private static MyParticleEffect teleportEffect;

        static TeleportGateway() {
            CreateControls();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder) {
            RingwayBlock = Entity as IMyCollector;
            if (RingwayBlock == null) {
                MyLogger.Log($"TPGate: Init: Entity is not an upgrade module. EntityId: {Entity?.EntityId}");
                return;
            }

            Settings = Load(RingwayBlock);
            RingwayBlock.AppendingCustomInfo += AppendingCustomInfo;

            // Initialize power sink for charging
            Sink = RingwayBlock.Components.Get<MyResourceSinkComponent>();
            if (Sink != null) {
                Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, ComputeRequiredPower);
            }

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            lock (TeleportCore._lock) {
                TeleportCore._instances[RingwayBlock.EntityId] = this;
            }
        }

        private float ComputeRequiredPower() {
            if (!RingwayBlock.IsWorking) return 0f;

            // When not teleporting, draw power to charge
            if (!_isTeleporting) {
                if (Settings.StoredPower < Settings.MaxStoredPower) {
                    return CHARGE_RATE * 1000f; // Convert to watts
                }

                return 0f;
            }

            // During teleport, draw a massive amount of power
            float powerRequired = CalculatePowerRequired(_jumpDistance);
            float powerPerSecond = powerRequired / (_teleportCountdown / 60f);
            return powerPerSecond * 1000f; // Convert to watts
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb) {
            try {
                if (!block.IsWorking) {
                    sb.Append("--- Gateway Offline ---\n");
                    sb.Append("The gateway is not functional. Check power and block integrity.\n");
                    return;
                }

                sb.Append("--- Teleport Gateway Status ---\n");

                if (Sink != null) {
                    float currentStoredPower = (float)Settings.StoredPower;
                    float maxStoredPower = Settings.MaxStoredPower;
                    float chargePercentage = (currentStoredPower / maxStoredPower) * 100f;

                    // Display power level
                    sb.Append($"Charge: {chargePercentage:F1}% ({currentStoredPower:F2}/{maxStoredPower:F2} MWh)\n");
                    sb.Append($"Minimum Charge for Teleport: {MIN_TELEPORT_CHARGE_PERCENTAGE * 100}%\n");

                    // Status based on charging and power availability
                    if (_isTeleporting) {
                        sb.Append("Status: Teleporting...\n");
                    }
                    else if (Settings.StoredPower >= maxStoredPower) {
                        sb.Append("Status: Fully Charged - Ready to Jump\n");
                    }
                    else if (Sink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, CHARGE_RATE * 1000f)) {
                        sb.Append("Status: Charging...\n");

                        float remainingPower = maxStoredPower - currentStoredPower;
                        float timeToFullCharge = remainingPower / CHARGE_RATE; // in seconds
                        sb.Append($"Time to Full Charge: {timeToFullCharge:F1} seconds\n");
                    }
                    else {
                        sb.Append("Status: Insufficient Power for Charging\n");
                        sb.Append("Check reactor output or power supply.\n");
                    }

                    // Warning if power level is low and teleport cannot proceed
                    if (Settings.StoredPower < maxStoredPower * POWER_THRESHOLD && !_isTeleporting) {
                        sb.Append("Warning: Power below safe teleport threshold\n");
                        sb.Append("Charge to at least 10% to ensure successful teleport.\n");
                    }
                }

                // Display linked gateways info (unchanged)
                var linkedGateways = TeleportCore._TeleportLinks.ContainsKey(Settings.GatewayName)
                    ? TeleportCore._TeleportLinks[Settings.GatewayName]
                    : new List<long>();
                int linkedCount = linkedGateways.Count;
                sb.Append($"Linked Gateways: {linkedCount}\n");

                if (linkedCount > 2) {
                    sb.Append($"WARNING: More than two gateways on channel '{Settings.GatewayName}'.\n");
                    sb.Append("         Only the nearest gateway will be used.\n");
                }

                if (linkedCount > 0) {
                    sb.Append("Linked To:\n");
                    var sourcePosition = RingwayBlock.GetPosition();

                    IMyCollector nearestGateway = null;
                    double nearestDistance = double.MaxValue;

                    foreach (var gatewayId in linkedGateways) {
                        if (gatewayId != RingwayBlock.EntityId) {
                            var linkedGateway = MyAPIGateway.Entities.GetEntityById(gatewayId) as IMyCollector;
                            if (linkedGateway != null) {
                                var distance = Vector3D.Distance(sourcePosition, linkedGateway.GetPosition());
                                string distanceStr = $"{distance / 1000:F1} km";

                                if (distance < nearestDistance) {
                                    nearestDistance = distance;
                                    nearestGateway = linkedGateway;
                                }

                                sb.Append($"  - {linkedGateway.CustomName}: {distanceStr}\n");
                            }
                            else {
                                sb.Append($"  - Unknown (ID: {gatewayId})\n");
                            }
                        }
                    }

                    if (nearestGateway != null && linkedCount > 1) {
                        sb.Append($"Active Destination: {nearestGateway.CustomName}\n");
                    }
                }
                else {
                    sb.Append("Status: Not linked to any other gateways\n");
                }

                // Settings Info (unchanged)
                sb.Append($"Allow Players: {(Settings.AllowPlayers ? "Yes" : "No")}\n");
                sb.Append($"Allow Ships: {(Settings.AllowShips ? "Yes" : "No")}\n");
                sb.Append($"Show Sphere: {(Settings.ShowSphere ? "Yes" : "No")}\n");
                sb.Append($"Sphere Diameter: {Settings.SphereDiameter} m\n");

            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"Error in AppendingCustomInfo: {e}");
            }
        }

        public override void UpdateOnceBeforeFrame() {
            try {
                base.UpdateOnceBeforeFrame();
                if (RingwayBlock?.CubeGrid?.Physics == null)
                    return;

                Sink = RingwayBlock.Components.Get<MyResourceSinkComponent>();
                if (Sink != null) {
                    var powerReq = new MyResourceSinkInfo() {
                        ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                        MaxRequiredInput = 1000f,
                        RequiredInputFunc = CalculatePowerDraw
                    };
                    Sink.AddType(ref powerReq); // This is the critical line we were missing
                    Sink.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, CalculatePowerDraw);
                    Sink.Update();

                    MyLog.Default.WriteLineAndConsole(
                        $"TPGate: UpdateOnceBeforeFrame: Initialized power sink for {RingwayBlock.EntityId}");
                }

                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            }
            catch (Exception e) {
                MyLog.Default.WriteLineAndConsole($"Ringway.UpdateOnceBeforeFrame: {e}");
            }
        }

        private float CalculatePowerDraw() {
            if (!RingwayBlock.IsWorking) return 0f;
            if (_isTeleporting) {
                float powerRequired = CalculatePowerRequired(_jumpDistance);
                return (powerRequired / (_teleportCountdown / 60f)) * 1000f;
            }

            return Settings.StoredPower < Settings.MaxStoredPower ? CHARGE_RATE * 1000f : 0f;
        }

        private float _targetPowerDrain = 0f;
        private float _initialPower;
        public bool _showSphereDuringCountdown;

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (RingwayBlock == null) return;

            DrawDebugLineToNearestLinkedGateway();

            var destGatewayId = TeleportCore.GetDestinationGatewayId(Settings.GatewayName, RingwayBlock.EntityId);
            var destGateway = MyAPIGateway.Entities.GetEntityById(destGatewayId) as IMyCollector;
            Vector3D destinationPosition = destGateway?.GetPosition() ?? Vector3D.Zero;

            if (_isTeleporting)
            {
                if (_teleportCountdown > 0)
                {
                    _teleportCountdown--;

                    // Show countdown every second (60 ticks)
                    if (_teleportCountdown % 60 == 0)
                    {
                        int secondsLeft = _teleportCountdown / 60;
                        // Broadcast countdown to all nearby players
                        NotifyPlayersInRange(
                            $"Jump in {secondsLeft}s... Distance: {_jumpDistance / 1000:F1}km",
                            RingwayBlock.GetPosition(),
                            100,
                            "White"
                        );
                    }

                    // Update sphere color during countdown
                    if (Settings.ShowSphere)
                    {
                        Color countdownColor = new Color(255, 255, 0, 10);
                        TeleportBubbleManager.CreateOrUpdateBubble(RingwayBlock, countdownColor);
                        TeleportBubbleManager.DrawBubble(RingwayBlock, countdownColor);
                    }
                    return;
                }

                float powerRequired = CalculatePowerRequired(_jumpDistance);
                Settings.StoredPower = Math.Max(0, Settings.StoredPower - powerRequired);
                Settings.Changed = true;

                NotifyPlayersInRange(
                    $"Jump completed to destination {_jumpDistance / 1000:F1}km away.",
                    RingwayBlock.GetPosition(),
                    100,
                    "White"
                );

                _isTeleporting = false;
                _showSphereDuringCountdown = false;

                // Only send jump request if we're not the server
                if (!MyAPIGateway.Multiplayer.IsServer)
                {
                    var message = new JumpRequestMessage
                    {
                        GatewayId = RingwayBlock.EntityId,
                        Link = Settings.GatewayName
                    };
                    MyAPIGateway.Multiplayer.SendMessageToServer(
                        NetworkHandler.JumpRequestId,
                        MyAPIGateway.Utilities.SerializeToBinary(message)
                    );
                }
                else
                {
                    ProcessJumpRequest(RingwayBlock.EntityId, Settings.GatewayName);
                }
            }
            else
            {
                // Normal charging logic...
                if (RingwayBlock.IsWorking && Settings.StoredPower < Settings.MaxStoredPower)
                {
                    if (Sink != null && Sink.IsPowerAvailable(MyResourceDistributorComponent.ElectricityId, CHARGE_RATE * 1000f))
                    {
                        Settings.StoredPower = Math.Min(Settings.MaxStoredPower, Settings.StoredPower + (CHARGE_RATE / 60f));
                        Settings.Changed = true;
                        Sink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, CHARGE_RATE * 1000f);
                        Sink.Update();
                    }
                    else
                    {
                        Sink?.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, 0f);
                        Sink?.Update();
                    }
                }
            }

            if (++_frameCounter >= SAVE_INTERVAL_FRAMES)
            {
                _frameCounter = 0;
                TrySave();
            }

            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                RingwayBlock.RefreshCustomInfo();
                RingwayBlock.SetDetailedInfoDirty();
            }

            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session != null)
            {
                if (!_showSphereDuringCountdown && Settings.ShowSphere)
                {
                    Color defaultColor = new Color(0, 0, 255, 10);
                    TeleportBubbleManager.CreateOrUpdateBubble(RingwayBlock, defaultColor);
                    TeleportBubbleManager.DrawBubble(RingwayBlock, defaultColor);
                }
            }
        }

        public override void UpdateAfterSimulation100() {
            if (!RingwayBlock.IsWorking) return;

            try {

                // New link update logic
                if (++_linkUpdateCounter >= LINK_UPDATE_INTERVAL) {
                    _linkUpdateCounter = 0;
                    TeleportCore.UpdateTeleportLinks();
                    MyLogger.Log("TPGate: UpdateAfterSimulation100: Updated teleport links");
                }

                // Refresh custom info only when the terminal is open
                if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel) {
                    RingwayBlock.RefreshCustomInfo();
                    RingwayBlock.SetDetailedInfoDirty();
                }
            }
            catch (Exception e) {
                MyLogger.Log($"TPGate: UpdateAfterSimulation100: Exception - {e}");
            }
        }

        private void TrySave() {
            if (!Settings.Changed) return;

            Save();
            MyLogger.Log($"TPGate: TrySave: Settings saved for EntityId: {RingwayBlock.EntityId}");
        }

        private void Save() {
            if (RingwayBlock.Storage == null) {
                RingwayBlock.Storage = new MyModStorageComponent();
            }

            string serializedData = MyAPIGateway.Utilities.SerializeToXML(Settings);
            RingwayBlock.Storage.SetValue(StorageGuid, serializedData);

            // Send the updated settings to the server
            var message = new SyncSettingsMessage { EntityId = RingwayBlock.EntityId, Settings = this.Settings };
            var data = MyAPIGateway.Utilities.SerializeToBinary(message);
            MyAPIGateway.Multiplayer.SendMessageToServer(NetworkHandler.SyncSettingsId, data);

            Settings.Changed = false;
            Settings.LastSaved = MyAPIGateway.Session.ElapsedPlayTime;
            MyLogger.Log($"TPGate: Save: Settings saved for EntityId: {RingwayBlock.EntityId}");
        }


        public void ApplySettings(TeleportGatewaySettings settings) {
            this.Settings = settings;
            MyLogger.Log(
                $"TPGate: ApplySettings: Applied settings for EntityId: {RingwayBlock.EntityId}, GatewayName: {Settings.GatewayName}");
        }

        private static TeleportGatewaySettings Load(IMyCollector block) {
            MyLogger.Log($"TPGate: Load: Called. Attempting to load with StorageGuid: {StorageGuid}");
            if (block == null) {
                MyLogger.Log($"TPGate: Load: RingwayBlock is null.");
                return new TeleportGatewaySettings();
            }

            if (block.Storage == null) {
                MyLogger.Log($"TPGate: Load: RingwayBlock Storage is null. Creating new Storage.");
                block.Storage = new MyModStorageComponent();
            }

            MyLogger.Log($"TPGate: Load: RingwayBlock and Storage not null.");
            string data;
            if (block.Storage.TryGetValue(StorageGuid, out data)) {
                MyLogger.Log($"TPGate: Load: blockid:{block.EntityId} Storage had data: {data}");
                try {
                    var settings = MyAPIGateway.Utilities.SerializeFromXML<TeleportGatewaySettings>(data);
                    if (settings != null) {
                        settings.Changed = false;
                        settings.LastSaved = MyAPIGateway.Session.ElapsedPlayTime;
                        MyLogger.Log($"TPGate: Load: Successfully loaded settings.");
                        return settings;
                    }
                    else {
                        MyLogger.Log($"TPGate: Load: Deserialized settings were null.");
                    }
                }
                catch (Exception ex) {
                    MyLogger.Log($"TPGate: Load - Exception loading settings: {ex}");
                }
            }
            else {
                MyLogger.Log($"TPGate: Load: No data found for StorageGuid.");
            }

            MyLogger.Log($"TPGate: Load: Creating and returning new TeleportGatewaySettings.");
            var newSettings = new TeleportGatewaySettings();
            newSettings.Changed = true; // Mark as changed so it will be saved
            return newSettings;
        }

        public override void Close() {
            Save();

            lock (TeleportCore._lock) {
                TeleportCore._instances.Remove(RingwayBlock.EntityId);
                MyLogger.Log(
                    $"TPGate: Close: Removed instance for EntityId {Entity.EntityId}. Remaining instances: {TeleportCore._instances.Count}");
            }

            TeleportBubbleManager.RemoveBubble(RingwayBlock);

            base.Close();
        }

        private void DrawDebugLineToNearestLinkedGateway() {
            // Check if there are linked gateways for the current gateway
            List<long> linkedGateways;
            if (!TeleportCore._TeleportLinks.TryGetValue(Settings.GatewayName, out linkedGateways) ||
                linkedGateways.Count <= 1) {
                return; // No links or only linked to itself
            }

            Vector3D sourcePosition = RingwayBlock.GetPosition();
            IMyCollector nearestGateway = null;
            double nearestDistance = double.MaxValue;

            foreach (var gatewayId in linkedGateways) {
                if (gatewayId == RingwayBlock.EntityId) continue; // Skip self

                var linkedGateway = MyAPIGateway.Entities.GetEntityById(gatewayId) as IMyCollector;
                if (linkedGateway != null) {
                    double distance = Vector3D.Distance(sourcePosition, linkedGateway.GetPosition());
                    if (distance < nearestDistance) {
                        nearestDistance = distance;
                        nearestGateway = linkedGateway;
                    }
                }
            }

            // Draw the line if a nearest gateway was found
            if (nearestGateway != null) {
                Vector3D destinationPosition = nearestGateway.GetPosition();
                Vector4 green = Color.Green;
                if (!MyAPIGateway.Utilities.IsDedicated) {
                    MySimpleObjectDraw.DrawLine(sourcePosition, destinationPosition, MyStringId.GetOrCompute("Square"),
                          ref green, 0.1f);
                }
            }
        }


        public override bool IsSerialized() {
            Save();
            return base.IsSerialized();
        }

        private static void CreateControls() {
            if (_controlsCreated) return;

            MyLogger.Log("TPGate: CreateControl: Creating custom controls and actions");

            var controls = new List<IMyTerminalControl>
            {
                CreateGatewayNameControl(),
                CreateJumpButton(),
                CreateAllowPlayersCheckbox(),
                CreateAllowShipsCheckbox(),
                CreateShowSphereCheckbox(),
                CreateSphereDiameterSlider()
            };

            var actions = new List<IMyTerminalAction>
            {
                CreateJumpAction(),
                CreateToggleShowSphereAction(),
                CreateShowSphereOnAction(),
                CreateShowSphereOffAction()
            };

            MyAPIGateway.TerminalControls.CustomControlGetter += (block, blockControls) => {
                if (block is IMyCollector && (block.BlockDefinition.SubtypeName == "RingwayCore" ||
                                              block.BlockDefinition.SubtypeName == "SmallRingwayCore")) {
                    blockControls.AddRange(controls);
                }
            };

            MyAPIGateway.TerminalControls.CustomActionGetter += (block, blockActions) => {
                if (block is IMyCollector && (block.BlockDefinition.SubtypeName == "RingwayCore" ||
                                              block.BlockDefinition.SubtypeName == "SmallRingwayCore")) {
                    blockActions.AddRange(actions);
                }
            };

            _controlsCreated = true;
            MyLogger.Log("TPGate: CreateControl: Custom controls and actions created");
        }

        private static IMyTerminalControl CreateGatewayNameControl() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyCollector>("GatewayName");
            control.Title = MyStringId.GetOrCompute("Gateway Name");
            control.Getter = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                return gateway != null ? new StringBuilder(gateway.Settings.GatewayName) : new StringBuilder();
            };
            control.Setter = (block, value) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.GatewayName = value.ToString();
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                }
            };
            control.SupportsMultipleBlocks = false;
            return control;
        }

        private static IMyTerminalControl CreateAllowPlayersCheckbox() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>("AllowPlayers");
            control.Title = MyStringId.GetOrCompute("Allow Players");
            control.Getter = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                return gateway != null ? gateway.Settings.AllowPlayers : false;
            };
            control.Setter = (block, value) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.AllowPlayers = value;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log($"TPGate: AllowPlayers set to {value} for EntityId: {block.EntityId}");
                }
            };
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static IMyTerminalControl CreateAllowShipsCheckbox() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>("AllowShips");
            control.Title = MyStringId.GetOrCompute("Allow Ships");
            control.Getter = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                return gateway != null ? gateway.Settings.AllowShips : false;
            };
            control.Setter = (block, value) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.AllowShips = value;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log($"TPGate: AllowShips set to {value} for EntityId: {block.EntityId}");
                }
            };
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static IMyTerminalControl CreateJumpButton() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCollector>("JumpButton");
            control.Title = MyStringId.GetOrCompute("Jump");
            control.Visible = (block) => true;
            control.Action = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) gateway.JumpAction(block as IMyCollector);
            };
            return control;
        }

        private static IMyTerminalAction CreateJumpAction() {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("Jump");
            action.Name = new StringBuilder("Jump");
            action.Icon = @"Textures\GUI\Icons\Actions\Jump.dds";
            action.Action = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) gateway.JumpAction(block as IMyCollector);
            };
            action.Writer = (b, sb) => sb.Append("Initiate Jump");
            action.ValidForGroups = true;
            return action;
        }

        private static IMyTerminalControl CreateShowSphereCheckbox() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyCollector>("ShowSphere");
            control.Title = MyStringId.GetOrCompute("Show Sphere");
            control.Getter = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                return gateway != null ? gateway.Settings.ShowSphere : false;
            };
            control.Setter = (block, value) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.ShowSphere = value;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log($"TPGate: ShowSphere set to {value} for EntityId: {block.EntityId}");
                }
            };
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static IMyTerminalControl CreateSphereDiameterSlider() {
            var control =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyCollector>("SphereDiameter");
            control.Title = MyStringId.GetOrCompute("Sphere Diameter");
            control.SetLimits(1, 100); // Set the range from 1 to 300
            control.Getter = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                return gateway != null ? gateway.Settings.SphereDiameter : 50.0f;
            };
            control.Setter = (block, value) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.SphereDiameter = value;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                }
            };
            control.Writer = (block, sb) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    sb.Append($"{gateway.Settings.SphereDiameter} meters");
                }
            };
            control.SupportsMultipleBlocks = true;
            return control;
        }

        private static IMyTerminalAction CreateToggleShowSphereAction() {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("ToggleShowSphere");
            action.Name = new StringBuilder("Toggle Show Sphere");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds"; // You may want to use a different icon
            action.Action = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.ShowSphere = !gateway.Settings.ShowSphere;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log(
                        $"TPGate: ShowSphere toggled to {gateway.Settings.ShowSphere} for EntityId: {block.EntityId}");
                }
            };
            action.Writer = (b, sb) => sb.Append(b.GameLogic.GetAs<TeleportGateway>()?.Settings.ShowSphere == true
                ? "Hide Sphere"
                : "Show Sphere");
            return action;
        }

        private static IMyTerminalAction CreateShowSphereOnAction() {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("ShowSphereOn");
            action.Name = new StringBuilder("Show Sphere On");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOn.dds"; // You may want to use a different icon
            action.Action = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.ShowSphere = true;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log($"TPGate: ShowSphere set to true for EntityId: {block.EntityId}");
                }
            };
            action.Writer = (b, sb) => sb.Append("Show Sphere On");
            return action;
        }

        private static IMyTerminalAction CreateShowSphereOffAction() {
            var action = MyAPIGateway.TerminalControls.CreateAction<IMyCollector>("ShowSphereOff");
            action.Name = new StringBuilder("Show Sphere Off");
            action.Icon = @"Textures\GUI\Icons\Actions\SwitchOff.dds"; // You may want to use a different icon
            action.Action = (block) => {
                var gateway = block.GameLogic.GetAs<TeleportGateway>();
                if (gateway != null) {
                    gateway.Settings.ShowSphere = false;
                    gateway.Settings.Changed = true;
                    gateway.TrySave();
                    MyLogger.Log($"TPGate: ShowSphere set to false for EntityId: {block.EntityId}");
                }
            };
            action.Writer = (b, sb) => sb.Append("Show Sphere Off");
            return action;
        }

        private int CalculateCountdown(double distanceInMeters) {
            float distanceInKm = (float)(distanceInMeters / 1000);
            float additionalSeconds = (distanceInKm / 100) * SECONDS_PER_100KM;
            float totalSeconds = Math.Max(BASE_COUNTDOWN_SECONDS, BASE_COUNTDOWN_SECONDS + additionalSeconds);

            return (int)(totalSeconds * 60); // Convert total time to ticks (assuming 60 ticks per second)
        }

        private float CalculatePowerRequired(double distanceInMeters)
        {
            distanceInMeters = Math.Min(distanceInMeters, MAX_TELEPORT_DISTANCE); // Clamp distance
            float distanceInKm = (float)(distanceInMeters / 1000);
            float maxDistanceInKm = (float)(MAX_TELEPORT_DISTANCE / 1000);
            float chargePercentage = MathHelper.Clamp(distanceInKm / maxDistanceInKm, MIN_TELEPORT_CHARGE_PERCENTAGE, 1.0f);
            return Settings.MaxStoredPower * chargePercentage;
        }

        public void JumpAction(IMyCollector block, bool isTimerInitiated = false)
        {
            if (_isTeleporting)
            {
                MyLogger.Log($"TPGate: JumpAction: Already teleporting, ignoring new request");
                return;
            }

            MyLogger.Log($"TPGate: JumpAction: Jump action triggered for EntityId: {block.EntityId}, Timer: {isTimerInitiated}");

            var link = Settings.GatewayName;
            if (string.IsNullOrEmpty(link))
            {
                MyLogger.Log($"TPGate: JumpAction: No valid link set");
                return;
            }

            _jumpDistance = 0;
            float powerRequired = 0;

            var destGatewayId = TeleportCore.GetDestinationGatewayId(link, block.EntityId);
            if (destGatewayId != 0)
            {
                var destGateway = MyAPIGateway.Entities.GetEntityById(destGatewayId) as IMyCollector;
                if (destGateway != null)
                {
                    _jumpDistance = Vector3D.Distance(block.GetPosition(), destGateway.GetPosition());
                    powerRequired = CalculatePowerRequired(_jumpDistance);
                }
                else
                {
                    MyLogger.Log($"TPGate: JumpAction: Destination gateway not found");
                    return;
                }
            }

            if (_jumpDistance > MAX_TELEPORT_DISTANCE)
            {
                MyLogger.Log($"TPGate: JumpAction: Jump distance exceeds maximum allowed range");
                NotifyPlayersInRange(
                    $"Jump distance {_jumpDistance / 1000:F1}km exceeds maximum allowed range of {MAX_TELEPORT_DISTANCE / 1000:F1}km.",
                    block.GetPosition(),
                    100,
                    "Red"
                );
                return;
            }

            if (Settings.StoredPower < powerRequired)
            {
                MyLogger.Log($"TPGate: JumpAction: Insufficient power for jump");
                NotifyPlayersInRange(
                    $"Insufficient power for {_jumpDistance / 1000:F1}km jump. Need {powerRequired:F1}MWh",
                    block.GetPosition(),
                    100,
                    "Red"
                );
                return;
            }

            _isTeleporting = true;
            _teleportCountdown = CalculateCountdown(_jumpDistance);
            Settings.StoredPower = Math.Max(0, Settings.StoredPower - powerRequired);
            Settings.Changed = true;

            // Send jump initiation to clients if this is server
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                var message = new JumpInitiatedMessage
                {
                    GatewayId = block.EntityId,
                    JumpDistance = _jumpDistance,
                    CountdownTicks = _teleportCountdown,
                    PowerRequired = powerRequired
                };
                var data = MyAPIGateway.Utilities.SerializeToBinary(message);
                MyAPIGateway.Multiplayer.SendMessageToOthers(NetworkHandler.JumpInitiatedId, data);
            }

            float totalSeconds = _teleportCountdown / 60f;
            NotifyPlayersInRange(
                $"Initiating {_jumpDistance / 1000:F1}km jump - {totalSeconds:F1} seconds",
                block.GetPosition(),
                100,
                "White"
            );

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            MyLogger.Log($"TPGate: JumpAction: Jump sequence started. Distance: {_jumpDistance / 1000:F1}km, Power: {powerRequired:F1}MWh");
        }

        public static void NotifyPlayersInRange(string text, Vector3D position, double radius, string font = "White") {
            BoundingSphereD bound = new BoundingSphereD(position, radius);
            List<IMyEntity> nearbyEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref bound);

            foreach (var entity in nearbyEntities) {
                IMyCharacter character = entity as IMyCharacter;
                if (character != null && character.IsPlayer) {
                    var notification = MyAPIGateway.Utilities.CreateNotification(text, 2000, font);
                    notification.Show();
                }
            }
        }

        public static void ProcessJumpRequest(long gatewayId, string link)
        {
            MyLogger.Log($"TPGate: ProcessJumpRequest: Processing jump request for gateway {gatewayId}, link {link}");

            // Only server should process jump requests
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            var sourceGateway = MyAPIGateway.Entities.GetEntityById(gatewayId) as IMyCollector;
            if (sourceGateway == null || !sourceGateway.IsWorking)
            {
                MyLogger.Log($"TPCore: ProcessJumpRequest: Gateway {gatewayId} is null or not functional");
                return;
            }

            var sourceGatewayLogic = sourceGateway.GameLogic.GetAs<TeleportGateway>();
            if (sourceGatewayLogic == null)
            {
                MyLogger.Log("TPCore: ProcessJumpRequest: Could not get gateway logic");
                return;
            }

            if (sourceGatewayLogic._isTeleporting)
            {
                MyLogger.Log("TPCore: ProcessJumpRequest: Gateway is already teleporting");
                return;
            }

            var destGatewayId = TeleportCore.GetDestinationGatewayId(link, sourceGateway.EntityId);
            var destGateway = MyAPIGateway.Entities.GetEntityById(destGatewayId) as IMyCollector;
            if (destGateway == null)
            {
                MyLogger.Log("TPCore: ProcessJumpRequest: Could not find destination gateway");
                return;
            }

            // Get the gateway's GridGroup
            List<IMyCubeGrid> gatewayGridGroup = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(sourceGateway.CubeGrid, GridLinkTypeEnum.Physical, gatewayGridGroup);

            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                try
                {
                    float sphereRadius = sourceGatewayLogic.Settings.SphereDiameter / 2.0f;
                    Vector3D sphereCenter = sourceGateway.GetPosition() + sourceGateway.WorldMatrix.Forward * sphereRadius;
                    BoundingSphereD sphere = new BoundingSphereD(sphereCenter, sphereRadius);

                    List<IMyEntity> entitiesInSphere = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                    Dictionary<long, HashSet<IMyCubeGrid>> gridGroups = new Dictionary<long, HashSet<IMyCubeGrid>>();
                    HashSet<IMyCubeGrid> processedGrids = new HashSet<IMyCubeGrid>();
                    List<IMyCharacter> charactersToTeleport = new List<IMyCharacter>();
                    int teleportedCount = 0;

                    // First pass - identify all entities to teleport
                    foreach (var entity in entitiesInSphere)
                    {
                        // Handle characters
                        var character = entity as IMyCharacter;
                        if (character != null)
                        {
                            if (sourceGatewayLogic.Settings.AllowPlayers)
                            {
                                charactersToTeleport.Add(character);
                                MyLogger.Log($"TPGate: ProcessJumpRequest: Found character to teleport");
                            }
                            continue;
                        }

                        // Handle grids
                        var grid = entity as IMyCubeGrid;
                        if (grid == null || grid.IsStatic || processedGrids.Contains(grid) || gatewayGridGroup.Contains(grid))
                        {
                            if (grid != null)
                            {
                                MyLogger.Log($"TPGate: ProcessJumpRequest: Skipping grid {grid.DisplayName} - Static: {grid.IsStatic}, Already Processed: {processedGrids.Contains(grid)}, Gateway Grid: {gatewayGridGroup.Contains(grid)}");
                            }
                            continue;
                        }

                        if (!sourceGatewayLogic.Settings.AllowShips)
                        {
                            MyLogger.Log($"TPGate: ProcessJumpRequest: Ships not allowed, skipping grid {grid.DisplayName}");
                            continue;
                        }

                        // Get connected grids
                        HashSet<IMyCubeGrid> connectedGrids = new HashSet<IMyCubeGrid>();
                        MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Physical, connectedGrids);
                        MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, connectedGrids);

                        MyLogger.Log($"TPGate: ProcessJumpRequest: Found grid group with {connectedGrids.Count} connected grids");

                        // Check for locked landing gear in the group
                        bool hasLockedLandingGear = false;
                        foreach (var connectedGrid in connectedGrids)
                        {
                            List<IMySlimBlock> landingGears = new List<IMySlimBlock>();
                            connectedGrid.GetBlocks(landingGears, b => b.FatBlock is SpaceEngineers.Game.ModAPI.Ingame.IMyLandingGear);

                            foreach (var gear in landingGears)
                            {
                                var landingGear = gear.FatBlock as SpaceEngineers.Game.ModAPI.Ingame.IMyLandingGear;
                                if (landingGear != null && landingGear.IsLocked)
                                {
                                    hasLockedLandingGear = true;
                                    MyLogger.Log($"TPGate: ProcessJumpRequest: Found locked landing gear on grid {connectedGrid.DisplayName}");
                                    break;
                                }
                            }
                            if (hasLockedLandingGear) break;
                        }

                        if (hasLockedLandingGear)
                        {
                            MyLogger.Log($"TPGate: ProcessJumpRequest: Skipping grid group due to locked landing gear");
                            continue;
                        }

                        // Find the main grid
                        var mainGrid = connectedGrids
                            .Select(g => g.GetTopMostParent() as IMyCubeGrid)
                            .FirstOrDefault(g => g != null);

                        if (mainGrid != null && !gridGroups.ContainsKey(mainGrid.EntityId))
                        {
                            gridGroups[mainGrid.EntityId] = connectedGrids;
                            MyLogger.Log($"TPGate: ProcessJumpRequest: Added grid group with main grid {mainGrid.DisplayName} (EntityId: {mainGrid.EntityId})");
                        }

                        processedGrids.UnionWith(connectedGrids);
                    }

                    // Teleport characters
                    foreach (var character in charactersToTeleport)
                    {
                        TeleportCore.TeleportEntity(character, sourceGateway, destGateway);
                        teleportedCount++;
                        MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("InvalidCustomBlinkParticleEnter", character.GetPosition());
                        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ShipPrototechJumpDriveJumpIn", character.GetPosition());
                        MyLogger.Log($"TPGate: ProcessJumpRequest: Teleported character");
                    }

                    // Teleport grid groups
                    foreach (var group in gridGroups)
                    {
                        var mainGrid = MyAPIGateway.Entities.GetEntityById(group.Key) as IMyCubeGrid;
                        if (mainGrid == null) continue;

                        TeleportCore.TeleportEntity(mainGrid, sourceGateway, destGateway);
                        teleportedCount += group.Value.Count;
                        MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("InvalidCustomBlinkParticleEnter", mainGrid.GetPosition());
                        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ShipPrototechJumpDriveJumpIn", mainGrid.GetPosition());
                        MyLogger.Log($"TPGate: ProcessJumpRequest: Teleported grid group with main grid {mainGrid.DisplayName}");
                    }

                    if (teleportedCount > 0)
                    {
                        MyLogger.Log($"TPGate: ProcessJumpRequest: Teleported {teleportedCount} entities ({charactersToTeleport.Count} characters and {gridGroups.Count} grid groups)");
                        NotifyPlayersInRange($"Teleported {teleportedCount} entities", sourceGateway.GetPosition(), 100, "White");
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.Log($"TPGate: ProcessJumpRequest: Exception during teleport: {ex}");
                }

                sourceGatewayLogic._isTeleporting = false;
                sourceGatewayLogic._showSphereDuringCountdown = false;

            }, sourceGatewayLogic._teleportCountdown.ToString());

            MyLogger.Log($"TPGate: ProcessJumpRequest: Scheduled teleport after {sourceGatewayLogic._teleportCountdown / 60f}s");
        }

        private static void CreateTeleportEffects(Vector3D position)
        {
            MyVisualScriptLogicProvider.CreateParticleEffectAtPosition("InvalidCustomBlinkParticleEnter", position);
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ShipPrototechJumpDriveJumpIn", position);
        }
    }
}
