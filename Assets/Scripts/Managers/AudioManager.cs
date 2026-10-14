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
        Credits,
        Level1,
        Level2BeforeCable,
        Level2AfterCable,
        Level3BeforePiano,
        Level3AfterPiano
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

    [Tooltip("Final boss music used in Hardcore mode. Falls back to Boss Music if empty.")]
    [SerializeField]
    private AudioClip bossHardcoreMusic;

    [SerializeField]
    private AudioClip creditsMusic;

    [Header("Level Music")]

    [Tooltip("Music used throughout Level 1 until the player leaves the case.")]
    [SerializeField]
    private AudioClip level1Music;

    [Tooltip("Music used in Level 2 from spawn until the Cable Cutters are picked up.")]
    [SerializeField]
    private AudioClip level2BeforeCableMusic;

    [Tooltip("Music used in Level 2 after the Cable Cutters are picked up, until the cable is cut.")]
    [SerializeField]
    private AudioClip level2AfterCableMusic;

    [Tooltip("Music used in Level 3 from spawn until the piano is opened.")]
    [SerializeField]
    private AudioClip level3BeforePianoMusic;

    [Tooltip("Music used in Level 3 after the piano is opened (Normal mode), until the boss is defeated.")]
    [SerializeField]
    private AudioClip level3AfterPianoMusic;

    [Tooltip("Level 3 post-piano / boss music used in Hardcore mode. Falls back to the Normal clip if empty.")]
    [SerializeField]
    private AudioClip level3AfterPianoHardcoreMusic;


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
    private bool musicPaused = false;


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

        // Load the master volume before any source volumes are applied.
        masterVolume =
            PlayerPrefs.GetFloat(
                "Settings.MasterVolume",
                masterVolume);

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
        musicPaused = false;

        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
            musicTransition = null;
        }

        // The main-menu cue should be audible immediately. The previous
        // crossfade could leave the music source fading from zero volume,
        // making the menu feel like it had a delayed audio start.
        if (musicType == MusicType.MainMenu)
        {
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
            return;
        }

        musicTransition = StartCoroutine(
            CrossfadeMusic(newClip)
        );
    }


    /// <summary>
    /// Plays a clip through the persistent music source. This is useful for
    /// scene-local music that is already assigned to an interactable while
    /// still keeping pause/resume and looping under AudioManager's control.
    /// </summary>
    public void PlayCustomMusic(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (musicSource != null &&
            musicSource.clip == clip &&
            musicSource.isPlaying)
        {
            return;
        }

        currentMusic = MusicType.None;
        musicPaused = false;

        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
        }

        musicTransition = StartCoroutine(
            CrossfadeMusic(clip)
        );
    }


    public bool IsPlayingClip(AudioClip clip)
    {
        return clip != null &&
               musicSource != null &&
               musicSource.clip == clip &&
               musicSource.isPlaying;
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
        musicPaused = false;
    }


    /// <summary>Immediately stops the current music without a fade.</summary>
    public void StopMusicImmediate()
    {
        if (musicTransition != null)
        {
            StopCoroutine(musicTransition);
            musicTransition = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
        }

        currentMusic = MusicType.None;
        musicPaused = false;
    }


    /// <summary>Pauses the music source without resetting its position.</summary>
    public void PauseMusic()
    {
        if (musicSource == null || !musicSource.isPlaying)
        {
            return;
        }

        musicSource.Pause();
        musicPaused = true;
    }


    /// <summary>Resumes the music from the exact position where it was paused.</summary>
    public void ResumeMusic()
    {
        if (musicSource == null || !musicPaused)
        {
            return;
        }

        musicSource.UnPause();
        musicPaused = false;
    }


    public bool IsMusicPaused()
    {
        return musicPaused;
    }


    /// <summary>Starts the correct music for a numbered level/case.</summary>
    public void PlayLevelMusic(int caseID)
    {
        switch (caseID)
        {
            case 1:
                PlayMusic(MusicType.Level1);
                break;

            case 2:
                PlayMusic(MusicType.Level2BeforeCable);
                break;

            case 3:
                PlayMusic(MusicType.Level3BeforePiano);
                break;

            default:
                Debug.LogWarning(
                    "[Audio] No level music mapping exists for Case ID " + caseID
                );
                break;
        }
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
                return ResolveDifficultyClip(
                    bossMusic,
                    bossHardcoreMusic
                );

            case MusicType.Credits:
                return creditsMusic;

            case MusicType.Level1:
                return level1Music;

            case MusicType.Level2BeforeCable:
                return level2BeforeCableMusic;

            case MusicType.Level2AfterCable:
                return level2AfterCableMusic;

            case MusicType.Level3BeforePiano:
                return level3BeforePianoMusic;

            case MusicType.Level3AfterPiano:
                return ResolveDifficultyClip(
                    level3AfterPianoMusic,
                    level3AfterPianoHardcoreMusic
                );

            default:
                return null;
        }
    }


    private static AudioClip ResolveDifficultyClip(
        AudioClip normalClip,
        AudioClip hardcoreClip)
    {
        bool hardcore =
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore();

        if (hardcore && hardcoreClip != null)
        {
            return hardcoreClip;
        }

        return normalClip;
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

        PlayerPrefs.SetFloat(
            "Settings.MasterVolume",
            masterVolume);

        PlayerPrefs.Save();

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