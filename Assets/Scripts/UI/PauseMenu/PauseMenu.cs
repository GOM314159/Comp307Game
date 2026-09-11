using UnityEngine;
using UnityEngine.EventSystems;
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

    private PlayerStateManager.PlayerState previousPlayerState;


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
        Time.timeScale = 1f;

        isPaused = false;

        if (container != null)
        {
            container.SetActive(false);
        }
    }


    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (isPaused)
        {
            ContinueButton();
            return;
        }

        if (PlayerStateManager.Instance == null)
        {
            return;
        }

        if (!PlayerStateManager.Instance.CanPause())
        {
            return;
        }

        PauseGame();
    }


    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        if (PlayerStateManager.Instance == null)
        {
            Debug.LogWarning("[PauseMenu] PlayerStateManager.Instance is missing.");
            return;
        }


        // Close inventory if it is open
        if (playerInventory != null)
        {
            if (playerInventory.IsInventoryOpen())
            {
                playerInventory.CloseInventory();
            }
        }


        // Save current GameMode
        if (GameStateManager.Instance != null)
        {
            previousGameMode = GameStateManager.Instance.GetGameMode();
        }


        // Save current PlayerState
        previousPlayerState = PlayerStateManager.Instance.GetCurrentState();


        // Save toolbar state and hide it
        if (toolbar != null)
        {
            toolbarWasActive = toolbar.activeSelf;
            toolbar.SetActive(false);
        }


        // Mark the game as paused
        isPaused = true;


        // Set GameMode to Paused
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Paused
            );
        }


        // Set PlayerState to PauseMenu
        PlayerStateManager.Instance.SetState(
            PlayerStateManager.PlayerState.PauseMenu
        );


        // Actually pause Unity's game time
        Time.timeScale = 0f;


        // Show pause menu
        if (container != null)
        {
            container.SetActive(true);
        }


        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        ClearUISelection();


        Debug.Log(
            "[PauseMenu] Game paused. Time.timeScale = "
            + Time.timeScale
        );
    }


    public void ContinueButton()
    {
        if (!isPaused)
        {
            return;
        }


        // Hide pause menu
        if (container != null)
        {
            container.SetActive(false);
        }


        // Restore toolbar
        if (toolbar != null)
        {
            toolbar.SetActive(toolbarWasActive);
        }


        // Resume Unity's game time
        Time.timeScale = 1f;


        // Restore previous GameMode
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }


        // Restore previous PlayerState
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                previousPlayerState
            );
        }


        // Mark game as unpaused
        isPaused = false;


        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        ClearUISelection();


        Debug.Log(
            "[PauseMenu] Game resumed. Time.timeScale = "
            + Time.timeScale
        );
    }


    public void SettingsButton()
    {
        Debug.Log("[PauseMenu] Settings button pressed.");
    }


    public void MainMenuButton()
    {
        Debug.Log("[PauseMenu] Returning to Main Menu.");


        // Make absolutely sure time is restored
        Time.timeScale = 1f;

        isPaused = false;


        // Hide pause menu
        if (container != null)
        {
            container.SetActive(false);
        }


        // Set player state to MainMenu
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.MainMenu
            );
        }


        // Open main menu
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OpenMainMenu();
        }
        else
        {
            Debug.LogError(
                "[PauseMenu] GameFlowManager.Instance is missing."
            );
        }
    }


    public void ExitButton()
    {
        Time.timeScale = 1f;

        isPaused = false;


        Debug.Log("[PauseMenu] Exiting game.");


#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

        Application.Quit();

#endif
    }


    private void ClearUISelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    public bool IsPaused()
    {
        return isPaused;
    }
}