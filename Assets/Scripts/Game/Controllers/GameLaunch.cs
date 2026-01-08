using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using YooAsset;
using Cysharp.Threading.Tasks;


public class GameLaunch : MonoBehaviour, IController, ICanSendEvent
{
    private IGameLoop updateScheduler;

    public EPlayMode LaunchMode;


    private void Awake()
    {
        UIModule.Instance.Initialize();
        OnStart().Forget();

    }

    async UniTask OnStart()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        (GameArchitecture.Interface as GameArchitecture).Registor();

        updateScheduler = this.GetUtility<IGameLoop>();

        this.GetSystem<PlayerSystem>().InitPlayerSystem();


        UIModule.Instance.Initialize();
        UIModule.Instance.PopUpWindow<GameWindow>();

        this.SendEvent<EventPlayerInit>(new EventPlayerInit());

    }




    // Update is called once per frame
    void Update()
    {
        if (updateScheduler != null)
            updateScheduler.Tick(Time.deltaTime);
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
