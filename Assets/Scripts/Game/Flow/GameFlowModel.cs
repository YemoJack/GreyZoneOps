using QFramework;

public enum GameFlowState
{
    None,
    Booting,
    StartMenu,
    LoadingToGame,
    InRaid,
    RaidEnded,
    LoadingToMenu
}

public class GameFlowModel : AbstractModel
{
    public GameFlowState State { get; private set; } = GameFlowState.None;

    protected override void OnInit()
    {
        State = GameFlowState.None;
    }

    public bool SetState(GameFlowState state)
    {
        if (State == state)
        {
            return false;
        }

        State = state;
        return true;
    }
}

