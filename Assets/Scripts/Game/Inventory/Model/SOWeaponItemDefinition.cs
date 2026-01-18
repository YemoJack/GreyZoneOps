using UnityEngine;

[CreateAssetMenu(fileName = "SOWeaponItem", menuName = "InventoryConfig/SOWeaponItemDefinition")]
public class SOWeaponItemDefinition : SOItemDefinition
{
    public SOWeaponConfigBase WeaponConfig;
    public GameObject WeaponPrefab;

    private void OnValidate()
    {
        Category = ItemCategory.Weapon;
    }
}
