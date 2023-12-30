using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class DurabilityModifierSession : MySessionComponentBase
{
    private const float DurabilityModifier = 0.1f;
    private bool _isInitialized = false;
    private Queue<IMyCubeGrid> gridQueue = new Queue<IMyCubeGrid>();

    public override void UpdateAfterSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            Initialize();
            _isInitialized = true;
            return;
        }

        for (int i = 0; i < 10 && gridQueue.Count > 0; i++)
        {
            IMyCubeGrid grid = gridQueue.Dequeue();
            ApplyDurabilityModifier(grid, DurabilityModifier);
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

    protected override void UnloadData()
    {
        base.UnloadData();
        MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
    }
}
