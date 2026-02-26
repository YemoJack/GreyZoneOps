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
    /// 装备时移动速度倍率
    /// </summary>
    public float moveSpeedMultiplier = 1f;
    /// <summary>
    /// 装备时奔跑速度倍率
    /// </summary>
    public float runSpeedMultiplier = 1f;
    /// <summary>
    /// 命中特效
    /// </summary>
    public GameObject impactEffect;


}
