


/// <summary>
/// 武器接口
/// </summary>

public interface IWeapon
{
    SOWeaponConfigBase Config { get; }


    void Init(SOWeaponConfigBase sOWeapon);
    void OnEquip();       // 被装备
    void OnUnEquip();     // 被切换下去
    void TryAttack();       // 尝试攻击（近战/射击通用）
}