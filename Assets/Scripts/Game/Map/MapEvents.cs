using UnityEngine;

public struct EventMapLoaded
{
    public SOMapDefinition Map;
}

public struct EventMapPrefabReady
{
    public SOMapDefinition Map;
    public GameObject MapRoot;
}

public struct EventPlayerSpawned
{
    public SOMapDefinition Map;
    public Transform PlayerTransform;
    public PlayerRuntime PlayerRuntime;
}

public struct EventMapReady
{
    public SOMapDefinition Map;
    public GameObject MapRoot;
    public Transform PlayerTransform;
}

public struct EventMapStateChanged
{
    public MapState Previous;
    public MapState Current;
}

public struct EventMapTimeChanged
{
    public float Elapsed;
    public float Remaining;
    public float Duration;
}

public struct EventExtractionStarted
{
    public string ExtractionId;
    public float Duration;
}

public struct EventExtractionProgress
{
    public string ExtractionId;
    public float Progress;
    public float Remaining;
    public float Duration;
}

public struct EventExtractionCancelled
{
    public string ExtractionId;
}

public struct EventExtractionCompleted
{
    public string ExtractionId;
}
