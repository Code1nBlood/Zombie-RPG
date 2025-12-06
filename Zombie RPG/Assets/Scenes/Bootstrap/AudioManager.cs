using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Sound Library")]
    [SerializeField] private SFXLibrary sfxLibrary;
    [SerializeField] private MusicLibrary musicLibrary;

    [Header("Settings")]
    [SerializeField] private int poolSize = 15;

    private List<AudioSource> pool = new();
    private AudioSource musicSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // Пул для SFX
        for (int i = 0; i < poolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = sfxGroup;
            pool.Add(source);
        }

        // Отдельный источник для музыки
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    #region === SFX ===

    /// <summary>
    /// Воспроизвести звук по типу
    /// </summary>
    public void PlaySFX(SFXType type, float volume = 1f, float pitch = 1f)
    {
        AudioClip clip = sfxLibrary.GetClip(type);
        if (clip != null)
            PlayClip(clip, volume, pitch);
    }

    /// <summary>
    /// Воспроизвести конкретный клип
    /// </summary>
    public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetFreeSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();
    }

    /// <summary>
    /// Воспроизвести случайный звук из массива
    /// </summary>
    public void PlayRandomClip(AudioClip[] clips, float volume = 1f, float pitch = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlayClip(clips[Random.Range(0, clips.Length)], volume, pitch);
    }

    /// <summary>
    /// Воспроизвести звук с рандомным питчем (для разнообразия)
    /// </summary>
    public void PlaySFXRandomized(SFXType type, float volume = 1f, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        PlaySFX(type, volume, Random.Range(pitchMin, pitchMax));
    }

    private AudioSource GetFreeSource()
    {
        foreach (var source in pool)
            if (!source.isPlaying) return source;

        // Все заняты - останавливаем первый
        pool[0].Stop();
        return pool[0];
    }

    #endregion

    #region === Music ===

    /// <summary>
    /// Включить музыку по типу
    /// </summary>
    public void PlayMusic(MusicType type, float fadeTime = 1f)
    {
        AudioClip clip = musicLibrary.GetClip(type);
        if (clip != null)
            PlayMusicClip(clip, fadeTime);
    }

    /// <summary>
    /// Включить конкретный музыкальный клип
    /// </summary>
    public void PlayMusicClip(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToMusic(clip, fadeTime));
    }

    /// <summary>
    /// Остановить музыку
    /// </summary>
    public void StopMusic(float fadeTime = 1f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
    }

    /// <summary>
    /// Пауза/продолжение музыки
    /// </summary>
    public void PauseMusic(bool pause)
    {
        if (pause) musicSource.Pause();
        else musicSource.UnPause();
    }

    private IEnumerator FadeToMusic(AudioClip newClip, float fadeTime)
    {
        // Fade out текущую
        if (musicSource.isPlaying)
        {
            yield return FadeVolume(musicSource.volume, 0f, fadeTime * 0.5f);
            musicSource.Stop();
        }

        // Включаем новую
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in
        yield return FadeVolume(0f, 1f, fadeTime * 0.5f);
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        yield return FadeVolume(musicSource.volume, 0f, fadeTime);
        musicSource.Stop();
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled чтобы работало на паузе
            musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        musicSource.volume = to;
    }

    #endregion

    #region === Volume Control ===

    public void SetMasterVolume(float value)
    {
        mixer.SetFloat("MasterVolume", ToDecibels(value));
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        mixer.SetFloat("MusicVolume", ToDecibels(value));
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", ToDecibels(value));
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private float ToDecibels(float value)
    {
        // Избегаем Log10(0)
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }

    /// <summary>
    /// Загрузить сохранённые настройки громкости
    /// </summary>
    public void LoadVolumeSettings()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    #endregion
}