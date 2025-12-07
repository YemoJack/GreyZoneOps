using System.Collections.Generic;

public class BulletPool
{
    private readonly Stack<GOBullet> pool = new Stack<GOBullet>();
    private readonly int maxCount;

    public BulletPool(int maxCount)
    {
        this.maxCount = maxCount;
    }

    public GOBullet Get()
    {
        if (pool.Count > 0)
        {
            var bullet = pool.Pop();
            bullet.active = true;
            return bullet;
        }

        return new GOBullet(); // new 不会频繁执行，因为有 pool
    }

    public void Release(GOBullet bullet)
    {
        bullet.active = false;
        if (pool.Count < maxCount)
        {
            pool.Push(bullet);
        }
        else
        {
            // 超过容量，可选择销毁或丢弃
        }
    }
}
