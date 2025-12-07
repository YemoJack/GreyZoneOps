using System.Collections.Generic;
using UnityEngine;
using QFramework;

public class PlayerController : MonoBehaviour, IController
{
    public Transform WeaponRoot;
    public List<GameObject> weaponObjectList;

    private PlayerSystem playerSystem;

    private void Start()
    {
        LockCursor(true);
        playerSystem = this.GetSystem<PlayerSystem>();
        playerSystem.InitializeLoadout(WeaponRoot, weaponObjectList);
    }

    public void LockCursor(bool isLocked)
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
