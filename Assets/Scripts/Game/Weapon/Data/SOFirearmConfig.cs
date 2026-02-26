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

[System.Serializable]
public struct DamageFalloffEntry
{
    /// <summary>超过该距离时使用的伤害倍率</summary>
    public float distance;
    public float multiplier;
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
    public FireMode availableFireModes = FireMode.Single | FireMode.Burst | FireMode.Auto; // 默认支持 单发 + 三连发 + 全自动

    /// <summary>
    /// 当前的射击模式
    /// </summary>
    public FireMode currentFireMode = FireMode.Single;

    /// <summary>
    /// 弹夹容量
    /// </summary>
    public int magSize;

    /// <summary>
    /// 倍率配置
    /// </summary>
    public float zoomFactor = 1.25f;

    [Header("基础伤害")]
    /// <summary>
    /// 基础伤害
    /// </summary>
    public float baseDamage = 30f;
    /// <summary>
    /// 护甲伤害
    /// </summary>
    public float armorDamage;


    /// <summary>
    /// 优势射程
    /// </summary>
    public float range;

    /// <summary>
    ///  最大射程
    /// </summary>
    public float maxRange;

    [Header("伤害衰减")]
    /// <summary>距离-伤害倍率表（按距离升序配置）。例如 50 -> 0.8, 100 -> 0.4</summary>
    public List<DamageFalloffEntry> damageFalloff = new List<DamageFalloffEntry>
    {
        new DamageFalloffEntry { distance = 50f, multiplier = 0.8f },
        new DamageFalloffEntry { distance = 100f, multiplier = 0.4f }
    };


    [Header("后坐力控制")]
    /// <summary>
    /// 当前后坐力控制 （0-100）
    /// </summary>
    public float recoilControl = 40f;


    /// <summary>后坐力模式：每发的垂直 / 水平 偏移</summary>
    public Vector2[] recoilPattern;

    ///// <summary>松开射击后回正速度</summary>
    //public float recoilRecoverySpeed = 8f;

    /// <summary>
    /// 后坐力倍率范围
    /// </summary>
    public Vector2 recoilMulRange = new Vector2(0.4f, 1.4f);

    /// <summary>
    /// 停止开火后回正持续时间（秒）
    /// </summary>
    public float recoilDuration = 0.2f;

    /// <summary>
    /// 上抬速度（度/秒），0 表示使用全局默认值
    /// </summary>
    public float recoilRaiseSpeed = 0f;

    /// <summary>
    /// 回落速度（度/秒），0 表示使用全局默认值
    /// </summary>
    public float recoilReturnSpeed = 0f;


    [Header("操作速度")]

    /// <summary>
    /// 换弹时间
    /// </summary>
    public float reloadTime;

    /// <summary>
    /// 开镜时间
    /// </summary>
    public float aimTime = 0.5f;


    /// <summary>
    /// 装备时瞄准移动速度倍率
    /// </summary>
    public float aimMoveSpeedMultiplier = 0.6f;


    [Header("子弹散布")]
    [Tooltip("静止状态下的基础散布角度（度）")]
    public float idleSpread = 0.25f;

    [Tooltip("行走时的基础散布角度（度）")]
    public float walkSpread = 0.6f;

    [Tooltip("奔跑时的基础散布角度（度）")]
    public float runSpread = 1.2f;

    [Tooltip("跳跃或空中时的基础散布角度（度）")]
    public float jumpSpread = 1.5f;

    [Tooltip("瞄准状态下的基础散布角度（度）")]
    public float aimSpread = 0.05f;

    [Tooltip("瞄准状态下连续开火累计散布的最大角度（度）")]
    public float maxAimSpreadWhileFiring = 0.5f;

    [Tooltip("每次开火递增的散布角度（度）")]
    public float spreadIncreasePerShot = 0.05f;

    [Tooltip("连续开火累计散布的最大角度（度）")]
    public float maxSpreadWhileFiring = 2f;

    [Tooltip("停止射击后散布恢复到基础值的速度（度/秒）")]
    public float spreadRecoveryRate = 4f;



    [Header("枪械位置")]
    public Vector3 gunEquiptPos;
    public Vector3 gunEquiptRot;
    public Vector3 gunAimPos;
    public Vector3 gunAimRot;




}
