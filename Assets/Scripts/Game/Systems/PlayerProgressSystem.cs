using System;
using QFramework;
using UnityEngine;

public struct EventPlayerProgressChanged
{
    public PlayerProgressSaveData Data;
}

public class PlayerProgressSystem : AbstractSystem
{
    private PlayerProgressModel progressModel;
    private InventorySystem raidInventorySystem;
    private InventoryContainerModel inventoryContainerModel;
    private PersistentInventoryModel persistentInventoryModel;
    private GameFlowSystem gameFlowSystem;

    protected override void OnInit()
    {
        progressModel = this.GetModel<PlayerProgressModel>();
        this.RegisterEvent<EventExtractionSucceeded>(OnExtractionSucceeded);
        this.RegisterEvent<EventMapStateChanged>(OnMapStateChanged);
    }

    public PlayerProgressSaveData BuildSaveData()
    {
        EnsureModel();
        PlayerProgressSaveData data = progressModel.GetMutableData();
        RefreshDerivedValues(data);
        data.Normalize();
        return data.Clone();
    }

    public PlayerProgressSaveData GetCurrentProgress()
    {
        EnsureModel();
        PlayerProgressSaveData data = progressModel.GetMutableData();
        RefreshDerivedValues(data);
        data.Normalize();
        return data.Clone();
    }

    public void ApplySaveData(PlayerProgressSaveData data)
    {
        EnsureModel();
        progressModel.Apply(data);
        RefreshDerivedValues(progressModel.GetMutableData());
        NotifyChanged();
    }

    public void SetBaseStats(int level, int experience, int cash)
    {
        EnsureModel();
        var data = progressModel.GetMutableData();
        data.Level = Mathf.Max(1, level);
        data.Experience = 0;
        data.Cash = Mathf.Max(0, cash);
        data.Normalize();
        GainExperience(data, Mathf.Max(0, experience));
        RefreshDerivedValues(data);
        NotifyChanged();
    }

    public void AddCash(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        EnsureModel();
        PlayerProgressSaveData data = progressModel.GetMutableData();
        data.Cash += amount;
        RefreshDerivedValues(data);
        data.Normalize();
        NotifyChanged();
    }

    public void RefreshProgress()
    {
        EnsureModel();
        PlayerProgressSaveData data = progressModel.GetMutableData();
        RefreshDerivedValues(data);
        data.Normalize();
        NotifyChanged();
    }

    private void OnExtractionSucceeded(EventExtractionSucceeded _)
    {
        EnsureModel();
        EnsureRaidInventorySystem();

        PlayerProgressSaveData data = progressModel.GetMutableData();
        int income = raidInventorySystem != null ? raidInventorySystem.GetCurrentRaidIncome() : 0;
        int awardedExperience = CalculateExperienceReward(income);

        data.SuccessfulExtractionCount += 1;
        data.TotalExtractionIncome += income;
        GainExperience(data, awardedExperience);

        data.LastExtractionIncome = income;
        data.LastExtractionUtcTicks = DateTime.UtcNow.Ticks;
        RefreshDerivedValues(data);
        data.Normalize();
        NotifyChanged();
    }

    private void OnMapStateChanged(EventMapStateChanged e)
    {
        if (e.Previous != MapState.InRaid || e.Current != MapState.Ended)
        {
            return;
        }

        EnsureModel();
        PlayerProgressSaveData data = progressModel.GetMutableData();
        data.TotalRaidCount += 1;
        RefreshDerivedValues(data);
        data.Normalize();
        NotifyChanged();
    }

    private static int CalculateExperienceReward(int income)
    {
        int incomeReward = Mathf.Max(0, income / 10);
        return Mathf.Max(20, incomeReward);
    }

    private static void GainExperience(PlayerProgressSaveData data, int amount)
    {
        if (data == null || amount <= 0)
        {
            return;
        }

        data.Level = Mathf.Max(1, data.Level);
        data.Experience = Mathf.Max(0, data.Experience);
        data.Experience += amount;

        var guard = 0;
        while (guard < 1024)
        {
            guard++;
            var required = GetExperienceToNextLevel(data.Level);
            if (data.Experience < required)
            {
                break;
            }

            data.Experience -= required;
            data.Level++;
        }
    }

    private static int GetExperienceToNextLevel(int level)
    {
        var safeLevel = Mathf.Max(1, level);
        return 100 + (safeLevel - 1) * 25;
    }

    private void NotifyChanged()
    {
        EnsureModel();
        PlayerProgressSaveData mutable = progressModel.GetMutableData();
        RefreshDerivedValues(mutable);
        mutable.Normalize();
        PlayerProgressSaveData snapshot = mutable.Clone();
        this.SendEvent(new EventPlayerProgressChanged
        {
            Data = snapshot
        });
    }

    private void EnsureModel()
    {
        if (progressModel == null)
        {
            progressModel = this.GetModel<PlayerProgressModel>();
        }
    }

    private void EnsureRaidInventorySystem()
    {
        if (raidInventorySystem == null)
        {
            raidInventorySystem = this.GetSystem<InventorySystem>();
        }
    }

    private void EnsureInventoryContainerModel()
    {
        if (inventoryContainerModel == null)
        {
            inventoryContainerModel = this.GetModel<InventoryContainerModel>();
        }
    }

    private void EnsurePersistentInventoryModel()
    {
        if (persistentInventoryModel == null)
        {
            persistentInventoryModel = this.GetModel<PersistentInventoryModel>();
        }
    }

    private void EnsureGameFlowSystem()
    {
        if (gameFlowSystem == null)
        {
            gameFlowSystem = this.GetSystem<GameFlowSystem>();
        }
    }

    private void RefreshDerivedValues(PlayerProgressSaveData data)
    {
        if (data == null)
        {
            return;
        }

        data.TotalAsset = CalculateTotalAssetValue(data.Cash);
    }

    private int CalculateTotalAssetValue(int cash)
    {
        int totalAsset = Mathf.Max(0, cash);

        EnsureRaidInventorySystem();
        EnsurePersistentInventoryModel();
        if (raidInventorySystem != null && persistentInventoryModel != null)
        {
            totalAsset += raidInventorySystem.CalculateItemsTotalValue(persistentInventoryModel.GetMutableItems());
        }

        if (ShouldIncludeOutOfRaidLoadoutInAsset() && raidInventorySystem != null)
        {
            EnsureInventoryContainerModel();
            totalAsset += raidInventorySystem.CalculateEquipmentTotalValue(inventoryContainerModel?.PlayerEquipment, includeStash: false);
        }

        return Mathf.Max(0, totalAsset);
    }

    private bool ShouldIncludeOutOfRaidLoadoutInAsset()
    {
        EnsureGameFlowSystem();
        GameFlowState state = gameFlowSystem != null ? gameFlowSystem.CurrentState : GameFlowState.None;
        return state != GameFlowState.InRaid &&
               state != GameFlowState.LoadingToGame &&
               state != GameFlowState.RaidEnded;
    }
}
