using Cysharp.Threading.Tasks;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using YooAsset;

public class ResTestMain : MonoBehaviour, IController
{

    public EPlayMode LaunchMode;


    // Start is called before the first frame update


    private void Start()
    {
        OnStart().Forget();
        UIModule.Instance.Initialize();
    }
    async UniTask OnStart()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        var updatedRemote = await this.GetUtility<IResLoader>().UpdateRes((progress, desc) =>
        {

        });

        (GameArchitecture.Interface as GameArchitecture).Registor();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            this.GetUtility<IResLoader>().LoadAsync<GameObject>("HitEff", (prefab) =>
            {
                Instantiate(prefab);
            });


            UIModule.Instance.PopUpWindow<GameWindow>();

        }
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }


}
