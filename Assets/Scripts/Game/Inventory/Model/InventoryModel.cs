using System.Collections;
using System.Collections.Generic;
using QFramework;

public enum InventoryContainerType
{
    Backpack,
    ChestRig,
    Stash,
    LootBox
}

public class InventoryModel : AbstractModel
{
    public Dictionary<InventoryContainerType, InventoryGrid> Grids;

    public ItemInstance HoldingItem;

    protected override void OnInit()
    {
        Grids = new Dictionary<InventoryContainerType, InventoryGrid>
        {
            { InventoryContainerType.Backpack, new InventoryGrid(6, 5) },
            { InventoryContainerType.LootBox, new InventoryGrid(5, 5) }
        };
    }
}

