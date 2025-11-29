using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;


public enum PlayerAnimState
{
    Idle,
    Walk,
    Run,
}




public class PlayerController : MonoBehaviour,IController
{

    public Animator Animator;

    private PlayerSystem playerSystem;


    private static readonly int AnimState = Animator.StringToHash("AnimState");





    void Start()
    {
        playerSystem = this.GetSystem<PlayerSystem>();

        this.RegisterEvent<EventPlayerChangeMoveState>(OnPlayerMoveStateChanged)
            .UnRegisterWhenGameObjectDestroyed(this);
        
    }

  


    private void OnPlayerMoveStateChanged(EventPlayerChangeMoveState e)
    {
        print(e.CurrentState);
        Animator.SetInteger("AnimState", (int)e.CurrentState);
    }





    public IArchitecture GetArchitecture()
    {
        return GameArchitecture.Interface;
    }
}
