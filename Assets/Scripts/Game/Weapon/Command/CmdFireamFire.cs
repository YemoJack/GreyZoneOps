using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using QFramework;

public struct HitTargetData
{
    public Vector3 dir;
    public float distance;
    public float time;
}



public class CmdFireamFire : AbstractCommand
{

    private Vector3 startPos;
    private Vector3 forward;

    private FirearmWeapon weapon;
    private SOFirearmConfig firearmConfig;
    private BulletManager bulletManager;

    public float gravity = 9.81f;

    public CmdFireamFire(FirearmWeapon weapon)
    {
        this.weapon = weapon;
        this.firearmConfig = weapon.Config as SOFirearmConfig;
    }

    public void Init(Vector3 startPos, Vector3 forward)
    {
        this.startPos = startPos;
        this.forward = forward;
    }

    protected override void OnExecute()
    {
        FireByDistance();
    }


    void FireByDistance()
    {
        Vector3 dir = forward.normalized;

        // 1. 优势射程内 → hitscan 优先
        if (Physics.Raycast(startPos, dir, out var hit, firearmConfig.range))
        {
            // 立即命中
            weapon.OnHitTarget(hit);

            // 可选：命中时生成一颗短生命周期的“假子弹”
            // 用于在摄像机特写时看到一条 tracer
            return;
        }

        // 2. 射线未命中 → 实体子弹从枪口飞出去
        SpawnBullet(startPos, dir);
    }


    private void SpawnBullet(Vector3 start, Vector3 dir)
    {
        if(bulletManager == null)
        {
            bulletManager = this.GetSystem<BulletManager>();
        }
        // 实体子弹应当从真实枪口飞出去，不是从“range 后面”生成
        bulletManager.Spawn(start, dir, weapon);
    }

}





//private void Fire()
//{
//    // 直接使用真实弹道模拟，不再使用旧的预测+插值系统
//    RunRealtimeSimulationRT().Forget();
//}

///// <summary>
///// ✔ 最终版实时弹道模拟（真实物理 + 连续 Raycast）
///// </summary>
//private async UniTaskVoid RunRealtimeSimulationRT()
//{
//    Vector3 pos = startPos;
//    Vector3 velocity = forward * firearmConfig.bulletSpeed;

//    float totalTime = 0f;

//    float maxTime = firearmConfig.maxRange / firearmConfig.bulletSpeed + 1f;

//    while (totalTime < maxTime)
//    {
//        float dt = Time.deltaTime;
//        Vector3 move = velocity * dt;

//        // 连续碰撞检测（防止高速穿透）
//        if (Physics.Raycast(pos, move.normalized, out RaycastHit hit, move.magnitude))
//        {
//            HitTarget(hit);
//            Debug.DrawLine(pos, hit.point, Color.red, 3f);
//            Debug.Log($"命中 {hit.point} 飞行时间:{totalTime:F3}s");
//            return;
//        }

//        // 无碰撞 → 更新物理
//        Vector3 lastPos = pos;
//        pos += move;
//        velocity += Vector3.down * gravity * dt;

//        // 画轨迹调试线
//        Debug.DrawLine(lastPos, pos, Color.yellow, 3f);

//        if (Vector3.Distance(startPos, pos) > firearmConfig.maxRange)
//        {
//            Debug.Log($"未命中，飞行时间:{totalTime:F3}s");
//            return;
//        }

//        totalTime += dt;

//        await UniTask.Yield();  // 等待下一帧
//    }
//}