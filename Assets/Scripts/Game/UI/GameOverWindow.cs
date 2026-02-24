using UnityEngine;
using UnityEngine.SceneManagement;
using ZMUIFrameWork;
using QFramework;
using Cysharp.Threading.Tasks;

public class GameOverWindow : WindowBase
{
    public GameOverWindowDataComponent dataCompt;
    private InputSys inputSys;
    private bool isReturningHome;

    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<GameOverWindowDataComponent>();
        dataCompt.InitComponent(this);
        inputSys = this.GetSystem<InputSys>();
        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
        SetCursorVisible(true);
        if (inputSys != null)
        {
            inputSys.SetInputEnabled(false);
        }
    }

    public override void OnHide()
    {

        base.OnHide();
    }

    public override void OnDestroy()
    {

        base.OnDestroy();
    }

    public void SetResult(bool isExtractSuccess, int income)
    {
        if (dataCompt == null)
        {
            return;
        }

        if (dataCompt.TitleText != null)
        {
            dataCompt.TitleText.text = isExtractSuccess ? "\u64A4\u79BB\u6210\u529F" : "\u64A4\u79BB\u5931\u8D25";
        }

        if (dataCompt.IncomeText != null)
        {
            dataCompt.IncomeText.text = $"\u6536\u76CA: {income}";
        }
    }

    public void OnHomeButtonClick()
    {
        if (isReturningHome)
        {
            return;
        }

        ReturnHomeAsync().Forget();
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    private async UniTask ReturnHomeAsync()
    {
        isReturningHome = true;

        var raidInventorySystem = this.GetSystem<InventorySystem>();
        if (raidInventorySystem != null)
        {
            bool saveOk = raidInventorySystem.SaveGameData();
            Debug.Log($"GameOverWindow: save before return home => {saveOk}");
        }

        this.GetSystem<MapSystem>()?.EndRaid();
        this.GetSystem<GameFlowSystem>()?.EnterLoadingToMenu();

        UIModule.Instance.DestroyAllWindow();
        var loadingWindow = UIModule.Instance.PopUpWindow<LoadingWindow>();
        loadingWindow?.SetProgressWidth(0f);

        var startSceneName = GameLaunch.Instance != null ? GameLaunch.StartSceneName : "StartScene";
        var loadOp = SceneManager.LoadSceneAsync(startSceneName, LoadSceneMode.Single);
        if (loadOp == null)
        {
            if (loadingWindow != null)
            {
                UIModule.Instance.DestroyWinodw<LoadingWindow>();
            }

            isReturningHome = false;
            return;
        }

        while (!loadOp.isDone)
        {
            var normalizedProgress = loadOp.progress < 0.9f
                ? Mathf.Clamp01(loadOp.progress / 0.9f)
                : 1f;
            loadingWindow?.SetProgress01(normalizedProgress);
            await UniTask.Yield();
        }

        loadingWindow?.SetProgressWidth(1000f);
        await UniTask.Delay(1000, DelayType.UnscaledDeltaTime);
        if (loadingWindow != null)
        {
            UIModule.Instance.DestroyWinodw<LoadingWindow>();
        }

        isReturningHome = false;
    }
}
