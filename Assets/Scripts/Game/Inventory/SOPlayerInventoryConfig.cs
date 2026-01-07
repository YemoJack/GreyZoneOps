using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOPlayerInventoryConfig", menuName = "InventoryConfig/PlayerInventoryConfig")]
public class SOPlayerInventoryConfig : ScriptableObject
{
    public List<ContainerConfig> containerConfigs = new List<ContainerConfig>();
}
