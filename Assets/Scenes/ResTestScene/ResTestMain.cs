using Cysharp.Threading.Tasks;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using YooAsset;

public class ResTestMain : MonoBehaviour,IController
{

    public EPlayMode LaunchMode;


    // Start is called before the first frame update


    private void Start()
    {
        OnStart().Forget();
    }
    async UniTask OnStart()
    {
        await this.GetUtility<IResLoader>().InitLoader(LaunchMode);

        var updatedRemote = await this.GetUtility<IResLoader>().UpdateRes((progress, desc) =>
        {
           
        });
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {

            this.GetUtility<IResLoader>().LoadAsync<GameObject>("HitEff", (prefab) =>
            {
                Instantiate(prefab);
            });

        }
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }


}
