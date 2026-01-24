using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;


public class ContainerSpawner : MonoBehaviour, IController
{
    private void Start()
    {
        this.RegisterEvent<EventGameInit>(Spawner).UnRegisterWhenGameObjectDestroyed(this);
    }


    private void Spawner(EventGameInit e)
    {
        var model = this.GetModel<InventoryContainerModel>();
        if (model == null)
        {
            Debug.LogError("InventoryContainerModel is Null");
            return;
        }

        var mapConfig = model.CurrentMapConfig;
        if (mapConfig == null)
        {
            Debug.LogError("ContainerSpawner mapConfig is null");
            return;
        }

        if (mapConfig != null && mapConfig.sceneContainers.Count > 0)
        {
            SpawnFromMapConfig(model, mapConfig);
            return;
        }


    }

    private void SpawnFromMapConfig(InventoryContainerModel model, SOInventoryContainerConfig config)
    {
        foreach (var entry in config.sceneContainers)
        {
            if (entry == null || entry.prefab == null) continue;

            var container = ResolveContainer(model, entry);
            var pos = entry.position;
            var rot = Quaternion.Euler(entry.rotationEuler);
            var instance = Instantiate(entry.prefab, pos, rot, null);

            if (entry.setInteractableLayer)
            {
                var interactLayer = LayerMask.NameToLayer("Interactable");
                if (interactLayer >= 0)
                {
                    instance.layer = interactLayer;
                }
            }

            var interactable = instance.GetComponent<ContainerInteractable>();
            if (interactable == null)
            {
                interactable = instance.AddComponent<ContainerInteractable>();
            }

            if (container != null)
            {
                interactable.ContainerId = container.InstanceId;
            }
            else if (!string.IsNullOrEmpty(entry.containerIdOverride))
            {
                interactable.ContainerId = entry.containerIdOverride;
            }

            interactable.FallbackType = entry.fallbackType;
        }
    }


    private static InventoryContainer ResolveContainer(InventoryContainerModel model, SceneContainerSpawnConfig entry)
    {
        if (model == null || entry == null) return null;

        if (entry.containerConfig != null)
        {
            return model.EnsureContainer(entry.containerConfig, entry.containerIdOverride);
        }

        if (!string.IsNullOrEmpty(entry.containerIdOverride))
        {
            return model.GetContainer(entry.containerIdOverride);
        }

        return null;
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
