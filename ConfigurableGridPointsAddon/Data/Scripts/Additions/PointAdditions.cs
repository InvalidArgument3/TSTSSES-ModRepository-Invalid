using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;

namespace ShipPoints
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class PointAdditions : MySessionComponentBase
    {
        private readonly Dictionary<string, double> PointValues = new Dictionary<string, double>
        {
            // to show up on the HUD, it needs to be listed here
            //["SmallBlockBatteryBlock"] = 0,
            //["TinyDieselEngine"] = 0,
            //["SmallDieselEngine"] = 0,
            //["MediumDieselEngine"] = 0,


        };

        private readonly Dictionary<string, double> FuzzyPoints = new Dictionary<string, double>();
        private readonly Func<string, MyTuple<string, float>> _climbingCostRename = ClimbingCostRename;

        private static MyTuple<string, float> ClimbingCostRename(string blockDisplayName)
        {
            float costMultiplier = 0f;

            switch (blockDisplayName)
            {
                case "test":
                    blockDisplayName = "test";
                    costMultiplier = 0f;
                    break;
            }

            return new MyTuple<string, float>(blockDisplayName, costMultiplier);
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            // Add fuzzy rules (can be displayname or subtype)
            //FuzzyPoints.Add("aero-wing", 0.33);
            //FuzzyPoints.Add("Control Surface", 0.33);
            //FuzzyPoints.Add("suspension2x2", 0.125);
            //FuzzyPoints.Add("suspension3x3", 0.25);
            //FuzzyPoints.Add("suspension5x5", 0.5);
            //FuzzyPoints.Add("SmallDragWheel", 0.33);

            // Process fuzzy rules
            foreach (var kvp in FuzzyPoints)
            {
                foreach (var block in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    var cubeBlock = block as MyCubeBlockDefinition;
                    if (cubeBlock != null)
                    {
                        // Check if the subtype contains the fuzzy rule key (case-insensitive)
                        if (cubeBlock.Id.SubtypeName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!PointValues.ContainsKey(cubeBlock.Id.SubtypeName))
                            {
                                PointValues[cubeBlock.Id.SubtypeName] = kvp.Value;
                            }
                        }
                        else if (cubeBlock.DisplayNameString != null && cubeBlock.DisplayNameString.Contains(kvp.Key))
                        {
                            if (!PointValues.ContainsKey(cubeBlock.Id.SubtypeName))
                            {
                                PointValues[cubeBlock.Id.SubtypeName] = kvp.Value;
                            }

                        }
                    }
                }
            }

            MyAPIGateway.Utilities.SendModMessage(2546247, PointValues);
            MyAPIGateway.Utilities.SendModMessage(2546247, _climbingCostRename);
        }
    }
}