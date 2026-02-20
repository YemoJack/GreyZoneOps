using UnityEngine;
using UnityEngine.UI;
using ZMUIFrameWork;

public class HomeWindow : WindowBase
{
    public HomeWindowDataComponent dataCompt;

    #region Lifecycle
    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<HomeWindowDataComponent>();
        dataCompt.InitComponent(this);
        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
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
    public void OnStartGameButtonClick()
    {
        GameLaunch.RequestStartGame();
    }

    public void OnWarehouseButtonClick()
    {
    }
    #endregion
}

