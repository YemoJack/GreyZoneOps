using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Utilities
        var gameLoop = new SystemUpdateScheduler();
        RegisterUtility(gameLoop);
        RegisterUtility<IGameLoop>(gameLoop);

        // 注册 Models
        RegisterModel(new WeaponInventoryModel());

        // 注册 Systems
        RegisterSystem(new InputSys());
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new WeaponSystem());
        RegisterSystem(new BulletManager());

    }
}
