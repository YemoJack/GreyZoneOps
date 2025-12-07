using QFramework;
using System.Collections.Generic;
using UnityEngine;

public class BulletManager : AbstractSystem
{
    private BulletPool bulletPool;
    private readonly List<GOBullet> bullets = new List<GOBullet>();

    protected override void OnInit()
    {
        bulletPool = new BulletPool(1000);
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
