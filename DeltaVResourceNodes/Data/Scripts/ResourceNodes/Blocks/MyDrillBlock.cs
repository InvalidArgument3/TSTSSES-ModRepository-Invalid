using Math0424.AnimationCore;
using Math0424.Networking;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRage.Voxels;
using VRageMath;
using System.IO;
using Sandbox.Game.EntityComponents;

using VRage.Game.Components;
using VRage.ModAPI;

namespace ResourceNodes
{
    abstract class MyDrillBlock : MyAbstractAnimatedBlock
    {

        private static MyDefinitionId EId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        protected IMyFunctionalBlock Blocc;
        protected IMyInventory Inventory;

        protected int tick = -1;
        protected int timesChecked = 0;
        protected int timesChecked2 = 0;
        protected int slowdown = 1;
        public bool IsProducing;
        protected bool InvFull, InGround;
        protected MyVoxelMaterialDefinition myOre = null;
        List<MyVoxelMaterialDefinition> oreList = new List<MyVoxelMaterialDefinition>();
        protected Action DepositedResources;

        protected int baseSpeed = 5;
        protected int invMultiplier = 8;
        protected int gatherammount = 5;
        protected string Ore = "";

        private readonly Dictionary<byte, int> materials = new Dictionary<byte, int>();

        public DrillStateUpdate state;

        public abstract void SetEmissive(Color color);
        double amount;
        // Define a boolean flag to keep track of whether the function has been executed
        bool hasRunOnce = false;

        protected string oreName = "";

        // Add a volume property
        private float primarySoundVolume = 1.0f;

        private MyEntity3DSoundEmitter soundEmitter; // Sound emitter        

        protected float basePowerDraw = 0.5f; // Base power draw in MW
        protected float maxPowerDraw = 2.0f; // Example max power draw in MW
        int parsedRange;

        int range; // Default value


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            LoadOntoBlock();

            Blocc = (IMyFunctionalBlock)Block;
            Block.UpgradeValues.Add("Productivity", 1f);
            Block.UpgradeValues.Add("Effectiveness", 1f);

            Block.OnClose += RemoveFromMiners;
            Blocc.AppendingCustomInfo += CustomInfo;

            // Initialize sound emitter
            soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Blocc);

            BlockInit();


            // Add controls when the block is initialized
            GSD_Controls.AddControls(ModContext);

            if (string.IsNullOrWhiteSpace(Blocc.CustomData))
            {
                Blocc.CustomData = "Range: 300\nSound: 100\nFilter: 2";
            }


            // Call HandleCustomData to initialize volume from custom data
            HandleCustomData();

            // Subscribe to CustomDataChanged event
            Blocc.CustomDataChanged += HandleCustomDataChanged;
        }


        public abstract void BlockInit();

        public override void BeforeFirstUpdate()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                // Retrieve the current CustomData
                var existingData = Blocc.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var customData = new StringBuilder();
                Blocc.CustomData = "";

                // Preserve existing FilterOre entries and other relevant data
                foreach (var line in existingData)
                {
                    if (!line.StartsWith("InitialRunCompleted"))
                    {
                        //MyAPIGateway.Utilities.ShowMessage("StaticDrill", $"{line}");
                        customData.AppendLine(line);
                        Blocc.CustomData = customData.ToString();
                    }
                }
                MyInventory component = new MyInventory(invMultiplier, new Vector3(1), MyInventoryFlags.CanSend);
                foreach (var i in Inv?.GetItems())
                {
                    if (i != null)
                    {
                        component.AddItems(i.Amount, i.Content);
                    }
                }
                Block.Components.Remove(typeof(MyInventoryBase));
                Block.Components.Add<MyInventoryBase>(component);
            }
            ((MyInventory)Inv).Constraint = new MyInventoryConstraint(MySpaceTexts.ToolTipItemFilter_AnyOre, null, true).AddObjectBuilderType(typeof(MyObjectBuilder_Ore));
        }

        public override void GameUpdate()
        {
            
            tick++;
            if (!MyAPIGateway.Session.IsServer)
            {
                return;
            }

            if (!Blocc.CubeGrid.IsStatic)
            {
                Blocc.Enabled = false;
                oreList.Clear();
            }
            else
            IsProducing = Blocc.Enabled && Blocc.IsWorking && !Inv.IsFull;

            if (tick % 1000 == 0 && timesChecked < 200)
            {
                materials.Clear();
                List<MyVoxelBase> detected = new List<MyVoxelBase>();
                Vector3D position = Block.PositionComp.GetPosition() + (Block.PositionComp.WorldMatrixRef.Down * (Block.BlockDefinition.Size.Y + .25));
                BoundingSphereD boundingSphereD = new BoundingSphereD(position, 2);
                MyGamePruningStructure.GetAllVoxelMapsInSphere(ref boundingSphereD, detected);
                AssignNewMaterial();
                timesChecked++;
            }



            // Example of using the sound emitter in the GameUpdate method
            if (IsProducing)
            {
                if (!soundEmitter.IsPlaying)
                {
                    soundEmitter.PlaySingleSound(new MySoundPair("ToolShipDrillIdle"), true, force3D: true);
                }
                soundEmitter.VolumeMultiplier = primarySoundVolume;

            }
            else
            {
                if (soundEmitter.IsPlaying)
                {
                    soundEmitter.StopSound(forced: false);
                }
            }

            if (IsProducing)
            {
                IsProducing = Block.ResourceSink.IsPoweredByType(EId) && Block.ResourceSink.IsPowerAvailable(EId, Block.ResourceSink.MaxRequiredInput);
            }

            if (tick % 10 == 0)
            {
                DrillStateUpdate packet = new DrillStateUpdate()
                {
                    blockId = Block.EntityId,
                };
                packet.invFull = InvFull;
                packet.oreName = oreList?.Count.ToString() ?? "nothing";
                packet.MinedOreRatio = myOre?.MinedOreRatio.ToString() ?? "0";
                packet.isProducing = IsProducing;
                packet.Productivity = Block.UpgradeValues["Productivity"].ToString();
                packet.Effectiveness = Block.UpgradeValues["Effectiveness"].ToString();
                packet.Count = amount.ToString();
                ResourceNode.Instance.Network.TransmitToPlayersWithinRange(Block.PositionComp.GetPosition(), packet, 1500, false);

                // Call HandleCustomData to initialize volume from custom data
                HandleCustomData();




                if (Block.IsBuilt)
                {
                    if (!Block.IsFunctional)
                    {
                        SetEmissive(Color.Orange);
                    }
                    else
                    {
                        if (myOre == null || !Blocc.Enabled || !Blocc.IsWorking)
                        {
                            SetEmissive(Color.Red);
                        }
                        else if (!IsProducing || myOre == null || Inv.IsFull || InvFull)
                        {
                            SetEmissive(Color.Yellow);
                        }
                    }
                }
                else
                {
                    if (myOre == null)
                    {
                        SetEmissive(Color.Red);
                    }
                    else if (myOre != null)
                    {
                        SetEmissive(Color.Aqua);
                    }
                    else
                    {
                        SetEmissive(Color.Yellow);
                    }
                }
            }

            if (IsProducing)
            {
                int filterType = 1;
                var filterOres = new HashSet<string>();

                var customData = Blocc.CustomData?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (customData != null)
                {
                    foreach (var line in customData)
                    {
                        if (line.StartsWith("Filter:"))
                        {
                            int type;
                            if (int.TryParse(line.Substring("Filter:".Length), out type))
                            {
                                filterType = type;
                            }
                        }
                        else if (line.StartsWith("FilterOre:"))
                        {
                            filterOres.Add(line.Substring("FilterOre:".Length).Trim());
                        }
                    }
                }
                if (myOre != null)
                {
                    int speed = (int)(baseSpeed / Block.UpgradeValues["Productivity"]) + slowdown;

                    if (tick % speed == 0)
                    {
                        float yield = 10 * Block.UpgradeValues["Effectiveness"];
                        foreach (var oreDefinition in oreList)
                        {
                            MyObjectBuilder_Ore oreObject = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(oreDefinition.MinedOre);

                            float newammount = gatherammount;
                            float amount = newammount * (yield * oreDefinition.MinedOreRatio);
                            bool InvFull = !Inv.CanItemsBeAdded((MyFixedPoint)amount, oreObject);

                            if (!InvFull)
                            {
                                if (filterType == 1)
                                {
                                    if (filterOres.Contains(oreObject.SubtypeName))
                                    {
                                        Inv.AddItems((MyFixedPoint)amount, oreObject);
                                        DepositedResources?.Invoke();
                                    }

                                }
                                else if (filterType == 2)
                                {
                                    if (!filterOres.Contains(oreObject.SubtypeName))
                                    {
                                        Inv.AddItems((MyFixedPoint)amount, oreObject);
                                        DepositedResources?.Invoke();
                                    }
                                }
                            }
                        }

                    }
                }
            }
        }

        private void AssignNewMaterial()
        {
            Vector3D position = Block.PositionComp.GetPosition();
            bool hasRangeChanged = false; // Flag to track if the range has changed

            // Extract Range value from CustomData
            var existingCustomData = Blocc.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in existingCustomData)
            {
                if (line.StartsWith("Range:"))
                {
                    int parsedRange;
                    if (int.TryParse(line.Substring("Range:".Length).Trim(), out parsedRange))
                    {
                        range = parsedRange;
                    }
                    break;
                }
            }
            //MyAPIGateway.Utilities.ShowMessage("StaticDrill", $"{Blocc.CustomName} {parsedRange} {range}");


            // Only proceed if the range has changed or if it's the first run
            if (!existingCustomData.Contains("InitialRunCompleted"))
            {
               int output = MapRange(range);
                int stepSize = output;
                //MyAPIGateway.Utilities.ShowMessage("OreDetector", $"Step Count: {output.ToString()}");


                for (int x = -100; x <= 100; x += stepSize)
                {
                    for (int y = -100; y <= 100; y += stepSize)
                    {
                        for (int z = -range; z <= range; z += stepSize)
                        {
                            List<MyVoxelBase> detected = new List<MyVoxelBase>();
                            Vector3D scanPosition = position + new Vector3D(x, y, z);
                            BoundingSphereD boundingSphereD = new BoundingSphereD(scanPosition, stepSize*10); // Adjust sphere radius accordingly
                            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref boundingSphereD, detected);

                            foreach (var map in detected)
                            {
                                GetResources(scanPosition, map);
                            }

                            detected.Clear(); // Clear the list after each scan to avoid unnecessary memory usage
                        }
                    }
                }

                // Sort materials and pick ores
                var options = new Dictionary<MyVoxelMaterialDefinition, int>();

                foreach (var m in materials.Keys)
                {
                    var def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m);
                    if (def != null)
                    {
                        if (def.CanBeHarvested && !string.IsNullOrEmpty(def.MinedOre) && !ResourceNode.Instance.MiningBlacklist.Contains(def.MinedOre))
                        {
                            options.Add(def, materials[m]);
                        }
                    }
                }

                if (options.Count == 0 && materials.Count >= 1)
                {
                    var e = materials.Keys.GetEnumerator();
                    e.MoveNext();
                    var def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(e.Current);
                    if (def != null)
                    {
                        options.Add(def, materials[e.Current]);
                    }
                }

                if (oreList.Count > 0)
                {
                    oreList.Clear();
                }

                // Pick top value and add it to the producer
                MyVoxelMaterialDefinition top = null;

                var customData = new StringBuilder();
                var uniqueOres = new HashSet<string>();
                var uniqueFilterOres = new HashSet<string>();

                string existingRange = null;
                string existingSound = null;
                string existingFilter = null;

                // Read existing custom data to preserve Range, Sound, and Filter entries
                var existingData = Blocc.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in existingData)
                {
                    if (line.StartsWith("Range:"))
                    {
                        existingRange = line;
                    }
                    else if (line.StartsWith("Sound:"))
                    {
                        existingSound = line;
                    }
                    else if (line.StartsWith("Filter:"))
                    {
                        existingFilter = line;
                    }
                    else if (line.StartsWith("FilterOre:"))
                    {
                        uniqueFilterOres.Add(line.Substring("FilterOre:".Length));
                        customData.AppendLine(line);
                    }
                    else if (!line.StartsWith("FoundOre:") || uniqueOres.Contains(line.Substring("FoundOre:".Length)))
                    {
                        uniqueOres.Add(line.Substring("FoundOre:".Length));
                        customData.AppendLine(line);
                    }
                }

                // Process options and add unique FoundOre entries
                foreach (var m in options)
                {
                    if (Ore == "Stone" && m.Key.MinedOre.ToString() == "Stone")
                    {
                        top = m.Key;
                        oreList.Clear();
                        oreList.Add(m.Key);
                        // Ensure no duplicates in custom data
                        if (uniqueOres.Add(m.Key.MinedOre.ToString()))
                        {
                            customData.AppendLine("FoundOre:" + m.Key.MinedOre.ToString());
                        }
                    }
                    else if (Ore != "Stone" && m.Key.MinedOre.ToString() != "Stone")
                    {
                        top = top ?? m.Key;
                        oreList.Add(m.Key);
                        // Ensure no duplicates in custom data
                        if (uniqueOres.Add(m.Key.MinedOre.ToString()))
                        {
                            customData.AppendLine("FoundOre:" + m.Key.MinedOre.ToString());
                            // MyAPIGateway.Utilities.ShowMessage("OreDetector", $"Ore detected at {m.Key.MinedOre.ToString()}");
                        }
                    }
                }

                // Append preserved Range, Sound, and Filter entries
                if (existingRange != null) customData.AppendLine(existingRange);
                if (existingSound != null) customData.AppendLine(existingSound);
                if (existingFilter != null) customData.AppendLine(existingFilter);

                if (!existingCustomData.Contains("InitialRunCompleted"))
                {
                    // Mark the initial run as completed
                    customData.AppendLine("InitialRunCompleted");
                }
                Blocc.CustomData = customData.ToString();

                RemoveFromMiners(Block);
                myOre = top;

                // Store the ore name in the custom data
                oreName = myOre != null ? myOre.MinedOre : "None";
                AddToMiners();
                var fb = Blocc as IMyFunctionalBlock;
                if (fb != null)
                {
                    fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
                    fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
                }
            }

        }
        private void CustomInfo(IMyTerminalBlock block, StringBuilder builder)
        {
            if (state != null)
            {
                builder.Clear();
                builder.Append($"\nExtracting \n\"{state.oreName}\" Ores");
                builder.Append($"\nProductivity: {state.Productivity}");
                builder.Append($"\nEffectiveness: {state.Effectiveness}");
                builder.AppendLine("\nVolume: " + (primarySoundVolume * 100).ToString("F0")); // Display volume as 0-100
            }
        }
        protected void UpdateCustomInfo()
        {
            Blocc.RefreshCustomInfo();
        }

        private void RemoveFromMiners(MyEntity e)
        {
            if (myOre != null && !string.IsNullOrEmpty(myOre.MinedOre))
            {
                if (ResourceNode.Instance.Miners.ContainsKey(myOre.MinedOre))
                {
                    ResourceNode.Instance.Miners[myOre.MinedOre].Remove(e.EntityId);
                    ResourceNode.Instance.Locations.Remove(e.EntityId);
                }
            }
        }

        private void AddToMiners()
        {
            if (myOre != null && !string.IsNullOrEmpty(myOre.MinedOre))
            {
                if (!ResourceNode.Instance.Miners.ContainsKey(myOre.MinedOre))
                {
                    ResourceNode.Instance.Miners.Add(myOre.MinedOre, new HashSet<long>());
                }
                ResourceNode.Instance.Miners[myOre.MinedOre].Remove(Block.EntityId);
                ResourceNode.Instance.Miners[myOre.MinedOre].Add(Block.EntityId);

                ResourceNode.Instance.Locations.Remove(Block.EntityId);
                ResourceNode.Instance.Locations.Add(Block.EntityId, Block.PositionComp.GetPosition());
            }
        }

        private void GetResources(Vector3D pos, MyVoxelBase map)
        {
            MyStorageData cache = new MyStorageData(MyStorageDataTypeFlags.ContentAndMaterial);
            cache.Resize(new Vector3I(1));

            Vector3I voxelPos;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(map.PositionLeftBottomCorner, ref pos, out voxelPos);
            map.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, voxelPos, voxelPos);

            if (cache.Material(0) != 255)
            {
                if (materials.ContainsKey(cache.Material(0)))
                {
                    materials[cache.Material(0)] += cache.Content(0);
                }
                else
                {
                    materials.Add(cache.Material(0), cache.Content(0));
                }
            }
        }
        private void HandleCustomDataChanged(IMyTerminalBlock block)
        {
            // Call HandleCustomData when custom data changes
            HandleCustomData();
        }
        public void HandleCustomData()
        {
            // Extract the sound volume from the custom data
            var existingCustomData = Blocc.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in existingCustomData)
            {
                if (line.StartsWith("sound:", StringComparison.OrdinalIgnoreCase))
                {
                    float parsedVolume;
                    if (float.TryParse(line.Substring("sound:".Length).Trim(), out parsedVolume))
                    {
                        primarySoundVolume = MathHelper.Clamp(parsedVolume / 100.0f, 0.0f, 1.0f); // Convert 0-100 to 0.0-1.0 and clamp between 0 and 1
                    }
                    else
                    {
                        primarySoundVolume = 1.0f; // Default value if parsing fails
                    }
                    break;
                }
            }

            int range = 30; // Default value

            // Extract Range value from CustomData
            foreach (var line in existingCustomData)
            {
                if (line.StartsWith("Range:"))
                {
                    int parsedRange;
                    if (int.TryParse(line.Substring("Range:".Length).Trim(), out parsedRange))
                    {
                        range = parsedRange;
                    }
                    break;
                }
            }


            // Adjust the power requirement based on the range
            float powerDraw = basePowerDraw + (range * 0.01f); // Increase power draw by 0.1 MW per unit of range
            powerDraw = Math.Min(powerDraw, maxPowerDraw); // Ensure the power draw does not exceed the max power draw

            Block.ResourceSink.SetMaxRequiredInputByType(EId, powerDraw);


            // Update the custom info to reflect the new volume
            UpdateCustomInfo();

            // Apply volume to the sound emitter
            if (soundEmitter != null)
            {
                soundEmitter.VolumeMultiplier = primarySoundVolume;
            }
        }
        public static int MapRange(int x)
        {
            // Define the original range and the target range
            int x1 = 30, x2 = 740;
            int y1 = 2, y2 = 25;
            
            // Define the transition point (300) and the value for x=300 (we want it to be 15)
            int transitionPoint = 300;
            int targetAtTransition = 8;

            // Calculate the slope for the linear interpolation
            float m = (float)(y2 - y1) / (x2 - x1);

            int y;

            // If x is less than or equal to the transition point, apply a slow growth using a logarithmic scale
            if (x <= transitionPoint)
            {
                // Adjust the scaling factor so that x = 300 results in 15
                float logValue = (float)(Math.Log(x - x1 + 1) / Math.Log(transitionPoint - x1 + 1)) * (targetAtTransition - y1) + y1;
                y = (int)logValue;
            }
            else
            {
                // For x > 300, use linear interpolation
                y = (int)(m * (x - x1) + y1);
            }

            // Ensure the result is within the target range [1, 50]
            y = Math.Min(Math.Max(y, y1), y2);

            return y;
        }


        protected IMyInventory Inv
        {
            get { return Blocc.GetInventory(0); }
        }

    }
}
