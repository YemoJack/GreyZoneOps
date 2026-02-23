using UnityEngine;

public struct EventAudioSettingsChanged
{
    public float MasterVolume;
    public float BgmVolume;
    public float SfxVolume;
    public bool Muted;
}

public struct EventWeaponFired
{
    public int WeaponId;
    public string WeaponName;
    public Vector3 Position;
    public float GunshotRange;
}

public struct EventWeaponDryFire
{
    public int WeaponId;
    public string WeaponName;
    public Vector3 Position;
}

public struct EventWeaponReloadStarted
{
    public int WeaponId;
    public string WeaponName;
    public float Duration;
    public Vector3 Position;
}

public struct EventWeaponReloadFinished
{
    public int WeaponId;
    public string WeaponName;
    public Vector3 Position;
}

public struct EventGunshotNoiseEmitted
{
    public int WeaponId;
    public string WeaponName;
    public Vector3 Position;
    public float Range;
}
