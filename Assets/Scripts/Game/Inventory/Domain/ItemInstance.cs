using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public readonly string InstanceId;
    public SOItemDefinition Definition;
    public int Count;
    public bool Rotated;
    public InventoryContainer AttachedContainer;

    public ItemInstance(SOItemDefinition def, int count = 1)
    {
        InstanceId = Guid.NewGuid().ToString("N");
        Definition = def;
        Count = Mathf.Max(1, count);
        Rotated = false;
    }

    /// <summary>当前堆叠是否已满</summary>
    public bool IsFull => Definition != null && Count >= Definition.MaxStack;

    /// <summary>剩余可叠加数量</summary>
    public int RemainingStackSpace => Definition == null ? 0 : Mathf.Max(0, Definition.MaxStack - Count);

    /// <summary>是否可以与目标堆叠</summary>
    public bool CanStackWith(ItemInstance other)
    {
        if (other == null || other.Definition != Definition) return false;
        return !IsFull;
    }

    /// <summary>将数量叠加到当前堆叠，返回剩余未叠加的数量</summary>
    public int AddToStack(int amount)
    {
        if (amount <= 0 || Definition == null || IsFull) return amount;

        var space = RemainingStackSpace;
        var toMove = Mathf.Min(space, amount);
        Count += toMove;
        return amount - toMove;
    }

    /// <summary>从堆叠中拆分出指定数量，若数量不足则返回null</summary>
    public ItemInstance SplitStack(int amount)
    {
        if (amount <= 0 || amount >= Count) return null;
        Count -= amount;
        return new ItemInstance(Definition, amount) { Rotated = Rotated };
    }
}
