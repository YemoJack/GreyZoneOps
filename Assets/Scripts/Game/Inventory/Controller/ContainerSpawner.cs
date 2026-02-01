using QFramework;
using UnityEngine;


public class ContainerSpawner : MonoBehaviour, IController
{
    // Deprecated: scene containers are now spawned by MapSystem after map load.

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
