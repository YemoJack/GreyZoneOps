using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 注册 Utilities
        RegisterUtility<IResLoader>(new ResLoaderYoo());
        RegisterUtility<ISaveLoader>(new SaveLoaderEasy());
        RegisterUtility<IAudioPlayer>(new UnityAudioPlayerUtility());
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
        RegisterModel(new PersistentInventoryModel());
        RegisterModel(new MapModel());
        RegisterModel(new GameFlowModel());
        RegisterModel(new PlayerProgressModel());
        RegisterModel(new AudioModel());

        // 注册 Systems
        RegisterSystem(new GameFlowSystem());
        RegisterSystem(new InputSys());
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new HealthSystem());
        RegisterSystem(new WeaponSystem());
        RegisterSystem(new BulletManager());
        RegisterSystem(new InventorySystem());
        RegisterSystem(new PlayerProgressSystem());
        RegisterSystem(new InteractionSystem());
        RegisterSystem(new MapSystem());
        RegisterSystem(new AudioSystem());
        RegisterSystem(new DevTestSystem());
    }
}
