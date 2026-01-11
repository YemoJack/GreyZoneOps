using System;
using System.Collections.Generic;
using UnityEngine;





[CreateAssetMenu(fileName = "SOInventoryContainerConfig", menuName = "InventoryConfig/InventoryContainerConfig")]
public class SOInventoryContainerConfig : ScriptableObject
{
    public int mapId;
    public List<SOContainerConfig> containerConfigs = new List<SOContainerConfig>();
}