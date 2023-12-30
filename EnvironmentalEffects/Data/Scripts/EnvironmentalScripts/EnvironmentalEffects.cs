using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
public class DurabilityModifierSession : MySessionComponentBase
{
    private const float DurabilityModifier = 0.1f;
    private bool _isInitialized = false;

    public override void UpdateBeforeSimulation()
    {
        if (!_isInitialized && MyAPIGateway.Session != null)
        {
            Initialize();
            _isInitialized = true;
        }
    }

    private void Initialize()
    {
        HashSet<IMyEntity> allEntities = new HashSet<IMyEntity>();
        MyAPIGateway.Entities.GetEntities(allEntities, e => e is IMyCubeGrid);

        foreach (var entity in allEntities)
        {
            // Check and cast
            if (entity is IMyCubeGrid)
            {
                IMyCubeGrid grid = (IMyCubeGrid)entity;
                ApplyDurabilityModifier(grid, DurabilityModifier);
            }
        }

        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
    }

    private void OnEntityAdd(IMyEntity entity)
    {
        // Check and cast
        if (entity is IMyCubeGrid)
        {
            IMyCubeGrid grid = entity as IMyCubeGrid;
            ApplyDurabilityModifier(grid, DurabilityModifier);
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

        if (MyAPIGateway.Session?.Player != null)
        {
            var message = $"Durability modifier of {modifier}x applied to {grid.DisplayName}";
            MyAPIGateway.Utilities.ShowNotification(message, 5000, "Blue");
        }
    }

    protected override void UnloadData()
    {
        base.UnloadData();
        MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
    }
}
