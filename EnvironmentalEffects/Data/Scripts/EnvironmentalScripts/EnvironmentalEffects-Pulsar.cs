using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using DefenseShields;
using Sandbox.Game;
using System;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class EnvEffectsPulsar : MySessionComponentBase
{
    private const float DurabilityModifier = 0.1f;
    private bool _isInitialized = false;
    private ShieldApi _shieldApi;
    private Queue<IMyCubeGrid> gridQueue = new Queue<IMyCubeGrid>();
    private DateTime lastQuestLogUpdate = DateTime.MinValue;


    public override void UpdateAfterSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            _shieldApi = new ShieldApi();
            _shieldApi.Load(); // Initialize the API
            Initialize();
            MyVisualScriptLogicProvider.SetQuestlog(true, "Durability and Shields Status");
            _isInitialized = true;
        }

        if (_isInitialized)
        {
            // Apply durability modifier
            IMyCubeGrid grid;
            for (int i = 0; i < 10 && gridQueue.Count > 0; i++)
            {
                grid = gridQueue.Dequeue();
                ApplyDurabilityModifier(grid, DurabilityModifier);
            }

            // Heal shields
            if (MyAPIGateway.Session.GameplayFrameCounter % 600 == 0)
            {
                HealShields();
            }


            // Quest log update
            if (DateTime.Now - lastQuestLogUpdate > TimeSpan.FromSeconds(10))
            {
                MyVisualScriptLogicProvider.RemoveQuestlogDetails();
                lastQuestLogUpdate = DateTime.Now;
            }



        }
    }

    private void Initialize()
    {
        MyVisualScriptLogicProvider.SetQuestlog(true, "Durability and Shields Status");
    }


    private void ApplyDurabilityModifier(IMyCubeGrid grid, float modifier)
    {
        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        grid.GetBlocks(blocks);

        foreach (IMySlimBlock block in blocks)
        {
            block.BlockGeneralDamageModifier = modifier;
        }

        var message = $"Durability modifier of {modifier}x applied to {grid.DisplayName}";
        MyVisualScriptLogicProvider.AddQuestlogObjective(message, false, false);
    }

    private void HealShields()
    {
        var allEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allEntities, e => e is IMyCubeGrid);

        foreach (IMyEntity entity in allEntities)
        {
            var grid = entity as IMyCubeGrid;
            if (grid != null)
            {
                var shieldBlock = _shieldApi.GetShieldBlock(grid);
                if (shieldBlock != null)
                {
                    float healAmount = 1000000; // Assuming 1 million shield HP to heal
                    _shieldApi.SetCharge(shieldBlock, _shieldApi.GetCharge(shieldBlock) + healAmount);

                    var message = $"Healed 1 million HP on shields of {grid.DisplayName}";
                    MyVisualScriptLogicProvider.AddQuestlogObjective(message, false, false);
                }
            }
        }
    }




    protected override void UnloadData()
    {
        // Unregister the OnEntityAdd event

        // Perform additional clean-up if necessary
        // For example, if you had any static references or other persistent data, you would clean them up here.

        // Call base UnloadData to ensure any base logic is executed
        base.UnloadData();
    }

}
