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
    private int reactorUpdateCounter = 0;
    private int reactorUpdateInterval = 600; // 600 frames = 10 seconds
    private float reactorOutputMultiplier = 2f; // Set the reactor output multiplier here

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

            // Process reactor updates
            ProcessReactorUpdates();

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
        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
    }

    private void OnEntityAdd(IMyEntity entity)
    {
        if (entity is IMyCubeGrid)
        {
            gridQueue.Enqueue(entity as IMyCubeGrid);
            QueueReactorUpdates(entity as IMyCubeGrid);
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

    private void QueueReactorUpdates(IMyCubeGrid grid)
    {
        var blocks = new List<IMySlimBlock>();
        grid.GetBlocks(blocks, block => block.FatBlock is IMyReactor);

        foreach (var block in blocks)
        {
            var reactor = block.FatBlock as IMyReactor;
            if (reactor != null)
            {
                UpdateReactorOutput(reactor, reactorOutputMultiplier);
            }
        }
    }

    private void ProcessReactorUpdates()
    {
        reactorUpdateCounter++;

        if (reactorUpdateCounter >= reactorUpdateInterval)
        {
            reactorUpdateCounter = 0;
        }
    }

    private void UpdateReactorOutput(IMyReactor reactor, float multiplier)
    {
        if (reactor != null)
        {
            // Update the reactor output
            var oldValue = reactor.PowerOutputMultiplier;
            reactor.PowerOutputMultiplier = multiplier;

            var message = $"Reactor output multiplier changed from {oldValue}x to {multiplier}x on {reactor.CubeGrid.DisplayName}";
            MyVisualScriptLogicProvider.AddQuestlogObjective(message, false, false);
        }
    }
}
