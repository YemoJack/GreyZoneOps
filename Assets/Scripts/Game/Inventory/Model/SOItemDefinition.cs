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

public enum ItemQuality
{
    White,
    Green,
    Blue,
    Purple,
    Orange,
    Red
}


[CreateAssetMenu(fileName = "SOItemDefinition", menuName = "InventoryConfig/SOItemDefinition")]
public class SOItemDefinition : ScriptableObject
{
    public int Id;
    public string Name;
    public string ResName;
    public Vector2Int Size;
    public Sprite icon;
    public int MaxStack = 1;
    public bool CanRotate;
    public ItemCategory Category;
    public ItemQuality Quality = ItemQuality.White;
    [Min(0)]
    public int Value;
}









