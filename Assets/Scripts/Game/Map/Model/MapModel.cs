using QFramework;
using UnityEngine;

public enum MapState
{
    None,
    Loading,
    InRaid,
    Ended
}

public class MapModel : AbstractModel
{
    private const string MapDefinitionPrefix = "Cfg_MapDefinition_";

    public SOMapDefinition CurrentMap { get; private set; }
    public MapState State { get; private set; } = MapState.None;
    public float RaidElapsed { get; private set; }
    public float RaidDuration { get; private set; }

    public float RemainingTime => Mathf.Max(0f, RaidDuration - RaidElapsed);

    protected override void OnInit()
    {
        LoadDefaultMap();
    }

    public void LoadDefaultMap()
    {
        var mapId = 0;
        var settings = GameSettingManager.Instance;
        if (settings != null && settings.Config != null)
        {
            mapId = settings.Config.DefaultMapId;
        }

        LoadMapById(mapId);
    }

    public void LoadMapById(int mapId)
    {
        var def = this.GetUtility<IResLoader>().LoadSync<SOMapDefinition>($"{MapDefinitionPrefix}{mapId}");
        if (def == null)
        {
            Debug.LogWarning($"MapModel: MapDefinition not found, id={mapId}");
        }
        SetMap(def);
    }

    public void SetMap(SOMapDefinition definition)
    {
        CurrentMap = definition;
        RaidDuration = definition != null ? Mathf.Max(0f, definition.raidDurationSeconds) : 0f;
        RaidElapsed = 0f;
        State = definition != null ? MapState.Loading : MapState.None;
    }

    public void SetState(MapState state)
    {
        State = state;
    }

    public void AddTime(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        RaidElapsed += deltaTime;
        if (RaidDuration > 0f && RaidElapsed > RaidDuration)
        {
            RaidElapsed = RaidDuration;
        }
    }
}
