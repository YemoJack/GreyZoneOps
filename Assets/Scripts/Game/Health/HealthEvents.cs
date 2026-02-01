public struct EventPlayerHealthChanged
{
    public HealthComponent Health;
    public float Current;
    public float Max;
    public float Normalized;
}

public struct EventPlayerDeath
{
    public HealthComponent Health;
}
