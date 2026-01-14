using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Utilities
        RegisterUtility<IResLoader>(new ResLoaderYoo());


    }


    public void Registor()
    {
        var gameLoop = new SystemUpdateScheduler();
        RegisterUtility(gameLoop);
        RegisterUtility<IGameLoop>(gameLoop);
        RegisterUtility<IObjectPoolUtility>(new ObjectPoolUtility());

        // 注册 Models
        RegisterModel(new WeaponInventoryModel());
        RegisterModel(new InventoryContainerModel());

        // 注册 Systems
        RegisterSystem(new InputSys());
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new WeaponSystem());
        RegisterSystem(new BulletManager());
        RegisterSystem(new InventorySystem());
        RegisterSystem(new InteractionSystem());
    }
}
