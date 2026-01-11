using UnityEngine;

public enum ItemCategory
{
    Weapon,
    helmet,
    Armor,
    Ammo,
    Medical,
    ChestRig,
    Backpack,
    Collection
}


[CreateAssetMenu(fileName = "SOItemDefinition", menuName = "InventoryConfig/SOItemDefinition")]
public class SOItemDefinition : ScriptableObject
{
    public int Id;
    public string Name;
    public Vector2Int Size;
    public Sprite icon;
    public int MaxStack = 1;
    public bool CanRotate;
    public ItemCategory Category;
}









