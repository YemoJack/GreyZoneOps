using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using YooAsset;
using Cysharp.Threading.Tasks;


public class GameController : MonoBehaviour, IController
{
    private IGameLoop updateScheduler;

    public EPlayMode LaunchMode;




    // Start is called before the first frame update
    void Awake()
    {
        OnInitRes().Forget();

    }


    async UniTask OnInitRes()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);



        (GameArchitecture.Interface as GameArchitecture).Registor();

        updateScheduler = this.GetUtility<IGameLoop>();
        UIModule.Instance.Initialize();
        UIModule.Instance.PopUpWindow<GameWindow>();

    }



    // Update is called once per frame
    void Update()
    {
        updateScheduler.Tick(Time.deltaTime);
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
