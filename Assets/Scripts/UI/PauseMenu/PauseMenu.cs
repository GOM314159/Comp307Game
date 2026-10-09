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

    // Short debounce only — must NOT last for the whole cursor-reassert
    // window, or Escape presses after resume get eaten.
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
        ignorePauseInputUntil = 0f;

        if (container != null)
        {
            container.SetActive(false);
        }
    }


    private void Update()
    {
        if (Time.unscaledTime < ignorePauseInputUntil)
        {
            return;
        }

        bool pausePressed = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.pauseKey.wasPressedThisFrame)
            {
                GameInputDevice.SetLastUsedDevice(
                    Keyboard.current
                );

                pausePressed = true;
            }
        }

        if (Gamepad.current != null)
        {
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
            Debug.LogWarning(
                "[PauseMenu] PlayerStateManager.Instance is missing."
            );
            return;
        }

        if (playerInventory != null &&
            playerInventory.IsInventoryOpen())
        {
            playerInventory.CloseInventory();
        }

        if (GameStateManager.Instance != null)
        {
            previousGameMode =
                GameStateManager.Instance.GetGameMode();
        }

        previousPlayerState =
            PlayerStateManager.Instance.GetCurrentState();

        if (toolbar != null)
        {
            toolbarWasActive = toolbar.activeSelf;
            toolbar.SetActive(false);
        }

        isPaused = true;

        // Ignore the same Escape/Start that opened the menu so it cannot
        // immediately close it on a held/repeat edge.
        ignorePauseInputUntil = Time.unscaledTime + 0.2f;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Paused
            );
        }

        PlayerStateManager.Instance.SetState(
            PlayerStateManager.PlayerState.PauseMenu
        );

        // AudioSource playback is independent of Time.timeScale, so pause
        // the music explicitly rather than relying on the game freeze.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseMusic();
        }

        Time.timeScale = 0f;

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.SetTutorialAudioPaused(true);

        if (container != null)
        {
            container.SetActive(true);
        }

        ClearUISelection();

        if (GameUICursorController.Instance != null)
        {
            if (GameInputDevice.LastUsedDeviceIsGamepad())
            {
                GameUICursorController.Instance
                    .ResetControllerCursorForUI();
            }
            else
            {
                GameUICursorController.Instance
                    .ShowCursorForUI();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

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

        // Debounce only — do not block Escape for a full second.
        ignorePauseInputUntil = Time.unscaledTime + 0.25f;

        bool resumedWithEscape =
            Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame ||
             Keyboard.current.escapeKey.isPressed);

        if (container != null)
        {
            container.SetActive(false);
        }

        if (toolbar != null)
        {
            toolbar.SetActive(toolbarWasActive);
        }

        Time.timeScale = 1f;

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.SetTutorialAudioPaused(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                previousGameMode
            );
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                previousPlayerState
            );
        }

        isPaused = false;

        ClearUISelection();

        // Claim gameplay cursor ownership IMMEDIATELY so
        // GameUICursorController.SyncCursorVisibility stops forcing the
        // mouse visible while we wait out Escape's Editor unlock.
        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cursorRestoreRoutine != null)
        {
            StopCoroutine(cursorRestoreRoutine);
        }

        cursorRestoreRoutine =
            StartCoroutine(
                HideCursorAfterEscapeResume(resumedWithEscape)
            );

        Debug.Log(
            "[PauseMenu] Game resumed. Time.timeScale = "
            + Time.timeScale
        );
    }


#if UNITY_EDITOR
    private void FocusGameViewAfterResume()
    {
        if (isPaused)
        {
            return;
        }

        System.Type gameViewType =
            System.Type.GetType(
                "UnityEditor.GameView,UnityEditor"
            );

        if (gameViewType == null)
        {
            return;
        }

        UnityEditor.EditorWindow gameView =
            UnityEditor.EditorWindow.GetWindow(
                gameViewType,
                false,
                null,
                true
            );

        if (gameView != null)
        {
            gameView.Focus();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
#endif


    private IEnumerator HideCursorAfterEscapeResume(
        bool resumedWithEscape)
    {
        if (resumedWithEscape)
        {
            // Escape unlocks the cursor in the Editor while held. Keep the
            // gameplay lock flag set, wait for release, then force-hide again.
            while (Keyboard.current != null &&
                   Keyboard.current.escapeKey.isPressed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                yield return null;
            }

            yield return null;
            yield return new WaitForEndOfFrame();
        }

        if (GameUICursorController.Instance != null)
        {
            GameUICursorController.Instance.HideCursorForGameplay();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

#if UNITY_EDITOR
        FocusGameViewAfterResume();
        UnityEditor.EditorApplication.delayCall +=
            FocusGameViewAfterResume;
#endif

        // Keep hammering the lock for a short window after Escape.
        float endTime = Time.unscaledTime + 1.0f;

        while (Time.unscaledTime < endTime)
        {
            if (isPaused)
            {
                break;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

#if UNITY_EDITOR
            if (Time.unscaledTime < endTime - 0.5f)
            {
                FocusGameViewAfterResume();
            }
#endif

            yield return null;
        }

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

        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }

        isPaused = false;
        ignorePauseInputUntil = 0f;

        if (cursorRestoreRoutine != null)
        {
            StopCoroutine(cursorRestoreRoutine);
            cursorRestoreRoutine = null;
        }

        if (container != null)
        {
            container.SetActive(false);
        }

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.MainMenu
            );
        }

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
