using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LetterUIManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static LetterUIManager Instance { get; private set; }


    // =========================================================
    // UI
    // =========================================================

    [Header("Letter UI")]

    [Tooltip("The panel/canvas that contains the full letter.")]
    [SerializeField]
    private GameObject letterCanvas;

    [Tooltip("The UI Image that displays the full letter.")]
    [SerializeField]
    private Image letterImage;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]

    [SerializeField]
    private PlayerStateManager playerStateManager;

    [SerializeField]
    private PlayerInventory playerInventory;


    // =========================================================
    // STATE
    // =========================================================

    private bool letterOpen = false;

    private PlayerStateManager.PlayerState previousPlayerState;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // SINGLETON
        // -----------------------------------------------------

        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        // -----------------------------------------------------
        // FIND PLAYER STATE MANAGER
        // -----------------------------------------------------

        if (playerStateManager == null)
        {
            playerStateManager =
                FindAnyObjectByType<PlayerStateManager>();
        }


        // -----------------------------------------------------
        // FIND PLAYER INVENTORY
        // -----------------------------------------------------

        if (playerInventory == null)
        {
            playerInventory =
                FindAnyObjectByType<PlayerInventory>();
        }


        // -----------------------------------------------------
        // START CLOSED
        // -----------------------------------------------------

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


        // =====================================================
        // ESCAPE CLOSES LETTER
        // =====================================================

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseLetter();
        }
    }


    // =========================================================
    // OPEN LETTER
    // =========================================================

    public void OpenLetter(
        Letter letter,
        Sprite selectedLetterImage)
    {
        if (letterOpen)
        {
            return;
        }

        if (playerStateManager == null)
        {
            Debug.LogError(
                "[LetterUI] PlayerStateManager not found."
            );

            return;
        }

        if (letterCanvas == null)
        {
            Debug.LogError(
                "[LetterUI] Letter Canvas has not been assigned."
            );

            return;
        }

        if (letterImage == null)
        {
            Debug.LogError(
                "[LetterUI] Letter Image has not been assigned."
            );

            return;
        }


        // =====================================================
        // DON'T OPEN WHILE INVENTORY IS OPEN
        // =====================================================

        PlayerStateManager.PlayerState currentState =
            playerStateManager.GetCurrentState();

        if (currentState ==
            PlayerStateManager.PlayerState.Inventory)
        {
            Debug.Log(
                "[LetterUI] Cannot open letter while "
                + "inventory is open."
            );

            return;
        }


        // =====================================================
        // SAVE CURRENT STATE
        // =====================================================

        previousPlayerState = currentState;


        // =====================================================
        // SET LETTER IMAGE
        // =====================================================

        letterImage.sprite =
            selectedLetterImage;

        letterImage.preserveAspect = true;


        // =====================================================
        // SHOW LETTER
        // =====================================================

        letterCanvas.SetActive(true);
        FitLetterToScreen();


        // =====================================================
        // CHANGE PLAYER STATE
        // =====================================================

        playerStateManager.SetState(
            PlayerStateManager.PlayerState.Reading
        );


        // =====================================================
        // CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;


        // =====================================================
        // MARK OPEN
        // =====================================================

        letterOpen = true;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(false);
        }


        Debug.Log(
            "[LetterUI] "
            + "Opened letter."
        );
    }


    // =========================================================
    // CLOSE LETTER
    // =========================================================

    public void CloseLetter()
    {
        if (!letterOpen)
        {
            return;
        }


        // =====================================================
        // HIDE LETTER
        // =====================================================

        if (letterCanvas != null)
        {
            letterCanvas.SetActive(false);
        }


        // =====================================================
        // CLEAR IMAGE
        // =====================================================

        if (letterImage != null)
        {
            letterImage.sprite = null;
        }


        // =====================================================
        // RESTORE PLAYER STATE
        // =====================================================

        if (playerStateManager != null)
        {
            playerStateManager.SetState(
                previousPlayerState
            );
        }


        // =====================================================
        // CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // =====================================================
        // MARK CLOSED
        // =====================================================

        letterOpen = false;
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(true);
        }

        Debug.Log(
            "[LetterUI] Letter closed."
        );
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    public void ToggleLetter()
    {
        if (letterOpen)
        {
            CloseLetter();
        }
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsLetterOpen()
    {
        return letterOpen;
    }

    private void FitLetterToScreen()
    {
        if (letterImage == null)
        {
            return;
        }

        if (letterImage.sprite == null)
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

        // Small border around the letter.
        float horizontalPadding = screenWidth * 0.04f;
        float verticalPadding = screenHeight * 0.04f;

        float availableWidth =
            screenWidth - (horizontalPadding * 2f);

        float availableHeight =
            screenHeight - (verticalPadding * 2f);

        Sprite sprite = letterImage.sprite;

        float spriteWidth = sprite.rect.width;
        float spriteHeight = sprite.rect.height;

        float widthScale =
            availableWidth / spriteWidth;

        float heightScale =
            availableHeight / spriteHeight;

        float scale =
            Mathf.Min(widthScale, heightScale);

        imageRect.sizeDelta = new Vector2(
            spriteWidth * scale,
            spriteHeight * scale
        );

        imageRect.anchoredPosition = Vector2.zero;

        letterImage.preserveAspect = true;
    }
}