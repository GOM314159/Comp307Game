using UnityEngine;

public class Level3PianoInteractable : WorldInteractable
{
    [Header("Piano Models")]

    [Tooltip("The CLOSED piano model.")]
    [SerializeField]
    private GameObject closedPiano;

    [Tooltip("The OPEN piano model.")]
    [SerializeField]
    private GameObject openPiano;

    [Header("Interaction Volume")]

    [Tooltip(
        "Invisible BoxCollider used for looking/interacting. " +
        "Place and size this cube around the piano in the Scene view. " +
        "Create a child named 'Interaction Collider' if unassigned."
    )]
    [SerializeField]
    private BoxCollider interactionCollider;

    [SerializeField]
    private bool automaticallyFindInteractionCollider = true;

    [SerializeField]
    private bool showColliderDebug = true;

    [Header("Level 3 Music")]

    [Tooltip(
        "Optional fallback clip for the music that already plays when " +
        "Level 3 starts. If assigned, it is routed through AudioManager " +
        "so pause/resume works correctly."
    )]
    [SerializeField]
    private AudioClip ambientMusic;

    [Tooltip(
        "If true, the assigned ambientMusic clip is started when this piano " +
        "is loaded. CaseManager can also start Level3BeforePiano music."
    )]
    [SerializeField]
    private bool playAmbientOnStart = true;

    [Tooltip(
        "Level 3 boss to watch. When this boss dies, the post-piano music " +
        "is stopped. Leave empty to auto-detect a Level3Boss in the scene. " +
        "If no boss is found, the music simply keeps playing."
    )]
    [SerializeField]
    private Level3Boss bossToWatchFor;

    [SerializeField]
    private bool automaticallyFindBoss = true;

    [Header("Gun Unlock")]

    [Tooltip("The Level 3 gun world object. It stays hidden until the piano is opened.")]
    [SerializeField]
    private GameObject gunObject;

    [Tooltip("If true, the script will automatically find a GunInteractable if gunObject is not assigned.")]
    [SerializeField]
    private bool automaticallyFindGun = true;

    [Header("Painting Changes")]

    [SerializeField]
    private Level3Painting[] paintings;

    [SerializeField]
    private bool findPaintingsAutomatically = true;

    [Header("Interaction")]

    [SerializeField]
    private string pianoPrompt = "Open Piano";

    private bool pianoOpened;
    private bool bossEventSubscribed;

    protected override void Awake()
    {
        base.Awake();

        ResolveInteractionCollider();

        pianoOpened = false;

        ResolveGunReference();
        SetGunVisible(false);

        SetPianoModelsClosed();
        EnableInteractionCollider();
    }

    private void Start()
    {
        ResolveBossReference();
        SubscribeToBoss();

        if (playAmbientOnStart && !pianoOpened)
        {
            StartLevel3StartingMusic();
        }
    }

    private void OnEnable()
    {
        SubscribeToBoss();
    }

    private void OnDisable()
    {
        UnsubscribeFromBoss();
    }

    private void ResolveInteractionCollider()
    {
        if (interactionCollider == null &&
            automaticallyFindInteractionCollider)
        {
            Transform child =
                transform.Find("Interaction Collider");

            if (child != null)
            {
                interactionCollider =
                    child.GetComponent<BoxCollider>();
            }
        }

        if (interactionCollider == null)
        {
            interactionCollider =
                GetComponent<BoxCollider>();
        }

        if (interactionCollider == null)
        {
            Debug.LogError(
                "[Level3PianoInteractable] No interaction BoxCollider " +
                "found on " + gameObject.name + ". Add a child named " +
                "'Interaction Collider' with a BoxCollider, or assign one " +
                "in the Inspector."
            );
            return;
        }

        PrepareInteractionCollider();
    }

    private void PrepareInteractionCollider()
    {
        if (interactionCollider == null)
        {
            return;
        }

        interactionCollider.isTrigger = true;
        interactionCollider.enabled = true;

        int interactionLayer =
            LayerMask.NameToLayer("Interaction");

        if (interactionLayer >= 0)
        {
            interactionCollider.gameObject.layer =
                interactionLayer;
        }
    }

    private void ResolveGunReference()
    {
        if (gunObject != null || !automaticallyFindGun)
        {
            return;
        }

        GunInteractable gun = FindFirstObjectByType<GunInteractable>(
            FindObjectsInactive.Include
        );

        if (gun != null)
        {
            gunObject = gun.gameObject;
        }
    }

    private void SetGunVisible(bool visible)
    {
        if (gunObject != null)
        {
            gunObject.SetActive(visible);
        }
    }

    private void ResolveBossReference()
    {
        if (bossToWatchFor != null || !automaticallyFindBoss)
        {
            return;
        }

        bossToWatchFor =
            FindFirstObjectByType<Level3Boss>(
                FindObjectsInactive.Include
            );

        if (bossToWatchFor == null)
        {
            Debug.Log(
                "[Level3PianoInteractable] No Level3Boss was found. " +
                "Post-piano music will continue indefinitely.",
                this
            );
        }
    }

    private void SubscribeToBoss()
    {
        if (bossEventSubscribed || bossToWatchFor == null)
        {
            return;
        }

        bossToWatchFor.OnBossDefeated += HandleBossDefeated;
        bossEventSubscribed = true;
    }

    private void UnsubscribeFromBoss()
    {
        if (!bossEventSubscribed || bossToWatchFor == null)
        {
            return;
        }

        bossToWatchFor.OnBossDefeated -= HandleBossDefeated;
        bossEventSubscribed = false;
    }

    private void HandleBossDefeated()
    {
        if (!pianoOpened)
        {
            return;
        }

        // The post-piano music is allowed to continue until the boss dies.
        // If no boss was found/assigned, this callback simply never occurs.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }
    }

    private void StartLevel3StartingMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "[Level3PianoInteractable] AudioManager.Instance is missing.",
                this
            );
            return;
        }

        // If the existing Piano.mp3 assignment is present, use it directly.
        // Otherwise CaseManager's Level3BeforePiano assignment is used.
        if (ambientMusic != null)
        {
            if (!AudioManager.Instance.IsPlayingClip(ambientMusic))
            {
                AudioManager.Instance.PlayCustomMusic(ambientMusic);
            }
        }
        else
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Level3BeforePiano
            );
        }
    }

    public override void Interact()
    {
        if (pianoOpened || !CanInteract())
        {
            return;
        }

        base.Interact();
        OpenPiano();
    }

    public override string GetInteractionPrompt()
    {
        return pianoPrompt;
    }

    private void OpenPiano()
    {
        if (pianoOpened)
        {
            return;
        }

        pianoOpened = true;

        // Piano interaction changes the music immediately.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Level3AfterPiano
            );
        }

        ResolveBossReference();
        SubscribeToBoss();

        SetPianoModelsOpen();
        SetGunVisible(true);
        DisableInteractionCollider();
        TriggerPaintings();
    }

    private void SetPianoModelsClosed()
    {
        if (closedPiano != null)
        {
            closedPiano.SetActive(true);
        }

        if (openPiano != null)
        {
            openPiano.SetActive(false);
        }
    }

    private void SetPianoModelsOpen()
    {
        if (closedPiano != null)
        {
            closedPiano.SetActive(false);
        }

        if (openPiano != null)
        {
            openPiano.SetActive(true);
        }
    }

    private void EnableInteractionCollider()
    {
        if (interactionCollider != null)
        {
            interactionCollider.enabled = true;
        }
    }

    private void DisableInteractionCollider()
    {
        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }
    }

    private void TriggerPaintings()
    {
        Level3Painting[] paintingsToChange = paintings;

        if ((paintingsToChange == null ||
             paintingsToChange.Length == 0) &&
            findPaintingsAutomatically)
        {
            paintingsToChange =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }

        if (paintingsToChange == null ||
            paintingsToChange.Length == 0)
        {
            Debug.LogWarning(
                "[Level3PianoInteractable] No Level3Painting components were found.",
                this
            );
            return;
        }

        for (int i = 0; i < paintingsToChange.Length; i++)
        {
            if (paintingsToChange[i] != null)
            {
                paintingsToChange[i].ChangeToSecondImage();
            }
        }
    }

    public override void ResetInteraction()
    {
        base.ResetInteraction();

        pianoOpened = false;

        ResolveGunReference();
        SetGunVisible(false);

        SetPianoModelsClosed();
        EnableInteractionCollider();
        ResetPaintings();

        if (playAmbientOnStart)
        {
            StartLevel3StartingMusic();
        }
    }

    private void ResetPaintings()
    {
        Level3Painting[] paintingsToReset = paintings;

        if ((paintingsToReset == null ||
             paintingsToReset.Length == 0) &&
            findPaintingsAutomatically)
        {
            paintingsToReset =
                FindObjectsByType<Level3Painting>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );
        }

        if (paintingsToReset == null)
        {
            return;
        }

        for (int i = 0; i < paintingsToReset.Length; i++)
        {
            if (paintingsToReset[i] != null)
            {
                paintingsToReset[i].ResetPainting();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showColliderDebug || interactionCollider == null)
        {
            return;
        }

        Gizmos.matrix = interactionCollider.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            interactionCollider.center,
            interactionCollider.size
        );
    }
#endif
}
