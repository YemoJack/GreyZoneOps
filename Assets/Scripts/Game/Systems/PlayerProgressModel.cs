using QFramework;

public class PlayerProgressModel : AbstractModel
{
    public PlayerProgressSaveData Data { get; private set; }

    protected override void OnInit()
    {
        Data = new PlayerProgressSaveData();
        Data.Normalize();
    }

    public PlayerProgressSaveData GetMutableData()
    {
        if (Data == null)
        {
            Data = new PlayerProgressSaveData();
        }

        Data.Normalize();
        return Data;
    }

    public void Apply(PlayerProgressSaveData data)
    {
        Data = data != null ? data.Clone() : new PlayerProgressSaveData();
        Data.Normalize();
    }

    public PlayerProgressSaveData Export()
    {
        if (Data == null)
        {
            Data = new PlayerProgressSaveData();
        }

        Data.Normalize();
        return Data.Clone();
    }
}
