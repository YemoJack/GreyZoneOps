using QFramework;

public class CmdReloadWeapon : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetSystem<WeaponSystem>().ReloadCurrentWeapon();
    }
}
