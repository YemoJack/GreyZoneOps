using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct ContainerPart
{
    public int partIndex;
    public Vector2Int Size;
}

[Serializable]
public class ContainerConfig
{
    public int containerId;
    public string containerName;
    public InventoryContainerType containerType;

    public List<ContainerPart> partGridDatas = new List<ContainerPart>();
}



[CreateAssetMenu(fileName = "SOInventoryContainerConfig", menuName = "InventoryConfig/InventoryContainerConfig")]
public class SOInventoryContainerConfig : ScriptableObject
{
    public List<ContainerConfig> containerConfigs = new List<ContainerConfig>();
}