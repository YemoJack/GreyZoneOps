using System;
using System.Collections.Generic;

public interface IObjectPool<T>
{
    int Count { get; }
    T Get();
    void Release(T item);
    void Clear();
}

public class ObjectPool<T> : IObjectPool<T>
{
    private readonly Stack<T> objects = new Stack<T>();
    private readonly Func<T> factory;
    private readonly Action<T> onGet;
    private readonly Action<T> onRelease;
    private readonly int maxCount;

    public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null, int maxCount = int.MaxValue)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.onGet = onGet;
        this.onRelease = onRelease;
        this.maxCount = Math.Max(0, maxCount);
    }

    public int Count => objects.Count;

    public T Get()
    {
        var item = objects.Count > 0 ? objects.Pop() : factory();
        onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null)
        {
            return;
        }

        onRelease?.Invoke(item);

        if (objects.Count < maxCount)
        {
            objects.Push(item);
        }
    }

    public void Clear()
    {
        objects.Clear();
    }
}

public class ObjectPoolUtility : IObjectPoolUtility
{
    public IObjectPool<T> CreatePool<T>(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null,
        int maxCount = int.MaxValue)
    {
        return new ObjectPool<T>(factory, onGet, onRelease, maxCount);
    }
}
