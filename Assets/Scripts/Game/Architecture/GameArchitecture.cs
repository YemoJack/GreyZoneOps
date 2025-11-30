using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // зЂВс Models
        RegisterModel(new WeaponInventoryModel());

        // зЂВс Systems
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new InputSys());
        RegisterSystem(new WeaponSystem());

    }
}
