using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage.Utils;
using System.Text;
using System.IO;
using System;

namespace ResourceNodes
{
    public static class GSD_Controls
    {
        private static bool controlsAdded = false;
        // Add fields to store range and sound settings
        private static Dictionary<IMyTerminalBlock, float> blockRanges = new Dictionary<IMyTerminalBlock, float>();
        private static Dictionary<IMyTerminalBlock, float> blockSounds = new Dictionary<IMyTerminalBlock, float>();
        private static Dictionary<IMyTerminalBlock, long> blockFilter = new Dictionary<IMyTerminalBlock, long>();
        private static Dictionary<IMyTerminalBlock, HashSet<string>> blockFilterOres = new Dictionary<IMyTerminalBlock, HashSet<string>>();
        private static HashSet<string> foundselectedOres = new HashSet<string>(); // Store selected ores temporarily
        private static HashSet<string> filterselectedOres = new HashSet<string>(); // Store selected ores temporarily



        public static void AddControls(IMyModContext context)
        {

            if (controlsAdded)
                return;

            controlsAdded = true;

            var myComboBox = CreateComboBox("Filter", "Whitelist or Blacklist", GetCombobox, SetCombobox, SetComboboxContent);

            var filterList = CreateListBox("filterList", "Filter Items", "Items on the Filter.", GetFilterListContent);
            filterList.ItemSelected = FilterListItemSelected;

            var removeButton = CreateButton("removeButton", "Remove", RemoveFromFilter);

            var foundList = CreateListBox("foundList", "Found Ores", "Ores the Drill Found.", GetFoundOresContent);
            foundList.ItemSelected = FoundListItemSelected;

            var addButton = CreateButton("addButton", "Add", AddToFilter);

            var rangeSlider = CreateSlider("rangeSlider", "Depth:", 30f, 740f, GetRange, SetRange, RangeSliderWriter);

            var soundSlider = CreateSlider("SoundSlider", "Sound:", 0f, 100f, GetSound, SetSound, SoundSliderWriter);
            
            var scanButton = CreateButton("scanButton", "Scan", Scan);

            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(scanButton);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(myComboBox);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(filterList);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(removeButton);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(foundList);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(addButton);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(rangeSlider);
            MyAPIGateway.TerminalControls.AddControl<IMyShipDrill>(soundSlider);


        }

        private static IMyTerminalControlCombobox CreateComboBox(string id, string title, Func<IMyTerminalBlock, long> getter, Action<IMyTerminalBlock, long> setter, Action<List<MyTerminalControlComboBoxItem>> content)
        {
            var comboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyShipDrill>(id);
            comboBox.Title = MyStringId.GetOrCompute(title);
            comboBox.Tooltip = MyStringId.GetOrCompute(title);
            comboBox.SupportsMultipleBlocks = false;
            comboBox.Getter = getter;
            comboBox.Setter = setter;
            comboBox.ComboBoxContent = content;
            return comboBox;
        }

        private static IMyTerminalControlListbox CreateListBox(string id, string title, string tooltip, Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> content)
        {
            var listBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyShipDrill>(id);
            listBox.Title = MyStringId.GetOrCompute(title);
            listBox.Tooltip = MyStringId.GetOrCompute(tooltip);
            listBox.Visible = block => true;
            listBox.Enabled = block => true;
            listBox.VisibleRowsCount = 5;
            listBox.Multiselect = true;
            listBox.SupportsMultipleBlocks = false;
            listBox.ListContent = content;
            return listBox;
        }

        private static IMyTerminalControlButton CreateButton(string id, string title, Action<IMyTerminalBlock> action)
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyShipDrill>(id);
            button.Title = MyStringId.GetOrCompute(title);
            button.Visible = block => true;
            button.Enabled = block => true;
            button.SupportsMultipleBlocks = false;
            button.Action = action;
            return button;
        }

        private static IMyTerminalControlSlider CreateSlider(string id, string title, float min, float max, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer)
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyShipDrill>(id);
            slider.Title = MyStringId.GetOrCompute(title);
            slider.SetLogLimits(min, max);
            slider.SetLimits(min, max);
            slider.SupportsMultipleBlocks = false;
            slider.Getter = getter;
            slider.Setter = setter;
            slider.Writer = writer;
            return slider;
        }
        private static void GetFilterListContent(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> content, List<MyTerminalControlListBoxItem> selectedItems)
        {
            content.Clear(); // Clear the existing content

            // Read the CustomData from the block
            var customData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Loop through each line and find FoundOre entries
            foreach (var line in customData)
            {
                if (line.StartsWith("FilterOre:"))
                {
                    // Extract the ore name and add it to the content list
                    var oreName = line.Substring("FilterOre:".Length).Trim();
                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(oreName), MyStringId.NullOrEmpty, oreName);
                    content.Add(item);
                }
            }
        }
        // Method to handle item selection in Found Ores list
        private static void FoundListItemSelected(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems)
        {
            // Store selected items to the temporary list
            foundselectedOres.Clear();
            foreach (var item in selectedItems)
            {
                foundselectedOres.Add(item.UserData.ToString());
            }
        }
        // Method to handle item selection in Found Ores list
        private static void FilterListItemSelected(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems)
        {
            // Store selected items to the temporary list
            filterselectedOres.Clear();
            foreach (var item in selectedItems)
            {
                filterselectedOres.Add(item.UserData.ToString());
            }
        }

        private static void GetFoundOresContent(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> content, List<MyTerminalControlListBoxItem> selectedItems)
        {
            content.Clear(); // Clear the existing content

            // Read the CustomData from the block
            var customData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Loop through each line and find FoundOre entries
            foreach (var line in customData)
            {
                if (line.StartsWith("FoundOre:"))
                {
                    // Extract the ore name and add it to the content list
                    var oreName = line.Substring("FoundOre:".Length).Trim();
                    var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(oreName), MyStringId.NullOrEmpty, oreName);
                    content.Add(item);
                }
            }
        }

        // Method to remove selected items from Filter list
        public static void RemoveFromFilter(IMyTerminalBlock block)
        {
            // Retrieve the current CustomData
            var existingData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var customData = new StringBuilder();

            // Preserve existing FilterOre entries and other relevant data
            foreach (var line in existingData)
            {
                bool shouldRemove = false;
                if (line.StartsWith("FilterOre:"))
                {
                    foreach (var selectedItem in filterselectedOres)
                    {
                        if (line.Contains(selectedItem))
                        {
                            shouldRemove = true;
                            break;
                        }
                    }
                }

                if (!shouldRemove)
                {
                    customData.AppendLine(line);
                }
            }

            // Update the block's CustomData with the new filter list
            block.CustomData = customData.ToString();

            // Optionally, you could call SaveCustomData(block) if you want to ensure other settings are persisted
            SaveCustomData(block);

            // Clear the selected ores after adding them to the filter
            filterselectedOres.Clear();

            // Toggle the ShowInToolbarConfig to force an update
            var fb = block as IMyFunctionalBlock;
            if (fb != null)
            {
                fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
                fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
            }
        }

        // Method to add selected items from Found Ore list to Filter list
        private static void AddToFilter(IMyTerminalBlock block)
        {
            // Retrieve the current CustomData
            var existingData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var customData = new StringBuilder();

            // Preserve existing FilterOre entries
            var existingFilterOres = new HashSet<string>();

            foreach (var line in existingData)
            {
                if (line.StartsWith("FilterOre:"))
                {
                    var oreName = line.Substring("FilterOre:".Length).Trim();
                    existingFilterOres.Add(oreName);
                    customData.AppendLine(line); // Keep existing FilterOre lines
                }
                else
                {
                    customData.AppendLine(line); // Keep other lines
                }
            }

            // Add new ores to the filter without duplicates
            foreach (var selectedItem in foundselectedOres)
            {
                if (!existingFilterOres.Contains(selectedItem))
                {
                    customData.AppendLine("FilterOre:" + selectedItem);
                    existingFilterOres.Add(selectedItem); // Update the existing filter ores set
                }
            }

            // Update the block's CustomData with the new filter list
            block.CustomData = customData.ToString();

            // Optionally, you could call SaveCustomData(block) if you want to ensure other settings are persisted
            SaveCustomData(block);

            // Clear the selected ores after adding them to the filter
            foundselectedOres.Clear();
            // Toggle the ShowInToolbarConfig to force an update
            var fb = block as IMyFunctionalBlock;
            if (fb != null)
            {
                fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
                fb.ShowInToolbarConfig = !fb.ShowInToolbarConfig;
            }

        }

        public static void SetComboboxContent(List<MyTerminalControlComboBoxItem> list)
        {
            list.Add(new MyTerminalControlComboBoxItem { Key = 1, Value = VRage.Utils.MyStringId.GetOrCompute("Whitelist") });
            list.Add(new MyTerminalControlComboBoxItem { Key = 2, Value = VRage.Utils.MyStringId.GetOrCompute("Blacklist") });
        }

        private static long GetCombobox(IMyTerminalBlock block)
        {
            // Load the custom data from the block if not already loaded
            LoadCustomData(block);

            if (blockFilter.ContainsKey(block))
            {
                return blockFilter[block];
            }
            else
            {
                // If Filter is not set, initialize it with default value and return
                blockFilter[block] = 2;
                return blockFilter[block];
            }
        }

        private static void SetCombobox(IMyTerminalBlock block, long key)
        {
            blockFilter[block] = key;
            SaveCustomData(block);
        }

        private static void SetRange(IMyTerminalBlock block, float range)
        {
            float roundedRange = (float)Math.Round(range); // Round the range to the nearest whole number
            blockRanges[block] = roundedRange;
            SaveCustomData(block);
        }

        private static float GetRange(IMyTerminalBlock block)
        {
            if (blockRanges.ContainsKey(block))
            {
                return blockRanges[block];
            }
            else
            {
                // If range is not set, initialize it with default value and return
                blockRanges[block] = 300f;
                LoadCustomData(block);
                return blockRanges[block];
            }
        }

        private static void RangeSliderWriter(IMyTerminalBlock block, StringBuilder builder)
        {
            double result = Math.Round(GetRange(block));
            builder.Append(result.ToString() + "m");
        }

        private static void SetSound(IMyTerminalBlock block, float sound)
        {
            float roundedSound = (float)Math.Round(sound); // Round the range to the nearest whole number
            blockSounds[block] = roundedSound;
            SaveCustomData(block);

        }
        private static float GetSound(IMyTerminalBlock block)
        {
            if (blockSounds.ContainsKey(block))
            {
                return blockSounds[block];
            }
            else
            {
                // If sound is not set, initialize it with default value and return
                blockSounds[block] = 100f;
                LoadCustomData(block);
                return blockSounds[block];
            }
        }

        private static void SoundSliderWriter(IMyTerminalBlock block, StringBuilder builder)
        {
            builder.Append(Math.Round(GetSound(block)).ToString() + "%");
        }

        // Method to save custom data to the block without overriding FoundOre entries
        private static void SaveCustomData(IMyTerminalBlock block)
        {
            var existingData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var customData = new StringBuilder();

            // Preserve existing FoundOre and FilterOre entries
            foreach (var line in existingData)
            {
                if (line.StartsWith("FoundOre:") || line.StartsWith("FilterOre:"))
                {
                    customData.AppendLine(line);
                }
            }

            // Add or update Range, Sound, Filter, and FilterItems entries
            bool rangeUpdated = false, soundUpdated = false, filterUpdated = false;

            foreach (var line in existingData)
            {
                if (line.StartsWith("Range:"))
                {
                    customData.AppendLine("Range:" + blockRanges[block]);
                    rangeUpdated = true;
                }
                else if (line.StartsWith("Sound:"))
                {
                    customData.AppendLine("Sound:" + blockSounds[block]);
                    soundUpdated = true;
                }
                else if (line.StartsWith("Filter:"))
                {
                    customData.AppendLine("Filter:" + blockFilter[block]);
                    filterUpdated = true;
                }
            }

            if (!rangeUpdated) customData.AppendLine("Range:" + blockRanges[block]);
            if (!soundUpdated) customData.AppendLine("Sound:" + blockSounds[block]);
            if (!filterUpdated) customData.AppendLine("Filter:" + blockFilter[block]);

            block.CustomData = customData.ToString();
        }
        
        private static void Scan(IMyTerminalBlock block)
        {
            
            // Retrieve the current CustomData
            var existingData = block.CustomData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var customData = new StringBuilder();
            block.CustomData = "";

            // Preserve existing FilterOre entries and other relevant data
            foreach (var line in existingData){
                 if (!line.StartsWith("InitialRunCompleted")){
                    //MyAPIGateway.Utilities.ShowMessage("StaticDrill", $"{line}");
                    customData.AppendLine(line);
                    block.CustomData = customData.ToString();
                 }
            }

        }


        private static void LoadCustomData(IMyTerminalBlock block)
        {
            if (string.IsNullOrEmpty(block.CustomData))
                return;

            var customDataLines = block.CustomData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in customDataLines)
            {
                var keyValue = line.Split(':');
                if (keyValue.Length != 2)
                    continue;

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                float floatValue;
                long longValue;
                switch (key)
                {
                    case "Range":
                        if (float.TryParse(value, out floatValue))
                        {
                            blockRanges[block] = floatValue;
                        }
                        break;
                    case "Sound":
                        if (float.TryParse(value, out floatValue))
                        {
                            blockSounds[block] = floatValue;
                        }
                        break;
                    case "Filter":
                        if (long.TryParse(value, out longValue))
                        {
                            blockFilter[block] = longValue;
                        }
                        break;
                }
            }
        }
    }
}