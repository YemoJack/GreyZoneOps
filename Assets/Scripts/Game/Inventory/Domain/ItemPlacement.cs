using UnityEngine;

public sealed class ItemPlacement
{
    public ItemInstance Item;
    public Vector2Int Pos;     // Grid cell (top-left) where the item starts
    public Vector2Int Size;    // How many cells the item occupies (after rotation)
    public bool Rotated;
}

