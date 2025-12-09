using QFramework;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : AbstractSystem, IUpdateSystem
{
    private IObjectPool<GOBullet> bulletPool;
    private readonly List<GOBullet> bullets = new List<GOBullet>();
    private IGameLoop updateScheduler;
    private IObjectPoolUtility objectPoolUtility;

    protected override void OnInit()
    {
        objectPoolUtility = this.GetUtility<IObjectPoolUtility>();
        bulletPool = objectPoolUtility.CreatePool(() => new GOBullet(),
            onGet: bullet => bullet.active = true,
            onRelease: bullet => bullet.active = false,
            maxCount: 1000);
        updateScheduler = this.GetUtility<IGameLoop>();
        updateScheduler.Register(this);
    }

    // 创建新子弹
    public GOBullet Spawn(Vector3 pos, Vector3 dir,FirearmWeapon firearmWeapon)
    {
        var b = bulletPool.Get();

        b.Init(pos, dir,9.8f, firearmWeapon);
        bullets.Add(b);
        return b;
    }

    public void RecycleBullet(GOBullet bullet)
    {
        bullets.Remove(bullet);
        bulletPool.Release(bullet);
    }

    // System 不能自己 Update，需要外部驱动
    public void OnUpdate(float dt)
    {
        for (int i = bullets.Count - 1; i >= 0; i--)
        {
            var b = bullets[i];

            b.Simulate(dt);

            if (!b.active)
            {
                RecycleBullet(b);
            }
        }
    }
}
