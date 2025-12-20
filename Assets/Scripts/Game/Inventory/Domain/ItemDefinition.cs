using UnityEngine;

public enum ItemCategory
{
    Weapon,
    Ammo,
    Medical,
    Material,
    Quest
}


[CreateAssetMenu(fileName = "SOItemDefinition", menuName = "InventoryConfig/SOItemDefinition")]
public class SOItemDefinition : ScriptableObject
{
    public int Id;
    public string Name;
    public Vector2Int Size;
    public int MaxStack;
    public bool CanRotate;
    public ItemCategory Category;
}
