using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuGroup;

    [Header("Game Mode / Gender Selection")]
    [SerializeField] private GameObject gameModeSelectionScreen;

    [Header("Settings")]
    [SerializeField] private SettingsMenu settingsMenu;


    // =========================================================
    // PLAYER SELECTIONS
    // =========================================================

    private GameStateManager.DifficultyMode selectedDifficulty =
        GameStateManager.DifficultyMode.Normal;

    private GameStateManager.PlayerGender selectedGender =
        GameStateManager.PlayerGender.Male;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log("[MainMenu] Main Menu started.");

        // ---------------------------------------------------------
        // TIME
        // ---------------------------------------------------------

        Time.timeScale = 1f;

        // ---------------------------------------------------------
        // MAIN MENU MUSIC
        // ---------------------------------------------------------

        // The same music cue continues into the Game Mode / Gender
        // selection screen. It is only stopped when Continue is pressed.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.MainMenu
            );
        }

        // ---------------------------------------------------------
        // GAME STATE
        // ---------------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.MainMenu
            );
        }

        // ---------------------------------------------------------
        // PLAYER STATE
        // ---------------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.MainMenu
            );
        }

        // ---------------------------------------------------------
        // UI
        // ---------------------------------------------------------

        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);

        if (gameModeSelectionScreen != null)
            gameModeSelectionScreen.SetActive(false);

        // ---------------------------------------------------------
        // CURSOR
        // ---------------------------------------------------------

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ClearUISelection();
    }


    // =========================================================
    // PLAY
    // =========================================================

    public void PlayButton()
    {
        Debug.Log("[MainMenu] Play pressed.");

        // If Play was reached with a controller, explicitly register that
        // device. This prevents the persistent cursor controller from
        // waiting for a mouse event before it knows a controller is active.
        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.buttonWest.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame))
        {
            GameInputDevice.SetLastUsedDevice(Gamepad.current);
        }

        if (mainMenuGroup != null)
        {
            mainMenuGroup.SetActive(false);
        }

        if (gameModeSelectionScreen != null)
        {
            gameModeSelectionScreen.SetActive(true);
        }

        ClearUISelection();
    }


    // =========================================================
    // DIFFICULTY
    // =========================================================

    public void NormalButton()
    {
        selectedDifficulty =
            GameStateManager.DifficultyMode.Normal;

        Debug.Log("[MainMenu] Difficulty selected: Normal");
    }


    public void HardButton()
    {
        selectedDifficulty =
            GameStateManager.DifficultyMode.Hardcore;

        Debug.Log("[MainMenu] Difficulty selected: Hardcore");
    }


    // =========================================================
    // GENDER
    // =========================================================

    public void MaleButton()
    {
        selectedGender =
            GameStateManager.PlayerGender.Male;

        Debug.Log("[MainMenu] Gender selected: Male");
    }


    public void FemaleButton()
    {
        selectedGender =
            GameStateManager.PlayerGender.Female;

        Debug.Log("[MainMenu] Gender selected: Female");
    }


    // =========================================================
    // CONTINUE
    // =========================================================

    public void ContinueButton()
    {
        Debug.Log(
            "[MainMenu] Continue pressed. " +
            "Difficulty: " + selectedDifficulty +
            " | Gender: " + selectedGender
        );

        // Prefer the cursor controller's gamepad state over wasPressedThisFrame.
        // SelectAtCursor can invoke this after the press frame, and Start /
        // South / EventSystem submit must all count as controller start.
        if (Gamepad.current != null &&
            (GameInputDevice.LastUsedDeviceIsGamepad() ||
             (GameUICursorController.Instance != null &&
              GameUICursorController.Instance.IsUsingGamepad()) ||
             Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.buttonWest.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame))
        {
            GameInputDevice.SetLastUsedDevice(Gamepad.current);
        }

        ClearUISelection();

        // The Main Menu cue ends exactly when the player confirms the
        // Game Mode / Gender selection.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }

        // Hide the cursor immediately, but KEEP the game-mode selection
        // screen active. Its full-screen background acts as the black
        // hold until IntroCutscene reports the first video frame, then
        // GameFlowManager.HideMenuAfterVideoFrame removes it.
        GameUICursorController cursorController =
            GameUICursorController.Instance;

        if (cursorController != null)
        {
            cursorController.HideCursorForGameplay();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (GameFlowManager.Instance == null)
        {
            Debug.LogError(
                "[MainMenu] GameFlowManager.Instance is missing!"
            );

            return;
        }

        GameFlowManager.Instance.StartNewGame(
            selectedDifficulty,
            selectedGender
        );
    }


    // =========================================================
    // SETTINGS
    // =========================================================

    public void SettingsButton()
    {
        Debug.Log("[MainMenu] Settings pressed.");

        if (settingsMenu == null)
        {
            settingsMenu =
                FindFirstObjectByType<SettingsMenu>();
        }

        if (settingsMenu == null)
        {
            Debug.LogError(
                "[MainMenu] SettingsMenu could not be found."
            );
            return;
        }

        settingsMenu.OpenFromMainMenu();
    }


    // =========================================================
    // CREDITS
    // =========================================================

    public void CreditsButton()
    {
        Debug.Log("[MainMenu] Credits pressed.");

        if (GameFlowManager.Instance == null)
        {
            Debug.LogError(
                "[MainMenu] GameFlowManager.Instance is missing!"
            );

            return;
        }

        GameFlowManager.Instance.OpenCredits();
    }


    // =========================================================
    // QUIT
    // =========================================================

    public void QuitButton()
    {
        Debug.Log("[MainMenu] Quit pressed.");

#if UNITY_EDITOR

        UnityEditor.EditorApplication.isPlaying = false;

#else

        // Normal UI quit intentionally bypasses the Alt+F4 cutscene.
        // Alt+F4 is intercepted separately by AltF4ExitCutscene.
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
}