using System;
using QFramework;

public interface IObjectPoolUtility : IUtility
{
    IObjectPool<T> CreatePool<T>(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null,
        int maxCount = int.MaxValue);
}
