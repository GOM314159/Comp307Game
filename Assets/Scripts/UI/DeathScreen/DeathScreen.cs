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

        Time.timeScale = 1f;

        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        PlayerStateManager playerStateManager =
            FindAnyObjectByType<PlayerStateManager>();

        if (playerStateManager != null)
        {
            playerStateManager.ResetToDefault();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetCurrentDay();
        }

        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClearUISelection();

        Debug.Log("[DeathScreen] Player respawned.");
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
        // SET GAME STATE
        // =========================================================

        if (GameStateManager.Instance != null)
        {
            if (GameStateManager.Instance.IsHardcore())
            {
                Debug.Log(
                    "[DeathScreen] Hardcore death - resetting entire game."
                );

                GameStateManager.Instance.ResetEntireGame();
            }
            else
            {
                Debug.Log(
                    "[DeathScreen] Normal death - returning to Main Menu."
                );

                GameStateManager.Instance.ResetCurrentDay();

                GameStateManager.Instance.SetGameMode(
                    GameStateManager.GameMode.MainMenu
                );
            }
        }


        // =========================================================
        // HIDE DEATH SCREEN
        // =========================================================

        if (deathScreenGroup != null)
        {
            deathScreenGroup.SetActive(false);
        }


        // =========================================================
        // SHOW MAIN MENU
        // =========================================================

        if (mainMenu != null)
        {
            mainMenu.SetActive(true);

            Debug.Log(
                "[DeathScreen] Main Menu UI activated."
            );
        }
        else
        {
            Debug.LogError(
                "[DeathScreen] Main Menu reference is not assigned."
            );
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