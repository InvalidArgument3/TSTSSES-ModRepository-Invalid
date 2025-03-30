using ParallelTasks;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace CustomHangar
{
    public class MassGathererWorkData : WorkData
    {
        public List<MyCubeGrid> grids = new List<MyCubeGrid>();
        public double massSum = 0;
        public long factionWalletData = -1;
        public IMyFaction faction = null;
        public long playerWalletData = -1;

        public void ScanGridMassAction(WorkData data)
        {
            int attempts = 0;
            var workData = (MassGathererWorkData)data;

            var blocks = new List<IMySlimBlock>();
            foreach (var cubeGrid in workData.grids)
            {
                if (cubeGrid.MarkedForClose)
                    continue;

                blocks.Clear();
                blocks.EnsureCapacity(cubeGrid.BlocksCount);
                ((IMyCubeGrid)cubeGrid).GetBlocks(blocks);
                GatherMass(blocks, workData);
            }

            GatherWalletData:
            if (!GatherWalletData(workData))
                MyAPIGateway.Parallel.Sleep(500);
            else
                return;

            if (attempts > 5)
                return;

            attempts++;
            goto GatherWalletData;
        }

        private void GatherMass(List<IMySlimBlock> blocks, MassGathererWorkData workData)
        {
            foreach (var block in blocks)
            {
                var fat = block.FatBlock;
                if (fat != null)
                {
                    var remote = fat as IMyRemoteControl;
                    var flightControl = fat as IMyFlightMovementBlock;
                    if (remote != null)
                        remote.SetAutoPilotEnabled(false);

                    if (flightControl != null)
                        flightControl.Enabled = false;
                }

                workData.massSum += block.Mass;
                if (block.FatBlock?.GetInventory() != null)
                {
                    var fatBlock = block.FatBlock;
                    for (int i = 0; i < fatBlock.InventoryCount; i++)
                    {
                        var inv = (MyInventory)fatBlock.GetInventory(i);
                        if (inv.ExternalMass != 0)
                        {
                            workData.massSum += (double)(inv.CurrentMass - inv.ExternalMass);
                        }
                        else
                        {
                            workData.massSum += (double)inv.CurrentMass;
                        }
                    }
                }
            }
        }

        public void ScanGridMassCallback(WorkData data)
        {
            var workData = (MassGathererWorkData)data;
            Session.Instance.previewMass = (float)workData.massSum;

            foreach(var grid in workData.grids)
            {
                MyAPIGateway.Entities.AddEntity(grid, true);
                Session.Instance.previewGrids.Add(grid);
            }

            Session.Instance.factionWallet = workData.factionWalletData;
            Session.Instance.playerWallet = workData.playerWalletData;
            Session.Instance.ToolEquipped(Session.Instance.playerCache.IdentityId, "", "");
        }

        private bool GatherWalletData(MassGathererWorkData workData)
        {
            if (workData.faction != null)
                if (!workData.faction.TryGetBalanceInfo(out workData.factionWalletData)) return false;

            if (!Session.Instance.playerCache.TryGetBalanceInfo(out workData.playerWalletData)) return false;

            if (workData.faction != null)
                if (workData.factionWalletData != -1 && workData.playerWalletData != -1) return true;

            if (workData.playerWalletData != -1) return true;

            return false;
        }
    }
}
