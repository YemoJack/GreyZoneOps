using UnityEngine;
using UnityEngine.UI;
using ZMUIFrameWork;
using QFramework;
using System;

public class HomeWindow : WindowBase
{
    public HomeWindowDataComponent dataCompt;
    private InputSys inputSys;

    #region Lifecycle
    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<HomeWindowDataComponent>();
        dataCompt.InitComponent(this);
        inputSys = this.GetSystem<InputSys>();
        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
        SetCursorVisible(true);
        inputSys?.SetInputEnabled(false);
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    #endregion

    #region UI Events


    public void OnSettingButtonClick()
    {
        UIModule.Instance.PopUpWindow<SettingWindow>();
    }

    public void OnStartGameButtonClick()
    {
        GameLaunch.RequestStartGame();
    }

    public void OnWarehouseButtonClick()
    {
        UIModule.Instance.PopUpWindow<WarehouseWindow>();
    }
    #endregion

    private void SetCursorVisible(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }


}
