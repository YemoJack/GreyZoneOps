using QFramework;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponSlotState
{
    Locked,
    Available,
}

public class WeaponInventoryEntry
{
    public int WeaponId { get; }
    public SOWeaponConfigBase Config { get; }
    public WeaponSlotState State { get; private set; }

    public WeaponInventoryEntry(int weaponId, SOWeaponConfigBase config, WeaponSlotState state)
    {
        WeaponId = weaponId;
        Config = config;
        State = state;
    }

    public void SetState(WeaponSlotState state)
    {
        State = state;
    }
}

public class WeaponInventoryModel : AbstractModel
{
    private readonly List<WeaponInventoryEntry> slots = new List<WeaponInventoryEntry>();

    public IReadOnlyList<WeaponInventoryEntry> Slots => slots;
    public int CurrentIndex { get; private set; } = -1;
    public WeaponInventoryEntry CurrentSlot =>
        (CurrentIndex >= 0 && CurrentIndex < slots.Count) ? slots[CurrentIndex] : null;

    protected override void OnInit() { }

    /// <summary>
    /// 添加或激活一个武器槽位（基于配置）。
    /// </summary>
    public bool AddOrActivateSlot(SOWeaponConfigBase config, out WeaponInventoryEntry entry)
    {
        entry = null;

        if (config == null)
        {
            Debug.LogWarning("尝试添加的武器配置为空");
            return false;
        }

        var existingIndex = slots.FindIndex(s => s.WeaponId == config.WeaponID);
        if (existingIndex >= 0)
        {
            entry = slots[existingIndex];
            entry.SetState(WeaponSlotState.Available);
            return true;
        }

        entry = new WeaponInventoryEntry(config.WeaponID, config, WeaponSlotState.Available);
        slots.Add(entry);

        if (CurrentIndex == -1)
        {
            CurrentIndex = 0;
        }

        return true;
    }

    public bool TryGetSlotById(int weaponId, out WeaponInventoryEntry entry)
    {
        entry = slots.Find(s => s.WeaponId == weaponId);
        return entry != null;
    }

    /// <summary>
    /// 按索引切换武器，包含边界与可用性校验。
    /// </summary>
    public bool TrySwitchWeapon(int index, out WeaponInventoryEntry entry)
    {
        entry = null;

        if (slots.Count == 0)
        {
            Debug.LogWarning("没有可用的武器槽位");
            return false;
        }

        if (index < 0 || index >= slots.Count)
        {
            Debug.LogWarning($"切换武器失败：索引 {index} 超出范围");
            return false;
        }

        var target = slots[index];
        if (target.State != WeaponSlotState.Available)
        {
            Debug.LogWarning($"切换武器失败：槽位 {index} 状态为 {target.State}");
            return false;
        }

        CurrentIndex = index;
        entry = target;
        return true;
    }

    public bool TrySwitchNextAvailable(out WeaponInventoryEntry entry)
    {
        entry = null;

        if (slots.Count == 0)
        {
            Debug.LogWarning("没有可用的武器槽位");
            return false;
        }

        var startIndex = CurrentIndex >= 0 ? CurrentIndex : 0;
        var count = slots.Count;

        for (int i = 1; i <= count; i++)
        {
            var candidateIndex = (startIndex + i) % count;
            var candidate = slots[candidateIndex];
            if (candidate.State == WeaponSlotState.Available)
            {
                CurrentIndex = candidateIndex;
                entry = candidate;
                return true;
            }
        }

        Debug.LogWarning("未找到可用的武器进行切换");
        return false;
    }
}
