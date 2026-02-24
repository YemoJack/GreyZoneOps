using QFramework;
using UnityEngine;
using UnityEngine.UI;
using ZMUIFrameWork;

public class SettingWindow : WindowBase
{
    public static bool IsWindowVisible { get; private set; }

    public SettingWindowDataComponent dataCompt;

    private AudioSystem audioSystem;
    private AudioModel audioModel;
    private bool sliderCallbacksBound;

    public override void OnAwake()
    {
        dataCompt = gameObject.GetComponent<SettingWindowDataComponent>();
        dataCompt.InitComponent(this);

        audioSystem = this.GetSystem<AudioSystem>();
        audioModel = this.GetModel<AudioModel>();
        BindSliderCallbacks();

        base.OnAwake();
    }

    public override void OnShow()
    {
        base.OnShow();
        IsWindowVisible = true;
        ShowAudioTab();
        RefreshAudioSliders();
    }

    public override void OnHide()
    {
        IsWindowVisible = false;
        base.OnHide();
    }

    public override void OnDestroy()
    {
        IsWindowVisible = false;
        UnbindSliderCallbacks();
        base.OnDestroy();
    }

    public void OnAudioToggleChange(bool state, Toggle toggle)
    {
        if (!state)
        {
            return;
        }

        ShowAudioTab();
    }

    public void OnOtherToggleChange(bool state, Toggle toggle)
    {
        if (!state)
        {
            return;
        }

        ShowOtherTab();
    }

    public void OnCloseButtonClick()
    {
        HideWindow();
    }

    private void BindSliderCallbacks()
    {
        if (sliderCallbacksBound || dataCompt == null)
        {
            return;
        }

        if (dataCompt.AudioVolumeSlider != null)
        {
            dataCompt.AudioVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (dataCompt.MusicVolumeSlider != null)
        {
            dataCompt.MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (dataCompt.SoundVolumeSlider != null)
        {
            dataCompt.SoundVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        sliderCallbacksBound = true;
    }

    private void UnbindSliderCallbacks()
    {
        if (!sliderCallbacksBound || dataCompt == null)
        {
            return;
        }

        if (dataCompt.AudioVolumeSlider != null)
        {
            dataCompt.AudioVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        }

        if (dataCompt.MusicVolumeSlider != null)
        {
            dataCompt.MusicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        }

        if (dataCompt.SoundVolumeSlider != null)
        {
            dataCompt.SoundVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        sliderCallbacksBound = false;
    }

    private void ShowAudioTab()
    {
        if (dataCompt == null)
        {
            return;
        }

        dataCompt.AudioToggle?.SetIsOnWithoutNotify(true);
        dataCompt.OtherToggle?.SetIsOnWithoutNotify(false);

        if (dataCompt.AudioSettingTransform != null)
        {
            dataCompt.AudioSettingTransform.gameObject.SetActive(true);
        }

        if (dataCompt.OtherSettingTransform != null)
        {
            dataCompt.OtherSettingTransform.gameObject.SetActive(false);
        }
    }

    private void ShowOtherTab()
    {
        if (dataCompt == null)
        {
            return;
        }

        dataCompt.AudioToggle?.SetIsOnWithoutNotify(false);
        dataCompt.OtherToggle?.SetIsOnWithoutNotify(true);

        if (dataCompt.AudioSettingTransform != null)
        {
            dataCompt.AudioSettingTransform.gameObject.SetActive(false);
        }

        if (dataCompt.OtherSettingTransform != null)
        {
            dataCompt.OtherSettingTransform.gameObject.SetActive(true);
        }
    }

    private void RefreshAudioSliders()
    {
        if (audioModel == null)
        {
            audioModel = this.GetModel<AudioModel>();
        }

        if (audioModel == null || dataCompt == null)
        {
            return;
        }

        dataCompt.AudioVolumeSlider?.SetValueWithoutNotify(audioModel.MasterVolume);
        dataCompt.MusicVolumeSlider?.SetValueWithoutNotify(audioModel.BgmVolume);
        dataCompt.SoundVolumeSlider?.SetValueWithoutNotify(audioModel.SfxVolume);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (audioSystem == null)
        {
            audioSystem = this.GetSystem<AudioSystem>();
        }

        audioSystem?.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (audioSystem == null)
        {
            audioSystem = this.GetSystem<AudioSystem>();
        }

        audioSystem?.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (audioSystem == null)
        {
            audioSystem = this.GetSystem<AudioSystem>();
        }

        audioSystem?.SetSfxVolume(value);
    }
}
