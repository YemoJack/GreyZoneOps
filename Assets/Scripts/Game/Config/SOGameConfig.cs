using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemQualityColor
{
    public ItemQuality Quality;
    public Color Color;
}

[Serializable]
public struct InputAxisConfig
{
    public string MoveHorizontalAxis;
    public string MoveVerticalAxis;
    public string LookHorizontalAxis;
    public string LookVerticalAxis;
}

[Serializable]
public struct InputKeyConfig
{
    public int FireButton;
    public int AimButton;
    public int AltButton;
    public KeyCode ReloadKey;
    public KeyCode FireModeSwitchKey;
    public KeyCode JumpKey;
    public KeyCode SprintKey;
    public KeyCode CrouchKey;
    public KeyCode TabKey;
    public KeyCode InteractKey;
}

[CreateAssetMenu(fileName = "SOGameConfig", menuName = "GameConfig/Global")]
public class SOGameConfig : ScriptableObject
{
    [Header("Input 输入")]
    public InputAxisConfig AxisConfig = new InputAxisConfig
    {
        MoveHorizontalAxis = "Horizontal",
        MoveVerticalAxis = "Vertical",
        LookHorizontalAxis = "Mouse X",
        LookVerticalAxis = "Mouse Y"
    };
    public InputKeyConfig KeyConfig = new InputKeyConfig
    {
        FireButton = 0,
        AimButton = 1,
        AltButton = 2,
        ReloadKey = KeyCode.R,
        FireModeSwitchKey = KeyCode.B,
        JumpKey = KeyCode.Space,
        SprintKey = KeyCode.LeftShift,
        CrouchKey = KeyCode.LeftControl,
        TabKey = KeyCode.Tab,
        InteractKey = KeyCode.E
    };

    [Header("Player Movement 角色移动")]
    public float MoveSpeed = 4f;
    public float SprintSpeed = 7f;
    public float SpeedChangeRate = 10f;
    public float JumpHeight = 1.2f;
    public float Gravity = -15f;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;

    [Header("Player Look 视角")]
    public float MouseSensitivity = 1.2f;
    public float PitchClampMin = -75f;
    public float PitchClampMax = 85f;
    [Header("Player Recoil 后坐力")]
    public float RecoilRaiseSpeed = 180f;
    public float RecoilReturnSpeed = 60f;

    [Header("Interaction 交互")]
    public float InteractRange = 2.5f;
    public LayerMask InteractableLayers = ~0;
    public QueryTriggerInteraction InteractTriggerInteraction = QueryTriggerInteraction.Collide;
    public string DefaultDoorOpenPrompt = "Open";
    public string DefaultDoorClosePrompt = "Close";

    [Header("Inventory 库存系统")]
    public List<ItemQualityColor> ItemQualityColors = new List<ItemQualityColor>();
    public float DraggingItemAlpha = 0.8f;
    public Color InventoryHighlightColor = new Color(1f, 0.9f, 0.3f, 1f);
    public Color InventoryWarningColor = new Color(1f, 0.4f, 0.2f, 1f);

    [Header("Map 地图")]
    public int DefaultMapId = 0;

    [Header("Player 玩家")]
    public string PlayerPrefabName = "Player";
}
