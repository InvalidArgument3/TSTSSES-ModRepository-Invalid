using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyFunctionalBlock = Sandbox.ModAPI.IMyFunctionalBlock;
using IMyLaserAntenna = Sandbox.ModAPI.Ingame.IMyLaserAntenna;
using IMyRadioAntenna = Sandbox.ModAPI.Ingame.IMyRadioAntenna;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace CargoTeleporter
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "LargeBlockSmallSorterTeleport",
        "SmallBlockMediumSorterTeleport")]
    public class CargoTeleporterSorterServer : MyGameLogicComponent
    {
        private MyCubeBlock _cargoTeleporter;
        private IMyInventory _inventory;
        private MyObjectBuilder_EntityBase _objectBuilder;
        private string _status;
        private string _targetBlockName;
        private string _targetGridName = "";
        private bool _sending;
        private string _oldName = "";

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            _objectBuilder = objectBuilder;
            _cargoTeleporter = Entity as MyCubeBlock;
            base.Init(objectBuilder);
            (_cargoTeleporter as IMyTerminalBlock).AppendingCustomInfo += AppendingCustomInfo;
            (_cargoTeleporter as IMyTerminalBlock).CustomNameChanged += CustomNameChange;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? _objectBuilder.Clone() as MyObjectBuilder_EntityBase : _objectBuilder;
        }

        public override void Close()
        {
            Logging.Close();
        }

        public override void UpdateBeforeSimulation100()
        {
            if (_cargoTeleporter == null) return;
            if (!((IMyFunctionalBlock) _cargoTeleporter).Enabled) return;

            try
            {
                if (MyAPIGateway.Session == null)
                {
                    Write("MyAPIGateway.Session is null");
                    return;
                }
                
                ParseName();
                
                if (_targetBlockName == "") return;
                
                if (_inventory == null) _inventory = _cargoTeleporter.GetInventory();
                if (_sending && _inventory.Empty())
                {
                    UpdateStatus("Status: Source Empty");
                    return;
                }
                
                //We use true, if it goes through the ref; (which I treat like an out) it will be changed.
                //If it goes through the linq; then we are unchanged and true is the correct value. (we have found the grid, it is this one)
                bool gridFound = true;
                IMyEntity targetBlock;
                if (_targetGridName.Length > 2 && _targetGridName != _cargoTeleporter.CubeGrid.Name)
                {
                    targetBlock = GetTarget(ref gridFound);
                }
                else
                {
                    var cubeBlocks = _cargoTeleporter.CubeGrid.GetFatBlocks();
                    targetBlock = cubeBlocks.First(x => x?.DisplayNameText == _targetBlockName && OwnershipUtils.IsSameFactionOrOwner(_cargoTeleporter, x));
                }

                var disonnectedStatus = "Status: Disconnected";
                //If grid not found, 
                if (!gridFound)
                {
                    Write("Grid Not Found");
                }
                disonnectedStatus += $"\nGrid: {(gridFound ? "" : "Not ")}Found";
                if (targetBlock == null)
                {
                    Write("Target Not Found");
                }
                //This message should only ever say T/S Not Found, but in case I flubbed it; it can say found
                disonnectedStatus += $"\n{(_sending ? "Target" : "Source")}: {(targetBlock != null ? "" : "Not ")}Found";

                if (targetBlock == null || !gridFound)
                {
                    UpdateStatus(disonnectedStatus);
                    return;
                }
                
                var targetInventory = targetBlock.GetInventory();
                var status = "Status: Connected\nTarget: ";
                var targetStatus = targetInventory.IsFull ? "Full" : targetInventory.Empty() ? "Empty" : "Some";
                var inventoryStatus = _inventory.IsFull ? "Full" : _inventory.Empty() ? "Empty" : "Some";
                status += _sending ? targetStatus : inventoryStatus;
                status += "\nSource: ";
                status += !_sending ? targetStatus : inventoryStatus;

                UpdateStatus(status);
                
                if (!targetInventory.IsFull && _sending)
                    targetInventory.TransferItemFrom(_inventory, 0, null, true, null, false);
                else if (!targetInventory.Empty() && !_inventory.IsFull && !_sending)
                    _inventory.TransferItemFrom(targetInventory, 0, null, true, null, false);
            }
            catch (Exception ex)
            {
                Logging.WriteLine(ex.ToString());
            }
        }

        private void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            sb.Clear();
            sb.Append(_status);
        }

        private void CustomNameChange(IMyTerminalBlock block)
        {
            if (!ParseName()) return;
            
            if (_targetBlockName.Length < 2)
            {
                Write("Name too small");
                UpdateStatus("Status: No filters\n\nPlease add [T:Block Name] or [F:Block Name].\nPlease add [G:Grid Name] if using antennas.");
                return;
            }
        }

        private void UpdateStatus(string status)
        {
            if (status == _status) return;
            _status = status;
            (_cargoTeleporter as IMyTerminalBlock).RefreshCustomInfo();
        }

        private void Write(string v)
        {
            if (Config.enableDebug) Logging.WriteLine(_cargoTeleporter.CubeGrid.DisplayName + " - " + _cargoTeleporter.DisplayNameText + ": " + v);
        }

        private bool ParseName()
        {
            var displayName = _cargoTeleporter.DisplayNameText;
            if (displayName == _oldName) return false;

            //For ease of reading
            const int NotFound = -1;
            const char StartBracket = '[';
            const char StopBracket = ']';
            const char ModeDelimiter = ':';
            const char ToMode = 'T';
            const char ToModeLower = 't';
            const char FromMode = 'F';
            const char FromModeLower = 'f';
            const char GlobalMode = 'G';
            const char GlobalModeLower = 'g';

            _targetBlockName = "";
            _targetGridName = "";

            var workingIndex = 0;
            while (true)
            {
                if (workingIndex >= displayName.Length)
                {
                    Write("Parsing Name - End Of String");
                    break;
                }

                var start = displayName.IndexOf(StartBracket, workingIndex);
                if (start == NotFound)
                {
                    Write("Parsing Name - '[' Not Found");
                    break;
                }

                var stop = displayName.IndexOf(StopBracket, start);
                if (stop == NotFound)
                {
                    Write("Parsing Name - Closing ']' Not Found");
                    break;
                }
                var delimiter = displayName.IndexOf(ModeDelimiter, start, stop - start);
                if (delimiter == NotFound)
                {
                    workingIndex = stop + 1; //We jump to stop instead of start because the delimiter does not exist; not because it is invalid
                    Write("Parsing Name - Missing ':' in [] pair");
                    continue;
                }

                //Trims [mode:name] to just mode and name
                var modePart = displayName.Substring(start + 1, delimiter - start - 1);
                var namePart = displayName.Substring(delimiter + 1, stop - delimiter - 1);

                //Parses name
                namePart = namePart.Trim();

                //Parses mode
                //First strip whitespace
                modePart = modePart.Trim();

                if (modePart.Length != 1)
                {
                    //Mode is invalid, advance start and resume search
                    workingIndex = start + 1;
                    Write($"Parsing Name - '{modePart}' is not 1 character.");
                    continue;
                }
                var modeChar = modePart[0];
                switch (modeChar)
                {
                    case ToMode:
                    case ToModeLower:
                        _targetBlockName = namePart;
                        _sending = true;
                        break;
                    case FromMode:
                    case FromModeLower:
                        _targetBlockName = namePart;
                        _sending = false;
                        break;
                    case GlobalMode:
                    case GlobalModeLower:
                        _targetGridName = namePart;
                        break;
                    default:
                        //Mode is invalid char, advance start and resume search
                        workingIndex = start + 1;
                        Write($"Parsing Name - '{modePart}' is not a valid character.");
                        continue;
                }
                workingIndex = stop + 1;

                if (_targetBlockName != "" && _targetGridName != "") break;
            }
            Write("Parsed: " + _targetGridName + ", " + _targetBlockName + ", " + _sending);
            _oldName = displayName;
            return true;
        }

        private IMyEntity GetTarget(ref bool foundGrid)
        {
            Write("Looking for [" + _targetGridName + "] " + _targetBlockName);
            
            var gridsToProcess = new List<IMyCubeGrid>();
            var gridsProcessed = new HashSet<IMyCubeGrid>();
            IMyEntity target = null;
            foundGrid = false;
            
            gridsToProcess.Add(_cargoTeleporter.CubeGrid);

            while (target == null && gridsToProcess.Count > 0)
            {
                var processing = gridsToProcess.Pop();
                if (gridsProcessed.Contains(processing)) continue;
                Write("Processing Grid: \"" + processing.DisplayName + "\"");
                
                var gridBlocks = processing.GetFatBlocks<IMyCubeBlock>().ToArray();

                if (processing.DisplayName == _targetGridName)
                {
                    try
                    {
                        target = gridBlocks.First(x => x?.DisplayNameText == _targetBlockName);
                        foundGrid = true;
                        break;
                    }
                    catch (InvalidOperationException e)
                    {
                        Write(e.ToString());
                    }
                }
                
                foreach (var antenna in gridBlocks.Where(x => x is IMyRadioAntenna && OwnershipUtils.IsSameFactionOrOwner(processing, x)).Cast<IMyRadioAntenna>().Where(x => x.Enabled && x.IsFunctional && x.IsBroadcasting))
                {
                    var sphere = new BoundingSphereD(antenna.GetPosition(), antenna.Radius);
                    var toCheck = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).Where(x => x is IMyCubeGrid).Cast<IMyCubeGrid>().Where(x => !gridsProcessed.Contains(x) && !gridsToProcess.Contains(x) && OwnershipUtils.IsSameFactionOrOwner(processing, x)).ToArray();
                    if (toCheck.Length != 0) Write("Found grids to check: " + string.Join(", ", toCheck.Select(x => "\"" + x.DisplayName + "\"")));
                    gridsToProcess.AddRange(toCheck);
                }

                foreach (var antenna in gridBlocks.Where(x => x is IMyLaserAntenna && OwnershipUtils.IsSameFactionOrOwner(processing, x)).Cast<IMyLaserAntenna>().Where(x => x.Status == MyLaserAntennaStatus.Connected))
                {
                    var sphere = new BoundingSphereD(antenna.TargetCoords, 1);
                    var toCheck = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).Where(x => x is IMyCubeGrid).Cast<IMyCubeGrid>().Where(x => !gridsProcessed.Contains(x) && !gridsToProcess.Contains(x) && OwnershipUtils.IsSameFactionOrOwner(processing, x)).ToArray();
                    if (toCheck.Length != 0) Write("Found grids to check: " + string.Join(", ", toCheck.Select(x => "\"" + x.DisplayName + "\"")));
                    gridsToProcess.AddRange(toCheck);
                }

                gridsProcessed.Add(processing);
            }

            return target;
        }
    }
}