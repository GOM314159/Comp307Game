using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Pause Menu")]
    [SerializeField] private GameObject container;

    [Header("Inventory")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject toolbar;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;

    private bool toolbarWasActive;

    private GameStateManager.GameMode previousGameMode;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    private void Start()
    {
        // Pause menu starts closed.
        if (container != null)
            container.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }


    // =========================================================
    // INPUT
    // =========================================================

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        // -----------------------------------------------------
        // IF ALREADY PAUSED
        // -----------------------------------------------------

        if (isPaused)
        {
            ContinueButton();
            return;
        }

        // -----------------------------------------------------
        // CHECK PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance == null)
            return;

        if (!PlayerStateManager.Instance.CanPause())
            return;

        // -----------------------------------------------------
        // OPEN PAUSE MENU
        // -----------------------------------------------------

        PauseGame();
    }


    // =========================================================
    // PAUSE
    // =========================================================

    public void PauseGame()
    {
        if (isPaused)
            return;


        // -----------------------------------------------------
        // CLOSE INVENTORY PROPERLY
        // -----------------------------------------------------

        // IMPORTANT:
        // Do NOT simply deactivate the inventory GameObject.
        // PlayerInventory needs to know that it has been closed.

        if (playerInventory != null &&
            playerInventory.IsInventoryOpen())
        {
            playerInventory.CloseInventory();
        }


        // -----------------------------------------------------
        // REMEMBER GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            previousGameMode =
                GameStateManager.Instance.GetGameMode();
        }


        // -----------------------------------------------------
        // REMEMBER TOOLBAR STATE
        // -----------------------------------------------------

        if (toolbar != null)
        {
            toolbarWasActive = toolbar.activeSelf;
            toolbar.SetActive(false);
        }


        // -----------------------------------------------------
        // PAUSE
        // -----------------------------------------------------

        isPaused = true;


        // Show pause menu.
        if (container != null)
            container.SetActive(true);


        // Tell GameStateManager we are paused.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Paused
            );
        }


        // Stop gameplay.
        Time.timeScale = 0f;


        // Make sure the cursor can interact with the pause menu.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        ClearUISelection();
    }


    // =========================================================
    // CONTINUE
    // =========================================================

    public void ContinueButton()
    {
        if (!isPaused)
            return;


        // -----------------------------------------------------
        // HIDE PAUSE MENU
        // -----------------------------------------------------

        if (container != null)
            container.SetActive(false);


        // -----------------------------------------------------
        // RESTORE TOOLBAR
        // -----------------------------------------------------

        if (toolbar != null)
            toolbar.SetActive(toolbarWasActive);


        // -----------------------------------------------------
        // RESTORE GAME
        // -----------------------------------------------------

        isPaused = false;

        Time.timeScale = 1f;


        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }


        // Restore gameplay cursor.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        ClearUISelection();
    }


    // =========================================================
    // SETTINGS
    // =========================================================

    public void SettingsButton()
    {
        Debug.Log("[PauseMenu] Settings button pressed.");

        // Settings UI can be added here later.
    }


    // =========================================================
    // MAIN MENU
    // =========================================================

    public void MainMenuButton()
    {
        Debug.Log("[PauseMenu] Returning to Main Menu.");

        // Stop pause mode.
        Time.timeScale = 1f;

        isPaused = false;

        // Hide pause menu.
        if (container != null)
        {
            container.SetActive(false);
        }

        // Return to the Main Menu through GameFlowManager.
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OpenMainMenu();
        }
        else
        {
            Debug.LogError(
                "[PauseMenu] GameFlowManager.Instance is missing!"
            );
        }
    }


    // =========================================================
    // EXIT
    // =========================================================

    public void ExitButton()
    {
        Time.timeScale = 1f;

        Debug.Log("[PauseMenu] Exiting game.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // =========================================================
    // UI
    // =========================================================

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsPaused()
    {
        return isPaused;
    }
}