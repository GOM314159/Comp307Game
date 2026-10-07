using UnityEngine;
using static GameStateManager;

public class PlayerDeathManager : MonoBehaviour
{
    public static PlayerDeathManager Instance { get; private set; }

    [Header("Death UI")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private GameObject hardcorePanel;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (deathScreen != null)
            deathScreen.SetActive(false);

        if (normalPanel != null)
            normalPanel.SetActive(false);

        if (hardcorePanel != null)
            hardcorePanel.SetActive(false);

        playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError(
                "[PlayerDeathManager] Could not find PlayerHealth."
            );
        }
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss += HandleLoss;
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -= HandleLoss;
        }
    }

    private void HandleLoss(LossReason reason)
    {
        if (reason != LossReason.PlayerDeath)
            return;

        ShowDeathScreen();
    }

    private void ShowDeathScreen()
    {
        if (deathScreen != null)
            deathScreen.SetActive(true);

        if (normalPanel != null)
            normalPanel.SetActive(false);

        if (hardcorePanel != null)
            hardcorePanel.SetActive(false);

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[PlayerDeathManager] GameStateManager is missing."
            );
            return;
        }

        bool diedDuringBossFight =
            GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight;

        if (diedDuringBossFight)
        {
            // A final-boss death is a run-ending death. There is no normal
            // respawn from the boss arena; the player must return to the
            // Main Menu and start a fresh run.
            if (normalPanel != null)
                normalPanel.SetActive(true);

            if (hardcorePanel != null)
                hardcorePanel.SetActive(false);

            if (normalRespawnButton != null)
                normalRespawnButton.gameObject.SetActive(false);

            Debug.Log(
                "[PlayerDeathManager] Boss-fight death. Respawn disabled; " +
                "Main Menu is required."
            );
        }
        else if (GameStateManager.Instance.IsNormal())
        {
            if (normalPanel != null)
                normalPanel.SetActive(true);

            if (normalRespawnButton != null)
                normalRespawnButton.gameObject.SetActive(true);

            Debug.Log(
                "[PlayerDeathManager] Normal death screen shown."
            );
        }
        else if (GameStateManager.Instance.IsHardcore())
        {
            if (hardcorePanel != null)
                hardcorePanel.SetActive(true);

            if (normalRespawnButton != null)
                normalRespawnButton.gameObject.SetActive(false);

            Debug.Log(
                "[PlayerDeathManager] Hardcore death screen shown."
            );
        }
    }

    public void Respawn()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[PlayerDeathManager] GameStateManager is missing."
            );

            return;
        }

        // ---------------------------------------------------------
        // FINAL BOSS DEATH
        // ---------------------------------------------------------

        if (GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight)
        {
            Debug.Log(
                "[PlayerDeathManager] Boss-fight death cannot respawn. " +
                "Returning to Main Menu with a full game reset."
            );

            HideDeathScreen();
            Time.timeScale = 1f;

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenMainMenu();
            }

            return;
        }

        // ---------------------------------------------------------
        // NORMAL MODE
        // ---------------------------------------------------------

        if (GameStateManager.Instance.IsNormal())
        {
            Debug.Log(
                "[PlayerDeathManager] Normal death. " +
                "Restarting current day."
            );

            HideDeathScreen();

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance
                    .RestartCurrentDayAfterDeath();
            }
            else
            {
                Debug.LogError(
                    "[PlayerDeathManager] GameFlowManager is missing."
                );
            }

            return;
        }

        // ---------------------------------------------------------
        // HARDCORE
        // ---------------------------------------------------------

        Debug.Log(
            "[PlayerDeathManager] Hardcore death. " +
            "Resetting entire game."
        );

        HideDeathScreen();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance
                .RestartEntireGameAfterDeath();
        }
        else
        {
            Debug.LogError(
                "[PlayerDeathManager] GameFlowManager is missing."
            );
        }
    }

    public void ReturnToMainMenu()
    {
        if (GameStateManager.Instance == null)
            return;

        bool bossFightDeath =
            GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight;

        if (!GameStateManager.Instance.IsHardcore() && !bossFightDeath)
        {
            Debug.LogWarning(
                "[PlayerDeathManager] Main menu requested outside a run-ending death state."
            );
            return;
        }

        Debug.Log(
            "[PlayerDeathManager] Hardcore run ended."
        );

        HideDeathScreen();

        GameStateManager.Instance.ResetEntireGame();
    }

    private void HideDeathScreen()
    {
        if (deathScreen != null)
            deathScreen.SetActive(false);

        if (normalPanel != null)
            normalPanel.SetActive(false);

        if (hardcorePanel != null)
            hardcorePanel.SetActive(false);
    }


}