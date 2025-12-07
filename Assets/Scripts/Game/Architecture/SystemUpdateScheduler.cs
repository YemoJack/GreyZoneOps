using System.Collections.Generic;
using QFramework;
using UnityEngine;

public interface IUpdateSystem
{
    void OnUpdate(float deltaTime);
}

public interface IGameLoop : IUtility
{
    float DeltaTime { get; }
    float UnscaledDeltaTime { get; }
    float TimeScale { get; }
    bool IsPaused { get; }

    void Register(IUpdateSystem updateSystem);
    void Unregister(IUpdateSystem updateSystem);
    void Tick(float deltaTime);

    void Pause();
    void Resume();
    void TogglePause();
    void SetTimeScale(float scale);
}

public class SystemUpdateScheduler : IGameLoop
{
    private readonly List<IUpdateSystem> updateSystems = new List<IUpdateSystem>();
    private bool isPaused;
    private float timeScale = 1f;

    public float DeltaTime { get; private set; }
    public float UnscaledDeltaTime { get; private set; }
    public float TimeScale => timeScale;
    public bool IsPaused => isPaused;

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

    public void Pause() => isPaused = true;

    public void Resume() => isPaused = false;

    public void TogglePause() => isPaused = !isPaused;

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0f, scale);
    }

    public void Tick(float deltaTime)
    {
        UnscaledDeltaTime = deltaTime;

        if (isPaused || timeScale <= 0f)
        {
            DeltaTime = 0f;
            return;
        }

        DeltaTime = deltaTime * timeScale;

        for (int i = 0; i < updateSystems.Count; i++)
        {
            updateSystems[i].OnUpdate(DeltaTime);
        }
    }
}
