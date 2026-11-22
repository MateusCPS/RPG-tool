using System;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    const string VolumeKey = "musicVolume";

    [SerializeField] AudioSource sourceA;
    [SerializeField] AudioSource sourceB;
    [SerializeField] float fadeSeconds = 2f;
    [SerializeField] float defaultVolume = 0.6f;

    public event Action TrackFinished;

    AudioSource active;
    AudioSource fading;
    float activeLevel;
    float fadingLevel;
    float volume;
    bool watching;
    bool started;

    public float Volume => volume;
    public float Position => active.clip != null ? active.time : 0f;

    void Awake()
    {
        active = sourceA;
        fading = sourceB;
        volume = PlayerPrefs.GetFloat(VolumeKey, defaultVolume);
        ApplyVolumes();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, volume);
        ApplyVolumes();
    }

    public void Play(AudioClip clip, float offset, bool loop)
    {
        if (clip == null)
        {
            Stop();
            return;
        }

        Swap();
        active.clip = clip;
        active.loop = loop;
        active.Play();
        active.time = Mathf.Clamp(offset, 0f, Mathf.Max(0f, clip.length - 0.1f));
        watching = !loop;
        started = false;
    }

    public void SetLoop(bool loop)
    {
        active.loop = loop;
        watching = !loop && active.clip != null;
    }

    public void Stop()
    {
        Swap();
        active.clip = null;
        watching = false;
    }

    void Swap()
    {
        fading.Stop();
        AudioSource previous = active;
        active = fading;
        fading = previous;
        fadingLevel = activeLevel;
        activeLevel = 0f;
        ApplyVolumes();
    }

    void Update()
    {
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
        activeLevel = Mathf.MoveTowards(activeLevel, active.clip != null ? 1f : 0f, step);
        fadingLevel = Mathf.MoveTowards(fadingLevel, 0f, step);
        if (fadingLevel <= 0f && fading.isPlaying) fading.Stop();
        ApplyVolumes();

        if (active.isPlaying) started = true;
        else if (watching && started)
        {
            watching = false;
            TrackFinished?.Invoke();
        }
    }

    void ApplyVolumes()
    {
        active.volume = activeLevel * volume;
        fading.volume = fadingLevel * volume;
    }
}
