using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreenGroup;

    [Header("Normal Mode")]
    [SerializeField] private GameObject normalModePanel;
    [SerializeField] private Button normalRespawnButton;
    [SerializeField] private Button normalMainMenuButton;

    [Header("Hardcore Mode")]
    [SerializeField] private GameObject hardcoreModePanel;
    [SerializeField] private Button hardcoreMainMenuButton;

    [Header("Main Menu")]

    [SerializeField] private GameObject mainMenu;


    private void Awake()
    {
        // Death screen starts hidden.
        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }
    }


    private void Start()
    {
        // GameStateManager has finished Awake() by this point.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss += HandleLoss;

            Debug.Log("[DeathScreen] Successfully subscribed to GameStateManager OnLoss.");
        }
        else
        {
            Debug.LogError(
                "[DeathScreen] GameStateManager.Instance is missing."
            );
        }

        // Connect buttons.
        if (normalRespawnButton != null)
        {
            normalRespawnButton.onClick.AddListener(OnNormalRespawnPressed);
        }

        if (normalMainMenuButton != null)
        {
            normalMainMenuButton.onClick.AddListener(OnMainMenuPressed);
        }

        if (hardcoreMainMenuButton != null)
        {
            hardcoreMainMenuButton.onClick.AddListener(OnMainMenuPressed);
        }
    }


    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -= HandleLoss;
        }
    }


    private void HandleLoss(GameStateManager.LossReason reason)
    {
        Debug.Log(
            "[DeathScreen] Loss received: " + reason
        );

        if (reason != GameStateManager.LossReason.PlayerDeath)
        {
            return;
        }

        ShowDeathScreen();
    }


    public void ShowDeathScreen()
    {
        Debug.Log("[DeathScreen] Showing death screen.");

        if (deathScreenGroup == null)
        {
            Debug.LogError(
                "[DeathScreen] Death Screen Group is not assigned."
            );

            return;
        }

        // =========================================================
        // SHOW DEATH SCREEN
        // =========================================================

        deathScreenGroup.SetActive(true);


        // =========================================================
        // SET PLAYER STATE
        // =========================================================

        PlayerStateManager playerStateManager =
            FindAnyObjectByType<PlayerStateManager>();

        if (playerStateManager != null)
        {
            playerStateManager.SetRIP();

            Debug.Log(
                "[DeathScreen] Player State set to RIP."
            );
        }


        // =========================================================
        // SET GAME STATE
        // =========================================================

        if (GameStateManager.Instance != null)
        {
            // Keep the overall game state as-is for now.
            // Death is handled through the Loss system.
        }


        // =========================================================
        // STOP GAMEPLAY
        // =========================================================

        Time.timeScale = 0f;


        // =========================================================
        // UNLOCK CURSOR
        // =========================================================

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameUICursorController.Instance != null &&
            GameInputDevice.LastUsedDeviceIsGamepad())
        {
            GameUICursorController.Instance
                .ResetControllerCursorForUI();
        }


        // =========================================================
        // CLEAR UI SELECTION
        // =========================================================

        ClearUISelection();


        // =========================================================
        // SELECT CORRECT DEATH PANEL
        // =========================================================

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsHardcore())
        {
            Debug.Log("[DeathScreen] Hardcore mode.");

            if (normalModePanel != null)
            {
                normalModePanel.SetActive(false);
            }

            if (hardcoreModePanel != null)
            {
                hardcoreModePanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log("[DeathScreen] Normal mode.");

            if (normalModePanel != null)
            {
                normalModePanel.SetActive(true);
            }

            if (hardcoreModePanel != null)
            {
                hardcoreModePanel.SetActive(false);
            }
        }
    }


    // =========================================================
    // NORMAL - RESPAWN
    // =========================================================

    private void OnNormalRespawnPressed()
    {
        Debug.Log("[DeathScreen] Respawn pressed.");

        // ---------------------------------------------------------
        // RESTORE TIME
        // ---------------------------------------------------------

        Time.timeScale = 1f;

        // ---------------------------------------------------------
        // GAME FLOW HANDLES THE ENTIRE RESPawn
        // ---------------------------------------------------------
        //
        // This will:
        //
        // 1. Get the current day
        // 2. Reset everything that happened during that day
        // 3. Restart the same day
        // 4. Open the Lobby
        // 5. Move the Player to PlayerRespawnPoint
        // 6. Reset health
        // 7. Reset movement/state
        //
        // ---------------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RestartCurrentDayAfterDeath();
        }
        else
        {
            Debug.LogError(
                "[DeathScreen] GameFlowManager.Instance is missing!"
            );

            return;
        }

        // ---------------------------------------------------------
        // HIDE DEATH SCREEN
        // ---------------------------------------------------------

        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }

        // ---------------------------------------------------------
        // CURSOR
        // ---------------------------------------------------------

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClearUISelection();

        Debug.Log(
            "[DeathScreen] Normal respawn requested through GameFlowManager."
        );
    }


    // =========================================================
    // MAIN MENU
    // =========================================================

    private void OnMainMenuPressed()
    {
        Debug.Log("[DeathScreen] Main Menu pressed.");

        // =========================================================
        // RESTORE TIME
        // =========================================================

        Time.timeScale = 1f;


        // =========================================================
        // HIDE DEATH SCREEN
        // =========================================================

        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }


        // =========================================================
        // CURSOR
        // =========================================================

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


        // =========================================================
        // CLEAR UI SELECTION
        // =========================================================

        ClearUISelection();


        // =========================================================
        // RETURN TO MAIN MENU
        // =========================================================
        //
        // GameFlowManager owns the entire "return to Main Menu"
        // flow: it resets game progress the same as before, and -
        // if death happened in a scene that was loaded on its own
        // rather than through GamePlayTest - it will load
        // GamePlayTest first so the Main Menu UI is guaranteed to
        // actually exist and work. Doing it this way instead of
        // toggling a local `mainMenu` reference here means this
        // still works no matter which scene the death happened in.

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OpenMainMenu();
        }
        else
        {
            Debug.LogError(
                "[DeathScreen] GameFlowManager.Instance is missing!"
            );
        }
    }


    private void HideDeathScreen()
    {
        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}