using QFramework;
using UnityEngine;

public class AudioModel : AbstractModel
{
    public float MasterVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool Muted { get; private set; }
    public AudioBgmId CurrentBgmId { get; private set; } = AudioBgmId.None;

    protected override void OnInit()
    {
        var config = GameSettingManager.Instance?.Config;
        if (config == null)
        {
            return;
        }

        MasterVolume = Mathf.Clamp01(config.DefaultMasterVolume);
        BgmVolume = Mathf.Clamp01(config.DefaultBgmVolume);
        SfxVolume = Mathf.Clamp01(config.DefaultSfxVolume);
        Muted = config.DefaultMuted;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
    }

    public void SetMuted(bool muted)
    {
        Muted = muted;
    }

    public void SetCurrentBgm(AudioBgmId id)
    {
        CurrentBgmId = id;
    }
}
