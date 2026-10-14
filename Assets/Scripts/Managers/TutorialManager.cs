using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// First-day tutorial. Starts after the Day 1 day-screen, fades the screen
/// from black, then teaches movement, look, movement actions, inventory,
/// toolbar cycling and attack in that order.
///
/// Put this on a persistent GamePlayTest/Persistent UI object.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Audio")]
    [Tooltip("0 = movement, 1 = look, 2 = crouch/jump/sprint, 3 = inventory, 4 = toolbar, 5 = attack, 6 = final tutorial line.")]
    [SerializeField] private AudioClip[] tutorialAudio = new AudioClip[7];

    [SerializeField, Min(0f)] private float fadeInDuration = 2f;
    [SerializeField, Min(0f)] private float delayAfterAction = 4f;
    [SerializeField] private float fadeCanvasAlpha = 1f;
    [SerializeField] private bool debugTutorial = true;

    [Header("Tutorial Barrier")]
    [Tooltip("A normal non-trigger BoxCollider that physically blocks the player. It is enabled during the tutorial and disabled when the tutorial is complete.")]
    [SerializeField] private GameObject tutorialBarrier;

    [Header("Fade")]
    [SerializeField] private int canvasSortingOrder = 8000;

    private PlayerControls controls;
    private Coroutine tutorialRoutine;
    private Image fadeImage;
    private Canvas fadeCanvas;

    private bool tutorialRunning;
    private int lastObservedDay = -1;
    private bool tutorialCompleted;
    private bool attackDetected;
    private bool crouchDetected;
    private bool jumpDetected;
    private bool sprintDetected;
    private bool inventoryInputDetected;
    private bool audioPaused;
    public void SetTutorialAudioPaused(bool paused)
    {
        audioPaused = paused;
        AudioSource source = GetComponent<AudioSource>();
        if (source == null) return;
        if (paused) source.Pause();
        else source.UnPause();
    }

    private IEnumerator WaitAfterAction()
    {
        float elapsed = 0f;
        while (tutorialRunning && elapsed < delayAfterAction)
        {
            if (!audioPaused)
                elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool moveLeft;
    private bool moveRight;
    private bool moveForward;
    private bool moveBackward;
    private bool lookDetected;

    private float previousScrollY;

    public static bool IsInteractionLocked =>
        Instance != null && Instance.tutorialRunning;

    public bool IsTutorialRunning => tutorialRunning;
    public bool IsTutorialCompleted => tutorialCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        lastObservedDay = GameStateManager.Instance != null ? GameStateManager.Instance.GetCurrentDay() : 1;
        controls = new PlayerControls();

        controls.Player.Croutch.performed += OnCrouch;
        controls.Player.Jump.performed += OnJump;
        controls.Player.Sprint.performed += OnSprint;
        controls.Player.Attack.performed += OnAttack;
        controls.Player.Inventory.performed += OnInventory;

        BuildFade();
        SetFadeAlpha(0f);

        ResolveTutorialBarrier();
        SetBarrier(false);
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f12Key.wasPressedThisFrame &&
            tutorialRunning)
        {
            SkipTutorial();
            return;
        }

        ResolveTutorialBarrier();
        int currentDay = GameStateManager.Instance != null
            ? GameStateManager.Instance.GetCurrentDay()
            : 1;

        if (currentDay != lastObservedDay)
        {
            int previousDay = lastObservedDay;
            lastObservedDay = currentDay;
            if (currentDay != 1)
            {
                // Day 1 tutorial progress is temporary. Leaving Day 1 (including
                // via a debug teleport) always clears its barrier and seals/state.
                StopTutorialForDayChange();
                if (previousDay == 1 && GameResetManager.Instance != null)
                    GameResetManager.Instance.ResetWorldForDayTransition();
            }
            else
            {
                tutorialCompleted = false;
                SetBarrier(false);
            }
        }

        if (currentDay != 1)
        {
            if (tutorialRunning) StopTutorialForDayChange();
            else SetBarrier(false);
            return;
        }

        if (!tutorialRunning)
            return;

        CheckMovementInput();
        CheckLookInput();
        CheckToolbarInput();
    }

    private void CheckMovementInput()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();

        const float threshold = 0.5f;

        if (move.x < -threshold) moveLeft = true;
        if (move.x > threshold) moveRight = true;
        if (move.y > threshold) moveForward = true;
        if (move.y < -threshold) moveBackward = true;
    }

    private void CheckLookInput()
    {
        Vector2 look = controls.Player.Look.ReadValue<Vector2>();

        if (look.sqrMagnitude > 0.0025f)
            lookDetected = true;
    }

    private void CheckToolbarInput()
    {
        if (Mouse.current != null)
        {
            float scrollY = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scrollY) > 0.01f)
                previousScrollY = scrollY;

            if (Mathf.Abs(previousScrollY) > 0.01f)
                // A scroll event is enough; clear it so one wheel movement
                // cannot be counted repeatedly.
                previousScrollY = 0f;
        }

        Gamepad pad = Gamepad.current;

        if (pad != null &&
            (pad.leftShoulder.wasPressedThisFrame ||
             pad.rightShoulder.wasPressedThisFrame))
        {
            GameInputDevice.SetLastUsedDevice(pad);
            toolbarInputDetected = true;
        }
    }

    private bool toolbarInputDetected;

    private void OnCrouch(InputAction.CallbackContext context)
    {
        if (tutorialRunning)
            crouchDetected = true;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (tutorialRunning)
            jumpDetected = true;
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (tutorialRunning)
            sprintDetected = true;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (tutorialRunning)
            attackDetected = true;
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        if (tutorialRunning)
            inventoryInputDetected = true;
    }

    public void BeginDay1Tutorial()
    {
        if (tutorialCompleted || tutorialRunning)
            return;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.GetCurrentDay() != 1)
            return;

        if (tutorialRoutine != null)
            StopCoroutine(tutorialRoutine);

        tutorialRoutine = StartCoroutine(RunTutorial());
    }

    private IEnumerator RunTutorial()
    {
        tutorialRunning = true;
        tutorialCompleted = false;

        SetBarrier(true);
        HideGameplayCursor();

        SetFadeAlpha(1f);

        if (fadeInDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                if (!tutorialRunning)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                SetFadeAlpha(Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
        }

        SetFadeAlpha(0f);

        // ---------------------------------------------------------
        // 1. MOVEMENT
        // ---------------------------------------------------------
        ResetMovementChecks();
        yield return PlayAudioAndWait(0);

        while (tutorialRunning &&
               !(moveLeft && moveRight && moveForward && moveBackward))
        {
            yield return null;
        }

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 2. LOOK
        // ---------------------------------------------------------
        lookDetected = false;
        yield return PlayAudioAndWait(1);

        while (tutorialRunning && !lookDetected)
            yield return null;

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 3. CROUCH / JUMP / SPRINT
        // ---------------------------------------------------------
        crouchDetected = false;
        jumpDetected = false;
        sprintDetected = false;

        yield return PlayAudioAndWait(2);

        while (tutorialRunning &&
               !(crouchDetected && jumpDetected && sprintDetected))
        {
            yield return null;
        }

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 4. INVENTORY
        // ---------------------------------------------------------
        inventoryInputDetected = false;
        yield return PlayAudioAndWait(3);

        while (tutorialRunning &&
               !inventoryInputDetected)
        {
            yield return null;
        }

        if (!tutorialRunning) yield break;

        // Wait until the Inventory actually opens, then require the player
        // to leave it again before continuing.
        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        while (tutorialRunning &&
               (inventory == null || !inventory.IsInventoryOpen()))
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
            yield return null;
        }

        while (tutorialRunning &&
               inventory != null &&
               inventory.IsInventoryOpen())
        {
            yield return null;
        }

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 5. TOOLBAR SLOT
        // ---------------------------------------------------------
        toolbarInputDetected = false;
        previousScrollY = 0f;

        yield return PlayAudioAndWait(4);

        while (tutorialRunning && !toolbarInputDetected)
        {
            if (Mouse.current != null &&
                Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f)
            {
                toolbarInputDetected = true;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null &&
                (pad.leftShoulder.wasPressedThisFrame ||
                 pad.rightShoulder.wasPressedThisFrame))
            {
                toolbarInputDetected = true;
            }

            yield return null;
        }

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 6. ATTACK
        // ---------------------------------------------------------
        attackDetected = false;
        yield return PlayAudioAndWait(5);

        while (tutorialRunning && !attackDetected)
            yield return null;

        if (!tutorialRunning) yield break;

        yield return WaitAfterAction();
        if (!tutorialRunning) yield break;

        // ---------------------------------------------------------
        // 7. FINAL TUTORIAL AUDIO
        // ---------------------------------------------------------
        // World interaction remains disabled and the barrier remains active
        // until this final line has completely finished.
        yield return PlayAudioAndWait(6);

        if (!tutorialRunning)
            yield break;

        CompleteTutorial();
    }

    private IEnumerator PlayAudioAndWait(int index)
    {
        if (index < 0 || index >= tutorialAudio.Length ||
            tutorialAudio[index] == null)
        {
            yield return null;
            yield break;
        }

        AudioClip clip = tutorialAudio[index];

        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.Stop();
        source.clip = clip;
        source.Play();
        if (audioPaused) source.Pause();

        if (debugTutorial)
            Debug.Log("[Tutorial] Playing audio step " + index + ": " + clip.name);

        float elapsed = 0f;

        while (tutorialRunning && elapsed < clip.length)
        {
            if (!audioPaused)
                elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        source.Stop();
    }

    private void CompleteTutorial()
    {
        tutorialRunning = false;
        tutorialCompleted = true;

        SetBarrier(false);
        SetFadeAlpha(0f);
        ShowGameplayCursor();

        tutorialRoutine = null;

        if (debugTutorial)
            Debug.Log("[Tutorial] Day 1 tutorial completed.");
    }

    public void SkipTutorial()
    {
        if (!tutorialRunning)
            return;

        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
            source.Stop();

        tutorialRunning = false;
        tutorialCompleted = true;

        SetBarrier(false);
        SetFadeAlpha(0f);
        ShowGameplayCursor();

        if (debugTutorial)
            Debug.Log("[Tutorial] Skipped with F12.");
    }

    public void ResetTutorial()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        tutorialRunning = false;
        tutorialCompleted = false;

        AudioSource source = GetComponent<AudioSource>();
        if (source != null)
            source.Stop();

        SetBarrier(false);
        SetFadeAlpha(0f);
    }

    private void ResetMovementChecks()
    {
        moveLeft = false;
        moveRight = false;
        moveForward = false;
        moveBackward = false;
    }

    private void StopTutorialForDayChange()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        AudioSource source = GetComponent<AudioSource>();
        if (source != null) source.Stop();

        tutorialRunning = false;
        tutorialCompleted = false;
        SetBarrier(false);
        SetFadeAlpha(0f);
    }

    private void ResolveTutorialBarrier()
    {
        if (tutorialBarrier != null) return;

        // Serialized scene references can become Unity-null after a scene
        // transition. Rebind by name so the persistent manager can always
        // release the current scene's tutorial barrier.
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform item in all)
        {
            if (item == null || !item.gameObject.scene.IsValid()) continue;
            if (item.name == "TutorialBarrier" || item.name == "TutorialBarriers")
            {
                tutorialBarrier = item.gameObject;
                return;
            }
        }
    }

    private void SetBarrier(bool active)
    {
        ResolveTutorialBarrier();
        if (tutorialBarrier != null)
            tutorialBarrier.SetActive(active && GameStateManager.Instance != null &&
                                     GameStateManager.Instance.GetCurrentDay() == 1);
    }

    private void HideGameplayCursor()
    {
        if (GameUICursorController.Instance != null)
            GameUICursorController.Instance.HideCursorForGameplay();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ShowGameplayCursor()
    {
        if (GameUICursorController.Instance != null)
            GameUICursorController.Instance.HideCursorForGameplay();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void BuildFade()
    {
        GameObject canvasObject = new GameObject("TutorialFadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = canvasSortingOrder;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("TutorialBlackFade");
        imageObject.transform.SetParent(canvasObject.transform, false);

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha * fadeCanvasAlpha);
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0.01f;
    }
}
