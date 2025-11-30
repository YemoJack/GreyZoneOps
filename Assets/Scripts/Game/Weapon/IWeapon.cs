


/// <summary>
/// 武器接口
/// </summary>

public interface IWeapon
{
    SOWeaponConfigBase Config { get; }


    void Init(SOWeaponConfigBase sOWeapon);
    void OnEquip();       // 被装备
    void OnUnEquip();     // 被切换下去
    void Tick();          // 每帧（用于自动开火，蓄力等）
    void TryFire();       // 尝试攻击（近战/射击通用）
}