using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using DefenseShields;
using Sandbox.Game;
using System;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class DurabilityModifierSession : MySessionComponentBase
{
    private const float DurabilityModifier = 0.1f;
    private bool _isInitialized = false;
    private bool _isApiInitialized = false;
    private ShieldApi _shieldApi;
    private Queue<IMyCubeGrid> gridQueue = new Queue<IMyCubeGrid>();
    private DateTime lastQuestLogUpdate = DateTime.MinValue;

    public override void UpdateAfterSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            _shieldApi = new ShieldApi();
            _isApiInitialized = _shieldApi.Load(); // Initialize the API
            Initialize();
            MyVisualScriptLogicProvider.SetQuestlog(true, "Durability and Shields Status");
            _isInitialized = true;
        }

        if (_isInitialized)
        {
            IMyCubeGrid grid;
            for (int i = 0; i < 10 && gridQueue.Count > 0; i++)
            {
                grid = gridQueue.Dequeue();
                ApplyDurabilityModifier(grid, DurabilityModifier);
            }

            if (MyAPIGateway.Session.GameplayFrameCounter % 600 == 0)
            {
                HealShields();
            }

            if (DateTime.Now - lastQuestLogUpdate > TimeSpan.FromSeconds(10))
            {
                MyVisualScriptLogicProvider.RemoveQuestlogDetails();
                lastQuestLogUpdate = DateTime.Now;
            }
        }
    }

    private void Initialize()
    {
        HashSet<IMyEntity> allEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allEntities, e => e is IMyCubeGrid);

        foreach (IMyEntity entity in allEntities)
        {
            if (entity is IMyCubeGrid)
            {
                gridQueue.Enqueue(entity as IMyCubeGrid);
            }
        }

        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
    }

    private void OnEntityAdd(IMyEntity entity)
    {
        if (entity is IMyCubeGrid)
        {
            gridQueue.Enqueue(entity as IMyCubeGrid);
        }
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
        MyVisualScriptLogicProvider.AddQuestlogObjective(message, false, true);
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
                    MyVisualScriptLogicProvider.AddQuestlogObjective(message, false, true);
                }
            }
        }
    }

    protected override void UnloadData()
    {
        base.UnloadData();
        MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
    }
}
