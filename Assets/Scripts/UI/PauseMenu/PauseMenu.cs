using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

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

    [Header("Settings")]
    [SerializeField] private SettingsMenu settingsMenu;

    private bool isPaused;

    private bool toolbarWasActive;

    private GameStateManager.GameMode previousGameMode;

    private PlayerStateManager.PlayerState previousPlayerState;

    // Prevent the same Escape/Start press from being processed twice during
    // the pause -> resume transition. This is especially important in the
    // Unity Editor, where Escape also changes Game View focus.
    private bool pauseTransitionInProgress;
    private float ignorePauseInputUntil;
    private Coroutine cursorRestoreRoutine;


    private void Awake()
    {
        if (playerInventory == null)
        {
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        }
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
        pauseTransitionInProgress = false;
        ignorePauseInputUntil = 0f;

        if (container != null)
        {
            container.SetActive(false);
        }
    }


    private void Update()
    {
        if (pauseTransitionInProgress ||
            Time.unscaledTime < ignorePauseInputUntil)
        {
            return;
        }

        bool pausePressed = false;


        // =====================================================
        // KEYBOARD
        // =====================================================

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.pauseKey.wasPressedThisFrame)
            {
                pausePressed = true;
            }
        }


        // =====================================================
        // GAMEPAD
        // =====================================================

        if (Gamepad.current != null)
        {
            // Start / Options button
            if (Gamepad.current.startButton.wasPressedThisFrame)
            {
                GameInputDevice.SetLastUsedDevice(
                    Gamepad.current
                );

                pausePressed = true;
            }
        }


        if (!pausePressed)
        {
            return;
        }


        // =====================================================
        // PAUSE / RESUME
        // =====================================================

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
        if (isPaused || pauseTransitionInProgress)
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

        if (GameUICursorController.Instance != null &&
            GameInputDevice.LastUsedDeviceIsGamepad())
        {
            GameUICursorController.Instance
                .ResetControllerCursorForUI();
        }

        Debug.Log(
            "[PauseMenu] Game paused. Time.timeScale = "
            + Time.timeScale
        );
    }


    public void ContinueButton()
    {
        if (!isPaused || pauseTransitionInProgress)
        {
            return;
        }

        pauseTransitionInProgress = true;
        ignorePauseInputUntil = Time.unscaledTime + 0.25f;


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


        // Lock the cursor again through the persistent cursor controller so
        // controller users do not depend on a mouse event to finish the
        // pause -> gameplay transition.
        ClearUISelection();

        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (cursorRestoreRoutine != null)
        {
            StopCoroutine(cursorRestoreRoutine);
        }

        cursorRestoreRoutine =
            StartCoroutine(ReassertGameplayCursorAfterResume());

#if UNITY_EDITOR
        // Escape can release the Unity Editor Game View focus after the
        // button/callback has already completed. Focus it again after the
        // event has finished so the locked gameplay cursor is authoritative.
        UnityEditor.EditorApplication.delayCall += FocusGameViewAfterResume;
#endif


        Debug.Log(
            "[PauseMenu] Game resumed. Time.timeScale = "
            + Time.timeScale
        );
    }


#if UNITY_EDITOR
    private void FocusGameViewAfterResume()
    {
        if (isPaused)
            return;

        System.Type gameViewType =
            System.Type.GetType("UnityEditor.GameView,UnityEditor");

        if (gameViewType == null)
            return;

        UnityEditor.EditorWindow gameView =
            UnityEditor.EditorWindow.GetWindow(gameViewType);

        if (gameView != null)
        {
            gameView.Focus();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
#endif

    private IEnumerator ReassertGameplayCursorAfterResume()
    {
        // Escape can make Unity Editor/Game View release the hardware cursor
        // after our ContinueButton has already run. Re-apply the gameplay
        // cursor state for several frames so no mouse movement is required.
        float endTime = Time.unscaledTime + 1.0f;

        while (Time.unscaledTime < endTime)
        {
            if (!isPaused)
            {
                // Do not call HideCursorForGameplay every frame here; that
                // method intentionally starts its own focus-recovery routine.
                // Repeating it would constantly restart that routine.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            yield return null;
        }

        pauseTransitionInProgress = false;
        cursorRestoreRoutine = null;
    }


    public void SettingsButton()
    {
        Debug.Log("[PauseMenu] Settings button pressed.");

        if (settingsMenu == null)
        {
            settingsMenu =
                FindFirstObjectByType<SettingsMenu>();
        }

        if (settingsMenu == null)
        {
            Debug.LogError(
                "[PauseMenu] SettingsMenu could not be found."
            );
            return;
        }

        settingsMenu.OpenFromPauseMenu();
    }


    public void MainMenuButton()
    {
        Debug.Log("[PauseMenu] Returning to Main Menu.");


        // Make absolutely sure time is restored
        Time.timeScale = 1f;

        isPaused = false;
        pauseTransitionInProgress = false;
        ignorePauseInputUntil = 0f;

        if (cursorRestoreRoutine != null)
        {
            StopCoroutine(cursorRestoreRoutine);
            cursorRestoreRoutine = null;
        }


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
        pauseTransitionInProgress = false;
        ignorePauseInputUntil = 0f;

        if (cursorRestoreRoutine != null)
        {
            StopCoroutine(cursorRestoreRoutine);
            cursorRestoreRoutine = null;
        }


        Debug.Log("[PauseMenu] Exiting game.");


#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

        // Normal UI quit intentionally bypasses the Alt+F4 cutscene.
        // Alt+F4 is intercepted separately by AltF4ExitCutscene.
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