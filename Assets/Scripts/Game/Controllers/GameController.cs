using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class GameController : MonoBehaviour,IController
{
    private IGameLoop updateScheduler;


    // Start is called before the first frame update
    void Awake()
    {
        updateScheduler = this.GetUtility<IGameLoop>();
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
