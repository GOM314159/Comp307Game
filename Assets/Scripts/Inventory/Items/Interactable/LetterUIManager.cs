using UnityEngine;
using TMPro;
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

    [Tooltip("The already-designed Letter Canvas.")]
    [SerializeField]
    private GameObject letterCanvas;

    [Tooltip("The TextMeshProUGUI component that displays the letter.")]
    [SerializeField]
    private TextMeshProUGUI letterText;


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

        if (Instance != null && Instance != this)
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
        // LETTER STARTS CLOSED
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
    string text)
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

        PlayerStateManager.PlayerState currentState =
            playerStateManager.GetCurrentState();

        // ---------------------------------------------------------
        // DON'T ALLOW LETTER WHILE INVENTORY IS OPEN
        // ---------------------------------------------------------

        if (currentState ==
            PlayerStateManager.PlayerState.Inventory)
        {
            Debug.Log(
                "[LetterUI] Cannot read letter while "
                + "inventory is open."
            );

            return;
        }

        // ---------------------------------------------------------
        // SAVE CURRENT STATE
        // ---------------------------------------------------------

        previousPlayerState = currentState;

        // ---------------------------------------------------------
        // SET TEXT
        // ---------------------------------------------------------

        letterText.text = text;

        // ---------------------------------------------------------
        // SHOW LETTER
        // ---------------------------------------------------------

        letterCanvas.SetActive(true);

        // ---------------------------------------------------------
        // LOCK PLAYER
        // ---------------------------------------------------------

        playerStateManager.SetState(
            PlayerStateManager.PlayerState.Reading
        );

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        letterOpen = true;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        letterOpen = false;


        Debug.Log(
            "[LetterUI] Letter closed."
        );
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool IsLetterOpen()
    {
        return letterOpen;
    }
}