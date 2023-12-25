﻿using CoreSystems.Api;
using Modular_Weaponry.Data.Scripts.WeaponScripts.DebugDraw;
using Modular_Weaponry.Data.Scripts.WeaponScripts.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Modular_Weaponry.Data.Scripts.WeaponScripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WeaponPartGetter : MySessionComponentBase
    {
        public static WeaponPartGetter Instance; // the only way to access session comp from other classes and the only accepted static field.
        public Dictionary<IMySlimBlock, WeaponPart> AllWeaponParts = new Dictionary<IMySlimBlock, WeaponPart>();
        public Dictionary<int, PhysicalWeapon> AllPhysicalWeapons = new Dictionary<int, PhysicalWeapon>();
        public int NumPhysicalWeapons = 0;

        public List<IMySlimBlock> QueuedBlockAdds = new List<IMySlimBlock>();
        public List<WeaponPart> QueuedConnectionChecks = new List<WeaponPart>();
        public Dictionary<WeaponPart, PhysicalWeapon> QueuedWeaponChecks = new Dictionary<WeaponPart, PhysicalWeapon>();

        public WcApi wAPI = new WcApi();

        public override void LoadData()
        {
            Instance = this;
            MyAPIGateway.Entities.OnEntityAdd += OnGridAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnGridRemove;
            wAPI.Load();
        }

        protected override void UnloadData()
        {
            Instance = null; // important for avoiding this object to remain allocated in memory
            MyAPIGateway.Entities.OnEntityAdd -= OnGridAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnGridRemove;
            wAPI.Unload();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            // Queue gridadds to account for world load/grid pasting
            foreach (var queuedBlock in QueuedBlockAdds.ToList())
            {
                OnBlockAdd(queuedBlock);
                QueuedBlockAdds.Remove(queuedBlock);
            }

            // Queue partadds to account for world load/grid pasting
            foreach (var queuedPart in QueuedConnectionChecks.ToList())
            {
                queuedPart.CheckForExistingWeapon();
                QueuedConnectionChecks.Remove(queuedPart);
            }

            // Queue weapon pathing to account for world load/grid pasting
            foreach (var queuedWeapon in QueuedWeaponChecks.Keys.ToList())
            {
                QueuedWeaponChecks[queuedWeapon].RecursiveWeaponChecker(queuedWeapon);
                QueuedWeaponChecks.Remove(queuedWeapon);
            }

            foreach (var weapon in AllPhysicalWeapons.Values)
                weapon.Update();

            MyAPIGateway.Utilities.ShowNotification("Weapons: " + AllPhysicalWeapons.Count + " | Parts: " + AllWeaponParts.Count, 1000 / 60);
        }

        private void OnGridAdd(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            IMyCubeGrid grid = (IMyCubeGrid) entity;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            grid.OnBlockAdded += OnBlockAdd;
            grid.OnBlockRemoved += OnBlockRemove;

            List<IMySlimBlock> existingBlocks = new List<IMySlimBlock>();
            grid.GetBlocks(existingBlocks);
            foreach (var block in existingBlocks)
                QueuedBlockAdds.Add(block);
        }

        private void OnBlockAdd(IMySlimBlock block)
        {
            foreach (var modularDefinition in DefinitionHandler.Instance.ModularDefinitions)
            {
                if (!modularDefinition.IsBlockAllowed(block))
                    return;

                WeaponPart w = new WeaponPart(block, modularDefinition);
            }
        }

        private void OnGridRemove(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
                return;

            IMyCubeGrid grid = (IMyCubeGrid)entity;
            grid.OnBlockAdded -= OnBlockAdd;
            grid.OnBlockRemoved -= OnBlockRemove;

            // Exclude projected and held grids
            if (grid.Physics == null)
                return;

            List<WeaponPart> toRemove = new List<WeaponPart>();
            foreach (var partKvp in AllWeaponParts)
            {
                if (partKvp.Key.CubeGrid == grid)
                {
                    toRemove.Add(partKvp.Value);
                }
            }

            foreach (var deadPart in toRemove)
            {
                deadPart.memberWeapon?.Close();
                AllWeaponParts.Remove(deadPart.block);
            }
        }

        private void OnBlockRemove(IMySlimBlock block)
        {
            WeaponPart part;
            if (AllWeaponParts.TryGetValue(block, out part))
            {
                //MyAPIGateway.Utilities.ShowNotification("Removing");
                part.memberWeapon?.Remove(part);
                AllWeaponParts.Remove(block);
            }
        }
    }
}
