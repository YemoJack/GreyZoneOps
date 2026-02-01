using QFramework;
using UnityEngine;

public class MapSceneBootstrap : MonoBehaviour, IController
{
    public SOMapDefinition OverrideMapDefinition;
    public bool AutoBeginRaid = true;
    private bool started;

    public void StartMap()
    {
        if (started)
        {
            return;
        }

        started = true;
        var mapSystem = this.GetSystem<MapSystem>();
        if (mapSystem == null)
        {
            return;
        }

        if (OverrideMapDefinition != null)
        {
            mapSystem.LoadMap(OverrideMapDefinition);
        }

        if (AutoBeginRaid)
        {
            mapSystem.BeginRaid();
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
