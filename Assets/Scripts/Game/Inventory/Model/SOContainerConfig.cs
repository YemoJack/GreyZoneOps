using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ContainerPart
{
    public int partIndex;
    public Vector2Int Size;
}


[CreateAssetMenu(fileName = "SOContainerConfig", menuName = "InventoryConfig/ContainerConfig")]
public class SOContainerConfig : ScriptableObject
{
    public int containerId;
    public string containerName;
    public InventoryContainerType containerType;

    public List<ContainerPart> partGridDatas = new List<ContainerPart>();
}
