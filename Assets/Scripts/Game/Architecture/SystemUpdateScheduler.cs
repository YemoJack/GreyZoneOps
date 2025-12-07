using System.Collections.Generic;
using QFramework;

public interface IUpdateSystem
{
    void OnUpdate(float deltaTime);
}

public class SystemUpdateScheduler : IUtility
{
    private readonly List<IUpdateSystem> updateSystems = new List<IUpdateSystem>();

    public void Register(IUpdateSystem updateSystem)
    {
        if (updateSystem != null && !updateSystems.Contains(updateSystem))
        {
            updateSystems.Add(updateSystem);
        }
    }

    public void Unregister(IUpdateSystem updateSystem)
    {
        if (updateSystem != null)
        {
            updateSystems.Remove(updateSystem);
        }
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < updateSystems.Count; i++)
        {
            updateSystems[i].OnUpdate(deltaTime);
        }
    }
}
