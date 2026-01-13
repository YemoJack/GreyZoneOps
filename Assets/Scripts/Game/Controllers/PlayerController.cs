using System.Collections.Generic;
using UnityEngine;
using QFramework;


public struct EventPlayerInit
{

}


public class PlayerController : MonoBehaviour, IController
{
    public Transform WeaponRoot;
    public List<GameObject> weaponObjectList;

    private WeaponSystem weaponSystem;

    private void Start()
    {
        this.RegisterEvent<EventPlayerInit>(OnInit).UnRegisterWhenGameObjectDestroyed(this);
    }

    private void OnInit(EventPlayerInit e)
    {

        weaponSystem = this.GetSystem<WeaponSystem>();
        weaponSystem.InitializeLoadout(WeaponRoot, weaponObjectList);
    }








    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
