using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdStartAttack : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetSystem<WeaponSystem>().StartAttack();
    }
}


