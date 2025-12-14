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
        updateScheduler = this.GetUtility<IGameLoop>();
    }


    private void Start()
    {
        OnInitRes().Forget();

    }
    async UniTask OnInitRes()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        // var updatedRemote = await this.GetUtility<IResLoader>().UpdateRes((progress, desc) =>
        // {

        // });
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
