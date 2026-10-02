using UnityEngine;
using UnityEngine.EventSystems;

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

        // Immediately hand cursor control back to gameplay.
        // This is done before GameFlowManager starts the cutscene so the
        // persistent GameUICursorController cannot leave the mouse cursor
        // visible for a frame or two after Start is pressed.
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

        //// Hide the menu immediately.
        //if (mainMenuGroup != null)
        //{
        //    mainMenuGroup.SetActive(false);
        //}

        //if (gameModeSelectionScreen != null)
        //{
        //    gameModeSelectionScreen.SetActive(false);
        //}

        // Start the actual game.
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