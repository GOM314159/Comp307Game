using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LetterUIManager : MonoBehaviour
{
    public static LetterUIManager Instance { get; private set; }

    [Header("Letter UI")]
    [Tooltip("The panel/canvas that contains the full letter.")]
    [SerializeField] private GameObject letterCanvas;

    [Tooltip("The UI Image that displays the full letter.")]
    [SerializeField] private Image letterImage;

    [Header("Audio")]
    [Tooltip("Plays when opening a letter ('SFX_Interact_Letter_Open').")]
    [SerializeField] private AudioClip letterOpenClip;

    [Tooltip("AudioSource used for letter audio. Created automatically if unassigned.")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField][Range(0f, 1f)] private float audioVolume = 1f;

    [Header("Player")]
    [SerializeField] private PlayerStateManager playerStateManager;
    [SerializeField] private PlayerInventory playerInventory;

    private bool letterOpen = false;
    private PlayerStateManager.PlayerState previousPlayerState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureAudioSource();

        if (playerStateManager == null)
        {
            playerStateManager = FindAnyObjectByType<PlayerStateManager>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (letterCanvas != null)
        {
            letterCanvas.SetActive(false);
        }

        letterOpen = false;
    }

    private void Update()
    {
        if (!letterOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseLetter();
        }
    }

    public void OpenLetter(Letter letter, Sprite selectedLetterImage)
    {
        if (letterOpen)
        {
            return;
        }

        if (playerStateManager == null)
        {
            Debug.LogError("[LetterUI] PlayerStateManager not found.");
            return;
        }

        if (letterCanvas == null)
        {
            Debug.LogError("[LetterUI] Letter Canvas has not been assigned.");
            return;
        }

        if (letterImage == null)
        {
            Debug.LogError("[LetterUI] Letter Image has not been assigned.");
            return;
        }

        PlayerStateManager.PlayerState currentState = playerStateManager.GetCurrentState();

        if (currentState == PlayerStateManager.PlayerState.Inventory)
        {
            Debug.Log("[LetterUI] Cannot open letter while inventory is open.");
            return;
        }

        previousPlayerState = currentState;

        letterImage.sprite = selectedLetterImage;
        letterImage.preserveAspect = true;

        letterCanvas.SetActive(true);
        FitLetterToScreen();

        playerStateManager.SetState(PlayerStateManager.PlayerState.Reading);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        letterOpen = true;

        PlayAudio(letterOpenClip);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }

        Debug.Log("[LetterUI] Opened letter.");
    }

    public void CloseLetter()
    {
        if (!letterOpen)
        {
            return;
        }

        if (letterCanvas != null)
        {
            letterCanvas.SetActive(false);
        }

        if (letterImage != null)
        {
            letterImage.sprite = null;
        }

        if (playerStateManager != null)
        {
            playerStateManager.SetState(previousPlayerState);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        letterOpen = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }

        Debug.Log("[LetterUI] Letter closed.");
    }

    public void ToggleLetter()
    {
        if (letterOpen)
        {
            CloseLetter();
        }
    }

    public bool IsLetterOpen()
    {
        return letterOpen;
    }

    private void FitLetterToScreen()
    {
        if (letterImage == null || letterImage.sprite == null)
        {
            return;
        }

        RectTransform imageRect = letterImage.rectTransform;
        Canvas canvas = letterImage.canvas;

        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        float screenWidth = canvasRect.rect.width;
        float screenHeight = canvasRect.rect.height;

        float horizontalPadding = screenWidth * 0.04f;
        float verticalPadding = screenHeight * 0.04f;

        float availableWidth = screenWidth - (horizontalPadding * 2f);
        float availableHeight = screenHeight - (verticalPadding * 2f);

        Sprite sprite = letterImage.sprite;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        float widthScale = availableWidth / spriteWidth;
        float heightScale = availableHeight / spriteHeight;

        float scale = Mathf.Min(widthScale, heightScale);

        imageRect.sizeDelta = new Vector2(
            spriteWidth * scale,
            spriteHeight * scale
        );

        imageRect.anchoredPosition = Vector2.zero;
        letterImage.preserveAspect = true;
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (clip == null) return;
        EnsureAudioSource();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, audioVolume);
        }
    }
}