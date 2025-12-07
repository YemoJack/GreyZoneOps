using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using UnityEngine.UI;

public class GamePanel : MonoBehaviour,IController
{

    public Text WeaponNameText;
    public Text AmmoNumText;

    // Start is called before the first frame update
    void Start()
    {
        
    }



    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }

}
