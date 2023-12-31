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
    private bool _isApiInitialized = false;
    private ShieldApi _shieldApi;
    private Queue<IMyCubeGrid> gridQueue = new Queue<IMyCubeGrid>();
    private DateTime lastQuestLogUpdate = DateTime.MinValue;
    private List<string> questDetails = new List<string>();

    public override void UpdateAfterSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            _shieldApi = new ShieldApi();
            _isApiInitialized = _shieldApi.Load(); // Initialize the API
            Initialize();
            MyVisualScriptLogicProvider.SetQuestlog(true, "System Effects (Pulsar)");
            _isInitialized = true;
        }

        if (_isInitialized)
        {
            ProcessGrids();
            ProcessHealShields();
            UpdateQuestLog();
        }
    }

    private void Initialize()
    {
        EnqueueAllGrids();
        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
    }

    private void EnqueueAllGrids()
    {
        var allGrids = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allGrids, e => e is IMyCubeGrid);
        foreach (IMyCubeGrid grid in allGrids)
        {
            gridQueue.Enqueue(grid);
        }
    }

    private void ProcessGrids()
    {
        for (int i = 0; i < 10 && gridQueue.Count > 0; i++)
        {
            var grid = gridQueue.Dequeue();
            ApplyDurabilityModifier(grid, DurabilityModifier);
        }
    }

    private void ProcessHealShields()
    {
        if (MyAPIGateway.Session.GameplayFrameCounter % 600 == 0)
        {
            HealShields();
        }
    }

    private void UpdateQuestLog()
    {
        if (DateTime.Now - lastQuestLogUpdate > TimeSpan.FromSeconds(10))
        {
            MyVisualScriptLogicProvider.RemoveQuestlogDetails();
            foreach (var detail in questDetails)
            {
                MyVisualScriptLogicProvider.AddQuestlogDetail(detail, false, false);
            }
            lastQuestLogUpdate = DateTime.Now;
        }
    }

    private void ApplyDurabilityModifier(IMyCubeGrid grid, float modifier)
    {
        var blocks = new List<IMySlimBlock>();
        grid.GetBlocks(blocks);

        foreach (var block in blocks)
        {
            block.BlockGeneralDamageModifier = modifier;
        }

        var message = $"Durability modifier of {modifier}x applied to {grid.DisplayName}";
        questDetails.Add(message);
    }

    private void HealShields()
    {
        var allEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allEntities, e => e is IMyCubeGrid);

        foreach (var entity in allEntities)
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
                    questDetails.Add(message);
                }
            }
        }
    }

    private void OnEntityAdd(IMyEntity entity)
    {
        IMyCubeGrid grid = entity as IMyCubeGrid;
        if (grid != null)
        {
            gridQueue.Enqueue(grid);
        }
    }

    protected override void UnloadData()
    {
        MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
        // Perform any additional cleanup...
    }
}
