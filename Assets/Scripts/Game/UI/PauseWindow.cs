using QFramework;
using UnityEngine;
using ZMUIFrameWork;

public class PauseWindow : WindowBase
{
    public static bool IsWindowVisible { get; private set; }

    public PauseWindowDataComponent dataCompt;

    private InputSys inputSys;
    private bool isHandlingGiveUp;

    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<PauseWindowDataComponent>();
        dataCompt.InitComponent(this);
        inputSys = this.GetSystem<InputSys>();
        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
        IsWindowVisible = true;
        SetCursorVisible(true);
        inputSys?.SetInputEnabled(false);
    }

    public override void OnHide()
    {
        IsWindowVisible = false;
        if (!isHandlingGiveUp)
        {
            inputSys?.SetInputEnabled(true);
            SetCursorVisible(false);
        }

        base.OnHide();
    }

    public override void OnDestroy()
    {
        IsWindowVisible = false;
        if (!isHandlingGiveUp)
        {
            inputSys?.SetInputEnabled(true);
            SetCursorVisible(false);
        }

        base.OnDestroy();
    }

    public void OnCloseButtonClick()
    {
        if (isHandlingGiveUp)
        {
            return;
        }

        HideWindow();
    }

    public void OnSettingButtonClick()
    {
        if (isHandlingGiveUp)
        {
            return;
        }

        UIModule.Instance.PopUpWindow<SettingWindow>();
    }

    public void OnGiveUpButtonClick()
    {
        if (isHandlingGiveUp)
        {
            return;
        }

        HandleGiveUpAsRaidFailure();
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    private void HandleGiveUpAsRaidFailure()
    {
        isHandlingGiveUp = true;
        var raidInventorySystem = this.GetSystem<InventorySystem>();
        raidInventorySystem?.ResetRaidRuntimeInventory();

        this.GetSystem<MapSystem>()?.EndRaid();

        UIModule.Instance.HideWindow<SettingWindow>();
        UIModule.Instance.HideWindow<PauseWindow>();

        var gameOverWindow = UIModule.Instance.PopUpWindow<GameOverWindow>();
        gameOverWindow?.SetResult(false, 0);
        UIModule.Instance.HideWindow<GameWindow>();
    }
}
