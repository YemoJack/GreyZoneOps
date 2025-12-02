using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType 
{ 
    Firearm, 
    Melee, 
}



public class SOWeaponConfigBase : ScriptableObject
{
    public int WeaponID;
    public string WeaponName;
    public WeaponType WeaponType;
    public string Discription;

    public GameObject WeaponPrefab;

    /// <summary>
    /// 命中特效
    /// </summary>
    public GameObject impactEffect;
    /// <summary>
    /// 攻击特效
    /// </summary>
    public GameObject attackEffect;

}
