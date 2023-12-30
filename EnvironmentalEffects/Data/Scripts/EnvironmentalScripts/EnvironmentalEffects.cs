using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using DefenseShields;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class DurabilityModifierSession : MySessionComponentBase
{
    private const float DurabilityModifier = 0.1f;
    private bool _isInitialized = false;
    private bool _isApiInitialized = false;
    private ShieldApi _shieldApi;
    private List<IMyTerminalBlock> _shieldBlocks = new List<IMyTerminalBlock>();
    private Queue<IMyCubeGrid> gridQueue = new Queue<IMyCubeGrid>();



    public override void UpdateAfterSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            _shieldApi = new ShieldApi();
            _isApiInitialized = _shieldApi.Load(); // Initialize the API
            Initialize();
            _isInitialized = true;
        }

        for (int i = 0; i < 10 && gridQueue.Count > 0; i++)
        {
            IMyCubeGrid grid = gridQueue.Dequeue();
            ApplyDurabilityModifier(grid, DurabilityModifier);
        }

        if (MyAPIGateway.Session.GameplayFrameCounter % 600 == 0)
        {
            HealShields();
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
                IMyCubeGrid grid = entity as IMyCubeGrid;
                gridQueue.Enqueue(grid);
            }
        }

        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
    }

    private void OnEntityAdd(IMyEntity entity)
    {
        if (entity is IMyCubeGrid)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            gridQueue.Enqueue(grid);
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

        if (MyAPIGateway.Session?.Player != null)
        {
            string message = $"Durability modifier of {modifier}x applied to {grid.DisplayName}";
            MyAPIGateway.Utilities.ShowNotification(message, 5000, "Blue");
        }
    }

    private void HealShields()
    {
        var allEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allEntities, e => e is IMyCubeGrid);

        foreach (IMyEntity entity in allEntities)
        {
            var grid = entity as IMyCubeGrid;
            if (grid == null) continue; // Skip if for some reason the cast failed

            var shieldBlock = _shieldApi.GetShieldBlock(grid);
            if (shieldBlock != null)
            {
                // Assuming 1 million shield HP to heal
                float healAmount = 1000000;
                _shieldApi.SetCharge(shieldBlock, _shieldApi.GetCharge(shieldBlock) + healAmount);

                // Optional: Notify the player that shields have been healed
                if (MyAPIGateway.Session?.Player != null)
                {
                    string message = $"Healed 1 million HP on shields of {grid.DisplayName}";
                    MyAPIGateway.Utilities.ShowNotification(message, 2000, "Blue");
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
