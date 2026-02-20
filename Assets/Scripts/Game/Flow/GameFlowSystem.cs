using QFramework;

public class GameFlowSystem : AbstractSystem
{
    private GameFlowModel flowModel;

    protected override void OnInit()
    {
        flowModel = this.GetModel<GameFlowModel>();
        this.RegisterEvent<EventMapStateChanged>(OnMapStateChanged);
        this.RegisterEvent<EventPlayerDeath>(OnPlayerDeath);
        this.RegisterEvent<EventExtractionSucceeded>(OnExtractionSucceeded);
    }

    public GameFlowState CurrentState => flowModel != null ? flowModel.State : GameFlowState.None;

    public void EnterBooting() => SetState(GameFlowState.Booting);

    public void EnterStartMenu() => SetState(GameFlowState.StartMenu);

    public void EnterLoadingToGame() => SetState(GameFlowState.LoadingToGame);

    public void EnterInRaid() => SetState(GameFlowState.InRaid);

    public void EnterRaidEnded() => SetState(GameFlowState.RaidEnded);

    public void EnterLoadingToMenu() => SetState(GameFlowState.LoadingToMenu);

    private void OnMapStateChanged(EventMapStateChanged e)
    {
        if (e.Current == MapState.InRaid)
        {
            EnterInRaid();
        }
        else if (e.Current == MapState.Ended)
        {
            EnterRaidEnded();
        }
    }

    private void OnPlayerDeath(EventPlayerDeath e)
    {
        EnterRaidEnded();
    }

    private void OnExtractionSucceeded(EventExtractionSucceeded e)
    {
        EnterRaidEnded();
    }

    private void SetState(GameFlowState state)
    {
        if (flowModel == null)
        {
            flowModel = this.GetModel<GameFlowModel>();
        }

        if (flowModel == null)
        {
            return;
        }

        var previous = flowModel.State;
        if (!flowModel.SetState(state))
        {
            return;
        }

        this.SendEvent(new EventGameFlowStateChanged
        {
            Previous = previous,
            Current = state
        });
    }
}

