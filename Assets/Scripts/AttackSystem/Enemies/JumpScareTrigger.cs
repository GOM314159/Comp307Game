using System.Collections;
using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    // =========================================================
    // DAY SETTINGS
    // =========================================================

    [Header("Day Settings")]

    [SerializeField, Min(1)]
    private int requiredDay = 2;


    // =========================================================
    // TRIGGER SETTINGS
    // =========================================================

    [Header("Trigger Settings")]

    [Tooltip("If enabled, this jumpscare can only happen once until the game is reset.")]
    [SerializeField]
    private bool triggerOnce = true;


    // =========================================================
    // PLAYER CAMERA
    // =========================================================

    [Header("Player Camera")]

    [Tooltip(
        "Optional. The script will automatically find the current Main Camera " +
        "if this reference becomes invalid after changing scenes."
    )]
    [SerializeField]
    private Camera playerCamera;

    [Tooltip("How far in front of the player's camera Nightmarionne appears.")]
    [SerializeField]
    private float cameraDistance = 1.5f;

    [Tooltip("Vertical offset relative to the camera.")]
    [SerializeField]
    private float cameraHeightOffset = -0.2f;


    // =========================================================
    // PAN INTO POSITION
    // =========================================================

    [Header("Pan Into Position")]

    [Tooltip("How long Nightmarionne takes to move into position.")]
    [SerializeField]
    private float panDuration = 0.2f;

    [Tooltip("Horizontal offset where Nightmarionne starts.")]
    [SerializeField]
    private float startingSideOffset = -1.5f;

    [Tooltip("Distance in front of the camera where Nightmarionne starts.")]
    [SerializeField]
    private float startingDistance = 2f;


    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Jumpscare Animation")]

    [Tooltip("Animator attached to Nightmarionne.")]
    [SerializeField]
    private Animator scareAnimator;

    [Tooltip("Trigger parameter used by the Animator.")]
    [SerializeField]
    private string animationTrigger = "JumpScar";


    // =========================================================
    // JUMPSCARE AUDIO
    // =========================================================

    [Header("Jumpscare Audio")]

    [Tooltip(
        "AudioSource used ONLY for the main jumpscare sound. " +
        "Keep its Pitch at whatever you want specifically for the jumpscare."
    )]
    [SerializeField]
    private AudioSource jumpscareAudioSource;

    [Tooltip("Sound played when the jumpscare begins.")]
    [SerializeField]
    private AudioClip jumpscareAudio;


    // =========================================================
    // JUMPSCARE DURATION
    // =========================================================

    [Header("Jumpscare Duration")]

    [Tooltip(
        "Amount of time the jumpscare remains active after Nightmarionne " +
        "has finished moving into position."
    )]
    [SerializeField, Min(0.1f)]
    private float scareDuration = 2f;


    // =========================================================
    // NIGHTMARIONNE VISUAL
    // =========================================================

    [Header("Jumpscare Visual")]

    [Tooltip("The Nightmarionne model that appears during the jumpscare.")]
    [SerializeField]
    private GameObject scareVisual;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Rotation")]

    [Tooltip("Extra rotation applied after Nightmarionne faces the player.")]
    [SerializeField]
    private Vector3 rotationOffset;


    // =========================================================
    // SCREEN OVERLAY
    // =========================================================

    [Header("Screen Overlay")]

    [Tooltip(
        "CanvasGroup controlling the transparent horror overlay. " +
        "The overlay should contain the dark vignette and red streaks."
    )]
    [SerializeField]
    private CanvasGroup jumpscareOverlay;

    [Tooltip("Name used to re-find the persistent overlay if its Inspector reference is lost.")]
    [SerializeField]
    private string jumpscareOverlayObjectName = "JumpscareOverlay";

    [Tooltip("How quickly the overlay appears.")]
    [SerializeField, Min(0f)]
    private float overlayFadeIn = 0.15f;

    [Tooltip("How quickly the overlay disappears.")]
    [SerializeField, Min(0f)]
    private float overlayFadeOut = 0.2f;

    [Tooltip("Maximum opacity of the overlay during the jumpscare.")]
    [SerializeField, Range(0f, 1f)]
    private float overlayMaxAlpha = 1f;


    // =========================================================
    // AFTER-JUMPSCARE AUDIO
    // =========================================================

    [Header("After-Jumpscare Audio")]

    [Tooltip(
        "Separate AudioSource for the thunderclap. " +
        "Set Pitch to 1 unless you intentionally want to alter the thunder."
    )]
    [SerializeField]
    private AudioSource thunderAudioSource;

    [Tooltip("Thunderclap played after the jumpscare has ended.")]
    [SerializeField]
    private AudioClip thunderclapAudio;


    [Tooltip(
        "Separate AudioSource for the evil laugh. " +
        "Set Pitch to 1 unless you intentionally want to alter the laugh."
    )]
    [SerializeField]
    private AudioSource evilLaughAudioSource;

    [Tooltip("Evil laugh played after the jumpscare. It can overlap the thunder.")]
    [SerializeField]
    private AudioClip evilLaughAudio;


    [Tooltip(
        "How long after the thunder starts before the evil laugh begins. " +
        "For example, 0.5 means the laugh starts half a second into the thunder."
    )]
    [SerializeField, Min(0f)]
    private float evilLaughDelay = 0.5f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugJumpscare = true;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool hasTriggered;
    private bool isPlaying;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // FIND ANIMATOR
        // -----------------------------------------------------

        if (scareAnimator == null && scareVisual != null)
        {
            scareAnimator =
                scareVisual.GetComponentInChildren<Animator>(true);
        }


        // -----------------------------------------------------
        // FIND JUMPSCARE AUDIO SOURCE
        // -----------------------------------------------------

        if (jumpscareAudioSource == null)
        {
            jumpscareAudioSource =
                GetComponent<AudioSource>();
        }


        // -----------------------------------------------------
        // FIND CURRENT CAMERA
        // -----------------------------------------------------

        FindPlayerCamera();


        // -----------------------------------------------------
        // NIGHTMARIONNE STARTS INVISIBLE
        // -----------------------------------------------------

        if (scareVisual != null)
        {
            scareVisual.SetActive(false);
        }


        // -----------------------------------------------------
        // OVERLAY STARTS INVISIBLE
        // -----------------------------------------------------

        ResolveJumpscareOverlay();
        if (jumpscareOverlay != null)
        {
            jumpscareOverlay.alpha = 0f;
            jumpscareOverlay.interactable = false;
            jumpscareOverlay.blocksRaycasts = false;
            jumpscareOverlay.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // FIND PLAYER CAMERA
    // =========================================================

    private void FindPlayerCamera()
    {
        // -----------------------------------------------------
        // CURRENT REFERENCE IS STILL VALID
        // -----------------------------------------------------

        if (playerCamera != null &&
            playerCamera.gameObject != null &&
            playerCamera.gameObject.activeInHierarchy)
        {
            return;
        }


        // -----------------------------------------------------
        // FIRST TRY MAIN CAMERA
        // -----------------------------------------------------

        playerCamera = Camera.main;


        if (playerCamera != null)
        {
            return;
        }


        // -----------------------------------------------------
        // FALLBACK:
        // SEARCH FOR A CAMERA TAGGED MAIN CAMERA
        // -----------------------------------------------------

        Camera[] cameras =
            FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        foreach (Camera cam in cameras)
        {
            if (cam != null &&
                cam.CompareTag("MainCamera"))
            {
                playerCamera = cam;
                return;
            }
        }
    }


    private void ResolveJumpscareOverlay()
    {
        if (jumpscareOverlay != null) return;

        if (!string.IsNullOrWhiteSpace(jumpscareOverlayObjectName))
        {
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform item in all)
            {
                if (item == null || !item.gameObject.scene.IsValid()) continue;
                if (item.name != jumpscareOverlayObjectName) continue;

                jumpscareOverlay = item.GetComponent<CanvasGroup>();
                if (jumpscareOverlay == null)
                    jumpscareOverlay = item.GetComponentInChildren<CanvasGroup>(true);
                if (jumpscareOverlay != null) return;
            }
        }
    }

    // =========================================================
    // PLAYER ENTERS TRIGGER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }


    private void OnTriggerStay(Collider other)
    {
        // A CharacterController teleported directly into the trigger can
        // miss OnTriggerEnter. TriggerStay makes normal and debug teleports
        // deterministic.
        TryTrigger(other);
    }


    private void TryTrigger(Collider other)
    {
        if (isPlaying)
            return;

        bool isPlayer =
            other.CompareTag("Player") ||
            other.transform.root.CompareTag("Player");

        if (!isPlayer)
            return;

        if (triggerOnce && hasTriggered)
            return;

        if (GameStateManager.Instance == null)
            return;

        if (GameStateManager.Instance.GetCurrentDay() != requiredDay)
            return;

        // Do not consume the one-shot trigger until the sequence has a valid camera.
        isPlaying = true;
        StartCoroutine(PlayJumpscare());
    }


    // =========================================================
    // MAIN JUMPSCARE
    // =========================================================

    private IEnumerator PlayJumpscare()
    {
        ResolveJumpscareOverlay();

        // Persistent GUI references can be stale after a scene transition.
        // Activate the overlay's parent chain before trying to fade it in.
        if (jumpscareOverlay != null)
        {
            Transform parent = jumpscareOverlay.transform;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            jumpscareOverlay.gameObject.SetActive(true);
            jumpscareOverlay.alpha = 0f;
            jumpscareOverlay.interactable = false;
            jumpscareOverlay.blocksRaycasts = false;
        }

        // =====================================================
        // FIND THE CURRENT CAMERA AGAIN
        //
        // This is important after returning from another scene.
        // =====================================================

        playerCamera = null;

        // The trigger can fire on the same frame as a scene/player camera
        // rebinding. Wait briefly for Camera.main instead of consuming the
        // one-shot trigger and leaving it permanently broken.
        const float cameraSearchTimeout = 1.5f;
        float cameraSearchElapsed = 0f;

        while (playerCamera == null &&
               cameraSearchElapsed < cameraSearchTimeout)
        {
            FindPlayerCamera();

            if (playerCamera != null)
                break;

            cameraSearchElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "[Jumpscare] Could not find the player's Main Camera.",
                this
            );

            isPlaying = false;
            hasTriggered = false;
            yield break;
        }


        // Camera is valid; this is now a real trigger.
        hasTriggered = true;

        // =====================================================
        // PLAYER STATE
        // =====================================================

        PlayerStateManager playerState =
            PlayerStateManager.Instance;


        PlayerStateManager.PlayerState previousState =
            PlayerStateManager.PlayerState.AllMovement;


        // =====================================================
        // LOCK PLAYER
        // =====================================================

        if (playerState != null)
        {
            previousState =
                playerState.GetCurrentState();

            playerState.SetState(
                PlayerStateManager.PlayerState.NoMovementNoCamera
            );
        }


        // =====================================================
        // SHOW NIGHTMARIONNE
        // =====================================================

        if (scareVisual != null)
        {
            scareVisual.SetActive(true);

            if (scareAnimator != null)
            {
                scareAnimator.enabled = true;
            }
        }


        // =====================================================
        // FINAL POSITION
        // =====================================================

        Vector3 finalPosition =
            playerCamera.transform.position
            + playerCamera.transform.forward * cameraDistance
            + playerCamera.transform.up * cameraHeightOffset;


        // =====================================================
        // START POSITION
        // =====================================================

        Vector3 startPosition =
            playerCamera.transform.position
            + playerCamera.transform.right * startingSideOffset
            + playerCamera.transform.forward * startingDistance
            + playerCamera.transform.up * cameraHeightOffset;


        // =====================================================
        // POSITION NIGHTMARIONNE
        // =====================================================

        if (scareVisual != null)
        {
            scareVisual.transform.position =
                startPosition;

            FacePlayer();
        }


        // =====================================================
        // START SCREEN OVERLAY
        // =====================================================

        if (jumpscareOverlay != null)
        {
            ActivateOverlayHierarchy();

            jumpscareOverlay.alpha = 0f;

            StartCoroutine(
                FadeOverlay(
                    0f,
                    overlayMaxAlpha,
                    overlayFadeIn
                )
            );
        }


        // =====================================================
        // PAN INTO POSITION
        // =====================================================

        float elapsed = 0f;


        while (elapsed < panDuration)
        {
            elapsed += Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / panDuration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            if (scareVisual != null)
            {
                scareVisual.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        finalPosition,
                        t
                    );


                FacePlayer();
            }


            yield return null;
        }


        // =====================================================
        // FINAL POSITION
        // =====================================================

        if (scareVisual != null)
        {
            scareVisual.transform.position =
                finalPosition;

            FacePlayer();
        }


        // =====================================================
        // ANIMATION
        // =====================================================

        if (scareAnimator != null)
        {
            scareAnimator.ResetTrigger(
                animationTrigger
            );

            scareAnimator.SetTrigger(
                animationTrigger
            );
        }
        else
        {
            Debug.LogWarning(
                "[Jumpscare] Animator is missing.",
                this
            );
        }


        // =====================================================
        // MAIN JUMPSCARE AUDIO
        // =====================================================

        if (jumpscareAudioSource != null &&
            jumpscareAudio != null)
        {
            jumpscareAudioSource.PlayOneShot(
                jumpscareAudio
            );
        }
        else
        {
            Debug.LogWarning(
                "[Jumpscare] Main jumpscare AudioSource or AudioClip is missing.",
                this
            );
        }


        if (debugJumpscare)
        {
            Debug.Log(
                "[Jumpscare] Started."
            );
        }


        // =====================================================
        // JUMPSCARE DURATION
        // =====================================================

        yield return new WaitForSecondsRealtime(
            scareDuration
        );


        // =====================================================
        // FADE OUT OVERLAY
        // =====================================================

        if (jumpscareOverlay != null)
        {
            yield return StartCoroutine(
                FadeOverlay(
                    jumpscareOverlay.alpha,
                    0f,
                    overlayFadeOut
                )
            );


            jumpscareOverlay.gameObject.SetActive(false);
        }


        // =====================================================
        // HIDE NIGHTMARIONNE
        // =====================================================

        if (scareVisual != null)
        {
            scareVisual.SetActive(false);
        }


        // =====================================================
        // RESTORE PLAYER
        // =====================================================

        RestorePlayerState(
            playerState,
            previousState
        );


        // =====================================================
        // AFTER-JUMPSCARE AUDIO
        //
        // Thunder starts.
        // Evil laugh starts after the configured delay.
        // They use DIFFERENT AudioSources and can overlap.
        // =====================================================

        StartCoroutine(
            AfterJumpscareAudioSequence()
        );


        // =====================================================
        // FINISHED
        // =====================================================

        isPlaying = false;


        if (debugJumpscare)
        {
            Debug.Log(
                "[Jumpscare] Finished. Nightmarionne disappeared."
            );
        }
    }


    // =========================================================
    // AFTER-JUMPSCARE AUDIO SEQUENCE
    // =========================================================

    private IEnumerator AfterJumpscareAudioSequence()
    {
        // -----------------------------------------------------
        // THUNDER STARTS
        // -----------------------------------------------------

        if (thunderAudioSource != null &&
            thunderclapAudio != null)
        {
            thunderAudioSource.PlayOneShot(
                thunderclapAudio
            );


            if (debugJumpscare)
            {
                Debug.Log(
                    "[Jumpscare] Thunderclap started."
                );
            }
        }
        else
        {
            if (debugJumpscare)
            {
                Debug.LogWarning(
                    "[Jumpscare] Thunder AudioSource or AudioClip is missing.",
                    this
                );
            }
        }


        // -----------------------------------------------------
        // WAIT BEFORE EVIL LAUGH
        // -----------------------------------------------------

        if (evilLaughAudioSource != null &&
            evilLaughAudio != null)
        {
            yield return new WaitForSecondsRealtime(
                evilLaughDelay
            );


            // -------------------------------------------------
            // EVIL LAUGH STARTS
            //
            // Thunder is still playing if its clip has not
            // finished yet.
            // -------------------------------------------------

            evilLaughAudioSource.PlayOneShot(
                evilLaughAudio
            );


            if (debugJumpscare)
            {
                Debug.Log(
                    "[Jumpscare] Evil laugh started."
                );
            }
        }
        else
        {
            if (debugJumpscare)
            {
                Debug.LogWarning(
                    "[Jumpscare] Evil Laugh AudioSource or AudioClip is missing.",
                    this
                );
            }
        }
    }


    private void ActivateOverlayHierarchy()
    {
        if (jumpscareOverlay == null)
            return;

        Transform current = jumpscareOverlay.transform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }


    // =========================================================
    // FACE PLAYER
    // =========================================================

    private void FacePlayer()
    {
        if (scareVisual == null ||
            playerCamera == null)
        {
            return;
        }


        Vector3 direction =
            playerCamera.transform.position
            - scareVisual.transform.position;


        if (direction.sqrMagnitude < 0.001f)
            return;


        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );


        scareVisual.transform.rotation =
            lookRotation *
            Quaternion.Euler(
                rotationOffset
            );
    }


    // =========================================================
    // OVERLAY FADE
    // =========================================================

    private IEnumerator FadeOverlay(
        float startAlpha,
        float targetAlpha,
        float duration)
    {
        if (jumpscareOverlay == null)
            yield break;


        if (duration <= 0f)
        {
            jumpscareOverlay.alpha =
                targetAlpha;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );


            jumpscareOverlay.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );


            yield return null;
        }


        jumpscareOverlay.alpha =
            targetAlpha;
    }


    // =========================================================
    // RESTORE PLAYER STATE
    // =========================================================

    private void RestorePlayerState(
        PlayerStateManager playerState,
        PlayerStateManager.PlayerState previousState)
    {
        if (playerState != null)
        {
            playerState.SetState(
                previousState
            );
        }
    }


    // =========================================================
    // RESET THIS JUMPSCARE
    // =========================================================

    public void ResetTrigger()
    {
        hasTriggered = false;
        isPlaying = false;


        // -----------------------------------------------------
        // HIDE NIGHTMARIONNE
        // -----------------------------------------------------

        if (scareVisual != null)
        {
            scareVisual.SetActive(false);
        }


        // -----------------------------------------------------
        // HIDE OVERLAY
        // -----------------------------------------------------

        if (jumpscareOverlay != null)
        {
            jumpscareOverlay.alpha = 0f;

            jumpscareOverlay.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // RESET ALL JUMPSCARES
    // =========================================================

    public static void ResetAllJumpscares()
    {
        JumpscareTrigger[] jumpscares =
            FindObjectsByType<JumpscareTrigger>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (JumpscareTrigger jumpscare
                 in jumpscares)
        {
            if (jumpscare != null)
            {
                jumpscare.ResetTrigger();
            }
        }


        Debug.Log(
            "[Jumpscare] All jumpscares reset for new game."
        );
    }
}