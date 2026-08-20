using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }


    // =========================================================
    // AUDIO TYPES
    // =========================================================

    public enum MusicType
    {
        None,
        MainMenu,
        Lobby,
        Theater,
        Case,
        Boss,
        Credits
    }


    public enum AmbienceType
    {
        None,
        Lobby,
        Theater,
        Case,
        Boss,
        Rain,
        Night
    }


    // =========================================================
    // AUDIO SOURCES
    // =========================================================

    [Header("Audio Sources")]

    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private AudioSource ambienceSource;

    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioSource voiceSource;


    // =========================================================
    // MUSIC
    // =========================================================

    [Header("Music")]

    [SerializeField]
    private AudioClip mainMenuMusic;

    [SerializeField]
    private AudioClip lobbyMusic;

    [SerializeField]
    private AudioClip theaterMusic;

    [SerializeField]
    private AudioClip caseMusic;

    [SerializeField]
    private AudioClip bossMusic;

    [SerializeField]
    private AudioClip creditsMusic;


    // =========================================================
    // AMBIENCE
    // =========================================================

    [Header("Ambience")]

    [SerializeField]
    private AudioClip lobbyAmbience;

    [SerializeField]
    private AudioClip theaterAmbience;

    [SerializeField]
    private AudioClip caseAmbience;

    [SerializeField]
    private AudioClip bossAmbience;

    [SerializeField]
    private AudioClip rainAmbience;

    [SerializeField]
    private AudioClip nightAmbience;


    // =========================================================
    // VOLUME
    // =========================================================

    [Header("Volume")]

    [Range(0f, 1f)]
    [SerializeField]
    private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float musicVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField]
    private float ambienceVolume = 0.7f;

    [Range(0f, 1f)]
    [SerializeField]
    private float sfxVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float voiceVolume = 1f;


    // =========================================================
    // TRANSITIONS
    // =========================================================

    [Header("Transitions")]

    [SerializeField]
    private float musicFadeDuration = 1.5f;

    [SerializeField]
    private float ambienceFadeDuration = 1.5f;


    // =========================================================
    // STATE
    // =========================================================

    private MusicType currentMusic = MusicType.None;
    private AmbienceType currentAmbience = AmbienceType.None;

    private Coroutine musicTransition;
    private Coroutine ambienceTransition;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
    }


    private void Start()
    {
        ApplyVolumes();
    }


    // =========================================================
    // AUDIO SOURCE SETUP
    // =========================================================

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = CreateAudioSource(
                "Music Source"
            );

            musicSource.loop = true;
        }

        if (ambienceSource == null)
        {
            ambienceSource = CreateAudioSource(
                "Ambience Source"
            );

            ambienceSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = CreateAudioSource(
                "SFX Source"
            );

            sfxSource.loop = false;
        }

        if (voiceSource == null)
        {
            voiceSource = CreateAudioSource(
                "Voice Source"
            );

            voiceSource.loop = false;
        }
    }


    private AudioSource CreateAudioSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(
            sourceName
        );

        sourceObject.transform.SetParent(
            transform
        );

        AudioSource source =
            sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;

        return source;
    }


    // =========================================================
    // MUSIC
    // =========================================================

    public void PlayMusic(MusicType musicType)
    {
        if (currentMusic == musicType &&
            musicSource.isPlaying)
        {
            return;
        }

        AudioClip newClip = GetMusicClip(musicType);

        if (newClip == null)
        {
            Debug.LogWarning(
                "[Audio] No music assigned for: "
                + musicType
            );

            return;
        }

        currentMusic = musicType;

        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
        }

        musicTransition = StartCoroutine(
            CrossfadeMusic(newClip)
        );
    }


    public void StopMusic()
    {
        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
        }

        musicTransition = StartCoroutine(
            FadeOutMusic()
        );

        currentMusic = MusicType.None;
    }


    private IEnumerator CrossfadeMusic(
        AudioClip newClip)
    {
        float startVolume = musicSource.volume;

        while (musicSource.volume > 0.01f)
        {
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                0f,
                Time.deltaTime /
                musicFadeDuration
            );

            yield return null;
        }

        musicSource.Stop();

        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        float targetVolume =
            musicVolume * masterVolume;

        while (musicSource.volume < targetVolume)
        {
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                targetVolume,
                Time.deltaTime /
                musicFadeDuration
            );

            yield return null;
        }

        musicSource.volume = targetVolume;
    }


    private IEnumerator FadeOutMusic()
    {
        while (musicSource.volume > 0.01f)
        {
            musicSource.volume = Mathf.MoveTowards(
                musicSource.volume,
                0f,
                Time.deltaTime /
                musicFadeDuration
            );

            yield return null;
        }

        musicSource.Stop();

        musicSource.volume =
            musicVolume * masterVolume;
    }


    private AudioClip GetMusicClip(
        MusicType musicType)
    {
        switch (musicType)
        {
            case MusicType.MainMenu:
                return mainMenuMusic;

            case MusicType.Lobby:
                return lobbyMusic;

            case MusicType.Theater:
                return theaterMusic;

            case MusicType.Case:
                return caseMusic;

            case MusicType.Boss:
                return bossMusic;

            case MusicType.Credits:
                return creditsMusic;

            default:
                return null;
        }
    }


    // =========================================================
    // AMBIENCE
    // =========================================================

    public void PlayAmbience(
        AmbienceType ambienceType)
    {
        if (currentAmbience == ambienceType &&
            ambienceSource.isPlaying)
        {
            return;
        }

        AudioClip newClip =
            GetAmbienceClip(ambienceType);

        if (newClip == null)
        {
            Debug.LogWarning(
                "[Audio] No ambience assigned for: "
                + ambienceType
            );

            return;
        }

        currentAmbience = ambienceType;

        if (ambienceTransition != null)
        {
            StopCoroutine(
                ambienceTransition
            );
        }

        ambienceTransition = StartCoroutine(
            CrossfadeAmbience(newClip)
        );
    }


    public void StopAmbience()
    {
        if (ambienceTransition != null)
        {
            StopCoroutine(
                ambienceTransition
            );
        }

        ambienceTransition = StartCoroutine(
            FadeOutAmbience()
        );

        currentAmbience = AmbienceType.None;
    }


    private IEnumerator CrossfadeAmbience(
        AudioClip newClip)
    {
        while (ambienceSource.volume > 0.01f)
        {
            ambienceSource.volume = Mathf.MoveTowards(
                ambienceSource.volume,
                0f,
                Time.deltaTime /
                ambienceFadeDuration
            );

            yield return null;
        }

        ambienceSource.Stop();

        ambienceSource.clip = newClip;
        ambienceSource.loop = true;
        ambienceSource.Play();

        float targetVolume =
            ambienceVolume * masterVolume;

        while (ambienceSource.volume < targetVolume)
        {
            ambienceSource.volume = Mathf.MoveTowards(
                ambienceSource.volume,
                targetVolume,
                Time.deltaTime /
                ambienceFadeDuration
            );

            yield return null;
        }

        ambienceSource.volume = targetVolume;
    }


    private IEnumerator FadeOutAmbience()
    {
        while (ambienceSource.volume > 0.01f)
        {
            ambienceSource.volume = Mathf.MoveTowards(
                ambienceSource.volume,
                0f,
                Time.deltaTime /
                ambienceFadeDuration
            );

            yield return null;
        }

        ambienceSource.Stop();

        ambienceSource.volume =
            ambienceVolume * masterVolume;
    }


    private AudioClip GetAmbienceClip(
        AmbienceType ambienceType)
    {
        switch (ambienceType)
        {
            case AmbienceType.Lobby:
                return lobbyAmbience;

            case AmbienceType.Theater:
                return theaterAmbience;

            case AmbienceType.Case:
                return caseAmbience;

            case AmbienceType.Boss:
                return bossAmbience;

            case AmbienceType.Rain:
                return rainAmbience;

            case AmbienceType.Night:
                return nightAmbience;

            default:
                return null;
        }
    }


    // =========================================================
    // SOUND EFFECTS
    // =========================================================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            clip,
            sfxVolume * masterVolume
        );
    }


    public void PlaySFX(
        AudioClip clip,
        float volumeMultiplier)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            clip,
            sfxVolume *
            masterVolume *
            volumeMultiplier
        );
    }


    // =========================================================
    // POSITIONAL SOUND
    // =========================================================

    public void PlayWorldSFX(
        AudioClip clip,
        Vector3 position,
        float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(
            clip,
            position,
            sfxVolume *
            masterVolume *
            volume
        );
    }


    // =========================================================
    // VOICE / DIALOGUE
    // =========================================================

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        voiceSource.PlayOneShot(
            clip,
            voiceVolume * masterVolume
        );
    }


    public void StopVoice()
    {
        voiceSource.Stop();
    }


    // =========================================================
    // VOLUME
    // =========================================================

    public void SetMasterVolume(float volume)
    {
        masterVolume =
            Mathf.Clamp01(volume);

        ApplyVolumes();
    }


    public void SetMusicVolume(float volume)
    {
        musicVolume =
            Mathf.Clamp01(volume);

        ApplyVolumes();
    }


    public void SetAmbienceVolume(float volume)
    {
        ambienceVolume =
            Mathf.Clamp01(volume);

        ApplyVolumes();
    }


    public void SetSFXVolume(float volume)
    {
        sfxVolume =
            Mathf.Clamp01(volume);
    }


    public void SetVoiceVolume(float volume)
    {
        voiceVolume =
            Mathf.Clamp01(volume);
    }


    private void ApplyVolumes()
    {
        musicSource.volume =
            musicVolume * masterVolume;

        ambienceSource.volume =
            ambienceVolume * masterVolume;
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public float GetMasterVolume()
    {
        return masterVolume;
    }


    public float GetMusicVolume()
    {
        return musicVolume;
    }


    public float GetAmbienceVolume()
    {
        return ambienceVolume;
    }


    public float GetSFXVolume()
    {
        return sfxVolume;
    }


    public float GetVoiceVolume()
    {
        return voiceVolume;
    }


    public MusicType GetCurrentMusic()
    {
        return currentMusic;
    }


    public AmbienceType GetCurrentAmbience()
    {
        return currentAmbience;
    }
}