using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataView : MonoBehaviour, IController
{
    [Header("Player Progress Texts")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text cashText;
    [SerializeField] private Text totalAssetText;
    [SerializeField] private Text totalRaidCountText;
    [SerializeField] private Text successfulExtractionCountText;
    [SerializeField] private Text totalExtractionIncomeText;
    [SerializeField] private Text lastExtractionIncomeText;
    [SerializeField] private Text lastExtractionTimeText;

    private IUnRegister progressChangedUnregister;

    private void OnEnable()
    {
        progressChangedUnregister = this.RegisterEvent<EventPlayerProgressChanged>(OnPlayerProgressChanged);
        RefreshFromSystem();
    }

    private void OnDisable()
    {
        progressChangedUnregister?.UnRegister();
        progressChangedUnregister = null;
    }

    public void SetData(PlayerProgressSaveData data)
    {
        if (data == null)
        {
            data = new PlayerProgressSaveData();
        }

        data.Normalize();

        SetText(levelText, "\u7b49\u7ea7: " + data.Level);
        SetText(experienceText, "\u7ecf\u9a8c: " + data.Experience);
        SetText(cashText, "\u73b0\u91d1: " + data.Cash);
        SetText(totalAssetText, "\u603b\u8d44\u4ea7: " + data.TotalAsset);
        SetText(totalRaidCountText, "\u603b\u5bf9\u5c40: " + data.TotalRaidCount);
        SetText(successfulExtractionCountText, "\u6210\u529f\u64a4\u79bb\u6b21\u6570: " + data.SuccessfulExtractionCount);
        SetText(totalExtractionIncomeText, "\u603b\u51c0\u6536\u76ca: " + data.TotalExtractionIncome);
        SetText(lastExtractionIncomeText, "\u4e0a\u5c40\u51c0\u6536\u76ca: " + data.LastExtractionIncome);
        SetText(lastExtractionTimeText, "\u4e0a\u5c40\u65f6\u95f4: " + FormatUtcTicks(data.LastExtractionUtcTicks));
    }

    private void RefreshFromSystem()
    {
        var data = this.GetSystem<PlayerProgressSystem>()?.GetCurrentProgress();
        SetData(data);
    }

    private void OnPlayerProgressChanged(EventPlayerProgressChanged e)
    {
        SetData(e.Data);
    }

    private static void SetText(Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    private static string FormatUtcTicks(long utcTicks)
    {
        if (utcTicks <= 0)
        {
            return "-";
        }

        try
        {
            var utcTime = new DateTime(utcTicks, DateTimeKind.Utc);
            return utcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return "-";
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}


