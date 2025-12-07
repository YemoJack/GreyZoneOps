using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class GameController : MonoBehaviour,IController
{
   
    private InputSys inputSys;
    private BulletManager bulletManager;


    // Start is called before the first frame update
    void Awake()
    {
        inputSys = this.GetSystem<InputSys>();
        bulletManager = this.GetSystem<BulletManager>();
    }

    // Update is called once per frame
    void Update()
    {
        inputSys.UpdateInput();
        bulletManager.OnUpdate(Time.deltaTime);
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
