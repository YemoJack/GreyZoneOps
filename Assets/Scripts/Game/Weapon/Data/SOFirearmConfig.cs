using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Flags]
public enum FireMode
{
    None = 0,
    Single = 1 << 0, // 0001
    Burst = 1 << 1, // 0010
    Auto = 1 << 2, // 0100
}


[CreateAssetMenu(fileName = "SOFirearmConfig", menuName = "WeaponConfig/SOFirearmConfig")]
public class SOFirearmConfig : SOWeaponConfigBase
{


    [Header("基础信息")]


    /// <summary>
    /// 射速 （发/秒）
    /// </summary>
    public float fireRate;

    /// <summary>
    /// 枪口初速
    /// </summary>
    public float bulletSpeed;

    /// <summary>
    /// 枪声传播范围
    /// </summary>
    public float GunshotRange;

    /// <summary>
    /// 射击模式
    /// </summary>
    public FireMode availableFireModes = FireMode.Single | FireMode.Auto; // 默认支持 单发 + 全自动

    /// <summary>
    /// 当前的射击模式
    /// </summary>
    public FireMode currentFireMode = FireMode.Single;

    /// <summary>
    /// 弹夹容量
    /// </summary>
    public int magSize;

    [Header("基础伤害")]
    /// <summary>
    /// 基础伤害
    /// </summary>
    public float baseDamage = 30f;
    /// <summary>
    /// 护甲伤害
    /// </summary>
    public float armorDamage;

    public AnimationCurve damageFalloffCurve; // x=距离，y=伤害倍率

    /// <summary>
    /// 优势射程
    /// </summary>
    public float range;

    /// <summary>
    ///  最大射程
    /// </summary>
    public float maxRange;


    [Header("后坐力控制")]
    /// <summary>
    /// 当前后坐力控制 （0-100）
    /// </summary>
    public float recoilControl = 40f;


    /// <summary>后坐力模式：每发的垂直 / 水平 偏移</summary>
    public Vector2[] recoilPattern;

    /// <summary>松开射击后回正速度</summary>
    public float recoilRecoverySpeed = 8f;

    /// <summary>
    /// 后坐力倍率范围
    /// </summary>
    public Vector2 recoilMulRange = new Vector2(0.4f, 1.4f);

    [HideInInspector]
    /// <summary>垂直后坐力倍率（附件影响）</summary>
    public float verticalRecoilMul = 1f;
    [HideInInspector]
    /// <summary>水平后坐力倍率（附件影响）</summary>
    public float horizontalRecoilMul = 1f;



    [Header("操作速度")]
    /// <summary>
    /// 当前操作速度
    /// </summary>
    public float operatingSpeed;
    /// <summary>
    /// 换弹时间
    /// </summary>
    public float reloadTime;

    /// <summary>
    /// 开镜时间
    /// </summary>
    public float aimTime = 0.5f;



    [Header("瞄准 & 腰射精度")]
    /// <summary>
    /// 当前腰射精度（0-100 100表示最小腰射角度，0表示最大腰射角度）
    /// </summary>
    public float hipfireAccuracy;


    /// <summary>
    /// 最小腰射角度(静止，移动，开火)
    /// </summary>
    public Vector3 MinHipfireAngle;
    /// <summary>
    /// 最大腰射角度(静止，移动，开火)
    /// </summary>
    public Vector3 MaxHipfireAngle;

    /// <summary>
    /// 射击时的腰射增量
    /// </summary>
    public float HipFireAddAngle;



    /// <summary>
    /// 倍率配置
    /// </summary>
    public float zoomFactor = 1.25f;



    [Header("子弹预制体")]
    public GameObject bulletPrefab;

}
