using UnityEngine;

public class UnityAudioPlayerUtility : IAudioPlayer
{
    private const string RuntimeRootName = "[AudioRuntime]";

    private GameObject runtimeRoot;
    private AudioSource musicSource;
    private AudioSource sfx2DSource;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private float musicClipVolume = 1f;
    private bool muted;

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMuted(bool muted)
    {
        this.muted = muted;
        ApplyVolumes();
    }

    public void PlayMusic(AudioClip clip, bool loop, float clipVolume = 1f)
    {
        EnsureRuntime();
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            musicSource.loop = loop;
            musicClipVolume = Mathf.Clamp01(clipVolume);
            musicSource.volume = GetMusicOutputVolume() * musicClipVolume;
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.pitch = 1f;
        musicClipVolume = Mathf.Clamp01(clipVolume);
        musicSource.volume = GetMusicOutputVolume() * musicClipVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicClipVolume = 1f;
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }

    public void PlaySfx2D(AudioClip clip, float clipVolume = 1f, float pitch = 1f)
    {
        EnsureRuntime();
        if (sfx2DSource == null || clip == null)
        {
            return;
        }

        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, GetSfxOutputVolume() * Mathf.Clamp01(clipVolume));
        sfx2DSource.pitch = 1f;
    }

    public void PlaySfx3D(
        AudioClip clip,
        Vector3 position,
        float clipVolume = 1f,
        float pitch = 1f,
        float spatialBlend = 1f,
        AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic,
        float maxDistance = -1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureRuntime();
        if (runtimeRoot == null)
        {
            return;
        }

        var go = new GameObject($"SFX_{clip.name}");
        go.transform.SetParent(runtimeRoot.transform, false);
        go.transform.position = position;

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.rolloffMode = rolloffMode;
        if (maxDistance > 0f)
        {
            source.maxDistance = maxDistance;
        }
        source.clip = clip;
        source.pitch = pitch;
        source.volume = GetSfxOutputVolume() * Mathf.Clamp01(clipVolume);
        source.Play();

        Object.Destroy(go, Mathf.Max(0.1f, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch))) + 0.2f);
    }

    private void EnsureRuntime()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = GameObject.Find(RuntimeRootName);
            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject(RuntimeRootName);
                Object.DontDestroyOnLoad(runtimeRoot);
            }
        }

        if (musicSource == null)
        {
            musicSource = GetOrAddAudioSource(runtimeRoot, "Music");
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        if (sfx2DSource == null)
        {
            sfx2DSource = GetOrAddAudioSource(runtimeRoot, "Sfx2D");
            sfx2DSource.playOnAwake = false;
            sfx2DSource.loop = false;
            sfx2DSource.spatialBlend = 0f;
        }

        ApplyVolumes();
    }

    private static AudioSource GetOrAddAudioSource(GameObject root, string childName)
    {
        var child = root.transform.Find(childName);
        GameObject target;
        if (child == null)
        {
            target = new GameObject(childName);
            target.transform.SetParent(root.transform, false);
        }
        else
        {
            target = child.gameObject;
        }

        var source = target.GetComponent<AudioSource>();
        if (source == null)
        {
            source = target.AddComponent<AudioSource>();
        }

        return source;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.mute = muted;
            if (musicSource.clip != null)
            {
                musicSource.volume = GetMusicOutputVolume() * musicClipVolume;
            }
        }

        if (sfx2DSource != null)
        {
            sfx2DSource.mute = muted;
            sfx2DSource.volume = GetSfxOutputVolume();
        }
    }

    private float GetMusicOutputVolume()
    {
        return muted ? 0f : Mathf.Clamp01(masterVolume * musicVolume);
    }

    private float GetSfxOutputVolume()
    {
        return muted ? 0f : Mathf.Clamp01(masterVolume * sfxVolume);
    }
}
