using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // зЂВс Models
        //RegisterModel(new WeaponModel());
        //RegisterModel(new InventoryModel());
        //RegisterModel(new ExtractionModel());
        //RegisterModel(new WorldModel());

        // зЂВс Systems
        RegisterSystem(new PlayerSystem());
        RegisterSystem(new InputSys());
        //RegisterSystem(new WeaponSystem());
        //RegisterSystem(new InventorySystem());
        //RegisterSystem(new MapSystem());
        //RegisterSystem(new AIEnemySystem());
        //RegisterSystem(new LootSystem());
        //RegisterSystem(new ExtractionSystem());
        //RegisterSystem(new UISystem());
    }
}
