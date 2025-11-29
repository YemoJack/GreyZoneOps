using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class GameController : MonoBehaviour,IController
{
   
    private InputSys inputSys;


    // Start is called before the first frame update
    void Awake()
    {
        inputSys = this.GetSystem<InputSys>();
    }

    // Update is called once per frame
    void Update()
    {
        inputSys.UpdateInput();
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
