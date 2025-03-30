using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.VisualScripting;
using VRage.Utils;

namespace SiegableSafeZones
{
    public static class Controls
    {
        public static bool _zoneControlsCreated;
        public static bool _jumpdriveControlsCreated;
        public static bool _jumpdriveActionsCreated;
        public static StringBuilder sb;

        public static IMyTerminalBlock currentBlock;

        public static void CreateZoneControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block as IMySafeZoneBlock == null) return;

            currentBlock = block;
            if (_zoneControlsCreated)
            {

                return;
            }

            _zoneControlsCreated = true;

            for (int i = controls.Count - 1; i >= 0; i--)
            {
                var toggle = controls[i] as IMyTerminalControlOnOffSwitch;
                var slider = controls[i] as IMyTerminalControlSlider;
                var combo = controls[i] as IMyTerminalControlCombobox;
                if (toggle != null)
                {
                    if (controls[i].Id == "SafeZoneCreate" && Session.Instance.config._enemyCheckStartup._enableEnemyChecks)
                        controls[i].Enabled = Block => !ActionControls.IsEnemyNear(Block);

                    controls[i].RedrawControl();
                }

                if (slider != null)
                {
                    if (controls[i].Id == "SafeZoneSlider")
                    {
                        controls[i].Visible = Block => false;
                        controls[i].RedrawControl();
                    }
                }

                if (combo != null)
                {
                    if (controls[i].Id == "SafeZoneShapeCombo")
                    {
                        controls[i].Visible = Block => false;
                        controls[i].RedrawControl();
                    }
                }
            }

            // Sep A
            var sepA = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMySafeZoneBlock>("ZoneSepA");
            sepA.Enabled = Block => true;
            sepA.SupportsMultipleBlocks = false;
            sepA.Visible = Block => true;
            MyAPIGateway.TerminalControls.AddControl<IMySafeZoneBlock>(sepA);
            controls.Add(sepA);

            // Siegable Safezone Label
            var SiegableSafeZoneLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMySafeZoneBlock>("SiegableSafeZoneLabelB");
            SiegableSafeZoneLabel.Enabled = Block => true;
            SiegableSafeZoneLabel.SupportsMultipleBlocks = false;
            SiegableSafeZoneLabel.Visible = Block => true;
            SiegableSafeZoneLabel.Label = MyStringId.GetOrCompute("--- Siegable SafeZones Errors ---");
            MyAPIGateway.TerminalControls.AddControl<IMySafeZoneBlock>(SiegableSafeZoneLabel);
            controls.Add(SiegableSafeZoneLabel);

            var enemyNearError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMySafeZoneBlock>("EnemyNearError");
            enemyNearError.Enabled = Block => true;
            enemyNearError.SupportsMultipleBlocks = false;
            enemyNearError.Visible = Block => ActionControls.CheckEnemyNearError(Block);
            enemyNearError.Label = MyStringId.GetOrCompute("Enemy Is Near");
            MyAPIGateway.TerminalControls.AddControl<IMySafeZoneBlock>(enemyNearError);
            controls.Add(enemyNearError);
        }

        public static void CreateJumpDriveControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block as IMyJumpDrive == null) return;

            currentBlock = block;
            if (_jumpdriveControlsCreated)
            {
                sb = new StringBuilder();
                /*if (ActionControls.selectedSafeZoneId == 0)
                {
                    sb.Clear();
                    return;
                }*/

                ZoneBlockSettings settings = null;
                /*foreach (var setting in Session.Instance.zoneBlockSettingsCache.Values)
                {
                    if (setting.IsSieging && setting.JDSiegingId == block.EntityId)
                    {
                        settings = setting;
                        break;
                    }
                }*/

                if (settings == null)
                    Session.Instance.zoneBlockSettingsCache.TryGetValue(ActionControls.selectedSafeZoneId, out settings);


                if (settings == null) return;
                int cost = Session.Instance.config._siegeConfig._siegeConsumptionAmt == -1 ? (int)Math.Round(settings.CurrentCharge, 0) : Session.Instance.config._siegeConfig._siegeConsumptionAmt;

                sb.Append("\n--- Siegable Safe Zones ---\n");
                sb.Append($"[Selected SafeZone Faction]: {settings.ZoneBlockFactionTag}\n");
                sb.Append($"[Selected SafeZone Charge]: {Math.Round(settings.CurrentCharge, 2)}%\n");
                sb.Append($"[Cost To Siege]: {cost} Token(s)");

                //settings._detailInfo = sb.ToString();
                block.RefreshCustomInfo();
                return;
            }

            _jumpdriveControlsCreated = true;

            // Sep A
            var sepA = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyJumpDrive>("SepA");
            sepA.Enabled = Block => true;
            sepA.SupportsMultipleBlocks = false;
            sepA.Visible = Block => true;
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(sepA);
            controls.Add(sepA);

            // Siegable Safezone Label
            var SiegableSafeZoneLabel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("SiegableSafeZoneLabel");
            SiegableSafeZoneLabel.Enabled = Block => true;
            SiegableSafeZoneLabel.SupportsMultipleBlocks = false;
            SiegableSafeZoneLabel.Visible = Block => true;
            SiegableSafeZoneLabel.Label = MyStringId.GetOrCompute("--- Siegable SafeZones ---");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(SiegableSafeZoneLabel);
            controls.Add(SiegableSafeZoneLabel);

            // Siegable Zones
            var siegableZones = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyJumpDrive>("SiegableZones");
            siegableZones.Enabled = Block => true;
            siegableZones.SupportsMultipleBlocks = false;
            siegableZones.Visible = Block => true;
            siegableZones.Title = MyStringId.GetOrCompute("Siegable SafeZones");
            siegableZones.ListContent = ActionControls.GetSafeZoneList;
            siegableZones.VisibleRowsCount = 10;
            siegableZones.ItemSelected = ActionControls.SetSafeZone;
            siegableZones.Multiselect = false;
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(siegableZones);
            controls.Add(siegableZones);

            // Siege Button
            var siegeButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyJumpDrive>("SiegeButton");
            siegeButton.Enabled = Block => ActionControls.AllowSiegeEnable(Block);
            siegeButton.SupportsMultipleBlocks = false;
            siegeButton.Visible = Block => true;
            siegeButton.Title = MyStringId.GetOrCompute("Siege SafeZone");
            //claimButton.Tooltip = MyStringId.GetOrCompute("Sets the claim area radius.");
            siegeButton.Action = Block => ActionControls.StartSiege(Block);
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(siegeButton);

            // Errors
            var Errors = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("Errors");
            Errors.Enabled = Block => true;
            Errors.SupportsMultipleBlocks = false;
            Errors.Visible = Block => ActionControls.DisplayErrors(Block);
            Errors.Label = MyStringId.GetOrCompute("Errors:");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(Errors);
            controls.Add(Errors);

            // Not in faction error
            var NotInFactionError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("NotInFactionError");
            NotInFactionError.Enabled = Block => true;
            NotInFactionError.SupportsMultipleBlocks = false;
            NotInFactionError.Visible = Block => ActionControls.CheckForFactionError(Block);
            NotInFactionError.Label = MyStringId.GetOrCompute("Not In Faction");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(NotInFactionError);
            controls.Add(NotInFactionError);

            // Faction offline error
            var factionOfflineError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("FactionOfflineError");
            factionOfflineError.Enabled = Block => true;
            factionOfflineError.SupportsMultipleBlocks = false;
            factionOfflineError.Visible = Block => ActionControls.CheckFactionOfflineError(Block);
            factionOfflineError.Label = MyStringId.GetOrCompute("Target faction is offline");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(factionOfflineError);
            controls.Add(factionOfflineError);

            // Valid tokens error
            var validTokensError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("ValidTokensError");
            validTokensError.Enabled = Block => true;
            validTokensError.SupportsMultipleBlocks = false;
            validTokensError.Visible = Block => ActionControls.CheckValidTokensError(Block);
            validTokensError.Label = MyStringId.GetOrCompute("Insufficient Funds");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(validTokensError);
            controls.Add(validTokensError);

            // Valid jumpdrive charge error
            var validJDChargeError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("ValidJDChargeError");
            validJDChargeError.Enabled = Block => true;
            validJDChargeError.SupportsMultipleBlocks = false;
            validJDChargeError.Visible = Block => ActionControls.CheckValidEnergyError(Block);
            validJDChargeError.Label = MyStringId.GetOrCompute("Jumpdrive not fully charged");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(validJDChargeError);
            controls.Add(validJDChargeError);

            // Check in range error
            var checkInRangeError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("CheckInRangeError");
            checkInRangeError.Enabled = Block => true;
            checkInRangeError.SupportsMultipleBlocks = false;
            checkInRangeError.Visible = Block => ActionControls.CheckInRangeError(Block);
            checkInRangeError.Label = MyStringId.GetOrCompute("Not in range");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(checkInRangeError);
            controls.Add(checkInRangeError);

            // Check siege error
            var checkSiegeError = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyJumpDrive>("CheckSiegeError");
            checkSiegeError.Enabled = Block => true;
            checkSiegeError.SupportsMultipleBlocks = false;
            checkSiegeError.Visible = Block => ActionControls.CheckSiegeError(Block);
            checkSiegeError.Label = MyStringId.GetOrCompute("Being Sieged");
            MyAPIGateway.TerminalControls.AddControl<IMyJumpDrive>(checkSiegeError);
            controls.Add(checkSiegeError);

        }

        public static void CreateZoneActions(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (block as IMySafeZoneBlock == null) return;

            foreach (var action in actions)
            {
                if (action.Id == "DecreaseSafeZoneXSlider")
                    action.Enabled = Block => false;

                if (action.Id == "IncreaseSafeZoneXSlider")
                    action.Enabled = Block => false;
            }
        }
    }
}
