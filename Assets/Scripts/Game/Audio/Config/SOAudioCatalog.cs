using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum AudioBgmId
{
    None = 0,
    Menu = 1,
    Raid = 2,
    RaidEnded = 3
}

public enum AudioSfxId
{
    None = 0,
    UiClick = 1,
    WeaponSwitch = 2,
    ExtractionStart = 7,
    ExtractionCancel = 8,
    ExtractionSuccess = 9,
    PlayerDeath = 10
}

public enum FirearmAudioCueType
{
    Fire = 0,
    DryFire = 1,
    ReloadStart = 2,
    ReloadFinish = 3
}

[Serializable]
public class AudioBgmEntry
{
    public AudioBgmId Id = AudioBgmId.None;
    public string ResName;
    [Range(0f, 1f)]
    public float Volume = 1f;
    public bool Loop = true;
}

[Serializable]
public class AudioSfxEntry
{
    public AudioSfxId Id = AudioSfxId.None;
    public string ResName;
    [Range(0f, 1f)]
    public float Volume = 1f;
    public bool PlayAs3D = false;
    [Range(0f, 1f)]
    public float SpatialBlend = 1f;
    public Vector2 PitchRange = new Vector2(1f, 1f);
}

[Serializable]
public class FirearmAudioCueEntry
{
    public string ResName;
}

[Serializable]
public class FirearmAudioProfileEntry
{
    public int WeaponId;

    [Header("Shot")]
    public FirearmAudioCueEntry Fire = new FirearmAudioCueEntry();
    public FirearmAudioCueEntry DryFire = new FirearmAudioCueEntry();

    [Header("Reload")]
    public FirearmAudioCueEntry ReloadStart = new FirearmAudioCueEntry();
    public FirearmAudioCueEntry ReloadFinish = new FirearmAudioCueEntry();

    public bool TryGetCue(FirearmAudioCueType cueType, out FirearmAudioCueEntry cue)
    {
        switch (cueType)
        {
            case FirearmAudioCueType.Fire:
                cue = Fire;
                return cue != null;
            case FirearmAudioCueType.DryFire:
                cue = DryFire;
                return cue != null;
            case FirearmAudioCueType.ReloadStart:
                cue = ReloadStart;
                return cue != null;
            case FirearmAudioCueType.ReloadFinish:
                cue = ReloadFinish;
                return cue != null;
            default:
                cue = null;
                return false;
        }
    }
}

[CreateAssetMenu(fileName = "SOAudioCatalog", menuName = "GameConfig/Audio Catalog")]
public class SOAudioCatalog : ScriptableObject
{
    public List<AudioBgmEntry> Bgms = new List<AudioBgmEntry>();
    [FormerlySerializedAs("Sfxs")]
    public List<AudioSfxEntry> CommonSfxs = new List<AudioSfxEntry>();
    public List<FirearmAudioProfileEntry> FirearmAudioProfiles = new List<FirearmAudioProfileEntry>();

    public bool TryGetBgm(AudioBgmId id, out AudioBgmEntry entry)
    {
        for (int i = 0; i < Bgms.Count; i++)
        {
            var item = Bgms[i];
            if (item != null && item.Id == id)
            {
                entry = item;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public bool TryGetSfx(AudioSfxId id, out AudioSfxEntry entry)
    {
        for (int i = 0; i < CommonSfxs.Count; i++)
        {
            var item = CommonSfxs[i];
            if (item != null && item.Id == id)
            {
                entry = item;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public bool TryGetFirearmProfile(int weaponId, out FirearmAudioProfileEntry entry)
    {
        for (int i = 0; i < FirearmAudioProfiles.Count; i++)
        {
            var item = FirearmAudioProfiles[i];
            if (item != null && item.WeaponId == weaponId)
            {
                entry = item;
                return true;
            }
        }

        entry = null;
        return false;
    }
}
