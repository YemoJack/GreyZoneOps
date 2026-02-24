using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class AudioSystem : AbstractSystem
{
    private const string SaveKeyAudioMasterVolume = "audio.master_volume";
    private const string SaveKeyAudioBgmVolume = "audio.bgm_volume";
    private const string SaveKeyAudioSfxVolume = "audio.sfx_volume";
    private const string SaveKeyAudioMuted = "audio.muted";

    private AudioModel model;
    private IAudioPlayer player;
    private IResLoader resLoader;
    private ISaveLoader saveLoader;
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    protected override void OnInit()
    {
        model = this.GetModel<AudioModel>();
        player = this.GetUtility<IAudioPlayer>();
        saveLoader = this.GetUtility<ISaveLoader>();

        LoadPersistedAudioSettings();
        ApplyOutputSettings();

        this.RegisterEvent<EventGameFlowStateChanged>(OnGameFlowStateChanged);
        this.RegisterEvent<EventExtractionStarted>(_ => PlaySfx(AudioSfxId.ExtractionStart));
        this.RegisterEvent<EventExtractionCancelled>(_ => PlaySfx(AudioSfxId.ExtractionCancel));
        this.RegisterEvent<EventExtractionSucceeded>(_ => PlaySfx(AudioSfxId.ExtractionSuccess));
        this.RegisterEvent<EventPlayerChangeWeapon>(_ => PlaySfx(AudioSfxId.WeaponSwitch));
        this.RegisterEvent<EventPlayerDeath>(_ => PlaySfx(AudioSfxId.PlayerDeath));

        this.RegisterEvent<EventWeaponFired>(OnWeaponFired);
        this.RegisterEvent<EventWeaponDryFire>(e => PlayFirearmCue(e.WeaponId, FirearmAudioCueType.DryFire, e.Position));
        this.RegisterEvent<EventWeaponReloadStarted>(e => PlayFirearmCue(e.WeaponId, FirearmAudioCueType.ReloadStart, e.Position));
        this.RegisterEvent<EventWeaponReloadFinished>(e => PlayFirearmCue(e.WeaponId, FirearmAudioCueType.ReloadFinish, e.Position));
    }

    public void SetMasterVolume(float volume)
    {
        EnsureDependencies();
        model.SetMasterVolume(volume);
        ApplyOutputSettings();
        SavePersistedAudioSettings();
        NotifySettingsChanged();
    }

    public void SetBgmVolume(float volume)
    {
        EnsureDependencies();
        model.SetBgmVolume(volume);
        ApplyOutputSettings();
        SavePersistedAudioSettings();
        NotifySettingsChanged();
    }

    public void SetSfxVolume(float volume)
    {
        EnsureDependencies();
        model.SetSfxVolume(volume);
        ApplyOutputSettings();
        SavePersistedAudioSettings();
        NotifySettingsChanged();
    }

    public void SetMuted(bool muted)
    {
        EnsureDependencies();
        model.SetMuted(muted);
        ApplyOutputSettings();
        SavePersistedAudioSettings();
        NotifySettingsChanged();
    }

    public void PlayBgm(AudioBgmId id, bool restartIfSame = false)
    {
        EnsureDependencies();
        if (id == AudioBgmId.None)
        {
            StopBgm();
            return;
        }

        if (!restartIfSame && model.CurrentBgmId == id)
        {
            return;
        }

        var catalog = ResolveCatalog();
        if (catalog == null || !catalog.TryGetBgm(id, out var entry) || entry == null)
        {
            StopBgm();
            return;
        }

        var clip = LoadAudioClip(entry.ResName);
        if (clip == null)
        {
            StopBgm();
            return;
        }

        player.PlayMusic(clip, entry.Loop, entry.Volume);
        model.SetCurrentBgm(id);
    }

    public void StopBgm()
    {
        EnsureDependencies();
        player.StopMusic();
        model.SetCurrentBgm(AudioBgmId.None);
    }

    public void PlaySfx(AudioSfxId id)
    {
        EnsureDependencies();
        if (!TryResolveSfx(id, out var entry))
        {
            return;
        }

        var clip = LoadAudioClip(entry.ResName);
        if (clip == null)
        {
            return;
        }

        var pitch = ResolvePitch(entry);
        player.PlaySfx2D(clip, entry.Volume, pitch);
    }

    public void PlaySfxAt(AudioSfxId id, Vector3 position)
    {
        EnsureDependencies();
        if (!TryResolveSfx(id, out var entry))
        {
            return;
        }

        var clip = LoadAudioClip(entry.ResName);
        if (clip == null)
        {
            return;
        }

        var pitch = ResolvePitch(entry);
        var spatialBlend = entry.PlayAs3D ? entry.SpatialBlend : 1f;
        player.PlaySfx3D(clip, position, entry.Volume, pitch, spatialBlend);
    }

    private void OnGameFlowStateChanged(EventGameFlowStateChanged evt)
    {
        switch (evt.Current)
        {
            case GameFlowState.StartMenu:
            case GameFlowState.LoadingToMenu:
                PlayBgm(AudioBgmId.Menu);
                break;
            case GameFlowState.InRaid:
            case GameFlowState.LoadingToGame:
                PlayBgm(AudioBgmId.Raid);
                break;
            case GameFlowState.RaidEnded:
                PlayBgm(AudioBgmId.RaidEnded);
                break;
        }
    }

    private void OnWeaponFired(EventWeaponFired evt)
    {
        float range = ResolveGunshotPropagationRange(evt.GunshotRange);
        var played = PlayFirearmCue(
            evt.WeaponId,
            FirearmAudioCueType.Fire,
            evt.Position,
            maxDistance: range > 0f ? range : -1f,
            rolloffMode: AudioRolloffMode.Linear);
        if (!played)
        {
            // Optional fallback for older catalogs without firearm profiles.
            // Intentionally no generic WeaponFire id fallback anymore.
        }

        if (range > 0f)
        {
            this.SendEvent(new EventGunshotNoiseEmitted
            {
                WeaponId = evt.WeaponId,
                WeaponName = evt.WeaponName,
                Position = evt.Position,
                Range = range
            });
        }
    }

    private void ApplyOutputSettings()
    {
        if (player == null || model == null)
        {
            return;
        }

        player.SetMasterVolume(model.MasterVolume);
        player.SetMusicVolume(model.BgmVolume);
        player.SetSfxVolume(model.SfxVolume);
        player.SetMuted(model.Muted);
    }

    private void NotifySettingsChanged()
    {
        if (model == null)
        {
            return;
        }

        this.SendEvent(new EventAudioSettingsChanged
        {
            MasterVolume = model.MasterVolume,
            BgmVolume = model.BgmVolume,
            SfxVolume = model.SfxVolume,
            Muted = model.Muted
        });
    }

    private void EnsureDependencies()
    {
        if (model == null)
        {
            model = this.GetModel<AudioModel>();
        }

        if (player == null)
        {
            player = this.GetUtility<IAudioPlayer>();
        }

        if (resLoader == null)
        {
            resLoader = this.GetUtility<IResLoader>();
        }

        if (saveLoader == null)
        {
            saveLoader = this.GetUtility<ISaveLoader>();
        }
    }

    private SOAudioCatalog ResolveCatalog()
    {
        return GameSettingManager.Instance?.Config?.AudioCatalog;
    }

    private bool PlayFirearmCue(
        int weaponId,
        FirearmAudioCueType cueType,
        Vector3 position,
        float maxDistance = -1f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic)
    {
        EnsureDependencies();
        var catalog = ResolveCatalog();
        if (catalog == null || !catalog.TryGetFirearmProfile(weaponId, out var profile) || profile == null)
        {
            return false;
        }

        if (!profile.TryGetCue(cueType, out var cue) || cue == null)
        {
            return false;
        }

        var clip = LoadAudioClip(cue.ResName);
        if (clip == null)
        {
            return false;
        }

        const float clipVolume = 1f;
        const float pitch = 1f;
        const float spatialBlend = 1f;
        player.PlaySfx3D(clip, position, clipVolume, pitch, spatialBlend, rolloffMode, maxDistance);

        return true;
    }

    private bool TryResolveSfx(AudioSfxId id, out AudioSfxEntry entry)
    {
        var catalog = ResolveCatalog();
        if (catalog != null && catalog.TryGetSfx(id, out entry))
        {
            return entry != null;
        }

        entry = null;
        return false;
    }

    private AudioClip LoadAudioClip(string resName)
    {
        EnsureDependencies();
        if (string.IsNullOrWhiteSpace(resName) || resLoader == null)
        {
            return null;
        }

        if (clipCache.TryGetValue(resName, out var cached) && cached != null)
        {
            return cached;
        }

        var clip = resLoader.LoadSync<AudioClip>(resName);
        if (clip != null)
        {
            clipCache[resName] = clip;
        }

        return clip;
    }

    private void LoadPersistedAudioSettings()
    {
        EnsureDependencies();
        if (model == null || saveLoader == null)
        {
            return;
        }

        model.SetMasterVolume(saveLoader.Load(SaveKeyAudioMasterVolume, model.MasterVolume));
        model.SetBgmVolume(saveLoader.Load(SaveKeyAudioBgmVolume, model.BgmVolume));
        model.SetSfxVolume(saveLoader.Load(SaveKeyAudioSfxVolume, model.SfxVolume));
        model.SetMuted(saveLoader.Load(SaveKeyAudioMuted, model.Muted));
    }

    private void SavePersistedAudioSettings()
    {
        EnsureDependencies();
        if (model == null || saveLoader == null)
        {
            return;
        }

        saveLoader.Save(SaveKeyAudioMasterVolume, model.MasterVolume);
        saveLoader.Save(SaveKeyAudioBgmVolume, model.BgmVolume);
        saveLoader.Save(SaveKeyAudioSfxVolume, model.SfxVolume);
        saveLoader.Save(SaveKeyAudioMuted, model.Muted);
    }

    private float ResolveGunshotPropagationRange(float eventRange)
    {
        return Mathf.Max(0f, eventRange);
    }

    private static float ResolvePitch(AudioSfxEntry entry)
    {
        return entry == null ? 1f : ResolvePitch(entry.PitchRange);
    }

    private static float ResolvePitch(Vector2 pitchRange)
    {
        var min = Mathf.Min(pitchRange.x, pitchRange.y);
        var max = Mathf.Max(pitchRange.x, pitchRange.y);
        if (Mathf.Approximately(min, max))
        {
            return Mathf.Max(0.01f, min);
        }

        return Mathf.Max(0.01f, Random.Range(min, max));
    }
}
