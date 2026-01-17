using System.Collections.Generic;
using UnityEngine;
using QFramework;


public struct EventGameInit
{

}


public class PlayerController : MonoBehaviour, IController
{
    public Transform WeaponRoot;
    public List<GameObject> weaponObjectList;

    private WeaponSystem weaponSystem;

    private void Start()
    {
        this.RegisterEvent<EventGameInit>(OnInit).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void OnInit(EventGameInit e)
    {

        weaponSystem = this.GetSystem<WeaponSystem>();
        weaponSystem.InitializeLoadout(WeaponRoot, weaponObjectList);
    }








    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
