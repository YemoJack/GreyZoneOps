using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Utilities
        RegisterUtility(new SystemUpdateScheduler());

        // 注册 Models
        RegisterModel(new WeaponInventoryModel());

        // 注册 Systems
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new InputSys());
        RegisterSystem(new WeaponSystem());
        RegisterSystem(new BulletManager());

    }
}
