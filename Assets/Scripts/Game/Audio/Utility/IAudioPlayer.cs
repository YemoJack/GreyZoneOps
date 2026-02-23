using QFramework;
using UnityEngine;

public interface IAudioPlayer : IUtility
{
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSfxVolume(float volume);
    void SetMuted(bool muted);

    void PlayMusic(AudioClip clip, bool loop, float clipVolume = 1f);
    void StopMusic();
    void PauseMusic();
    void ResumeMusic();

    void PlaySfx2D(AudioClip clip, float clipVolume = 1f, float pitch = 1f);
    void PlaySfx3D(
        AudioClip clip,
        Vector3 position,
        float clipVolume = 1f,
        float pitch = 1f,
        float spatialBlend = 1f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
        float maxDistance = -1f);
}
