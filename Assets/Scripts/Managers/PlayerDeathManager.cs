using UnityEngine;

public class PlayerDeathManager : MonoBehaviour
{
    public static PlayerDeathManager Instance { get; private set; }

    [Header("Death UI")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private GameObject hardcorePanel;

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
        HideDeathScreen();
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

    private void HandleLoss(GameStateManager.LossReason reason)
    {
        if (reason != GameStateManager.LossReason.PlayerDeath)
        {
            return;
        }

        ShowDeathScreen();
    }

    private void ShowDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }

        if (normalPanel != null)
        {
            normalPanel.SetActive(false);
        }

        if (hardcorePanel != null)
        {
            hardcorePanel.SetActive(false);
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[PlayerDeathManager] GameStateManager is missing."
            );
            return;
        }

        bool bossFightDeath =
            GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight;

        DeathScreen ui =
            FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);

        if (ui != null)
        {
            ui.SetBossFightDeath(bossFightDeath);
        }

        if (GameStateManager.Instance.IsHardcore())
        {
            if (hardcorePanel != null)
            {
                hardcorePanel.SetActive(true);
            }
        }
        else
        {
            if (normalPanel != null)
            {
                normalPanel.SetActive(true);
            }
        }

        if (bossFightDeath)
        {
            Debug.Log(
                "[PlayerDeathManager] Boss-fight death. " +
                "Respawn disabled; player must return to Main Menu."
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

        // A boss-fight death is always a complete run restart.
        if (GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight)
        {
            Debug.Log(
                "[PlayerDeathManager] Boss-fight death cannot respawn. " +
                "Returning to Main Menu with a full game reset."
            );

            Time.timeScale = 1f;

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OpenMainMenu();
            }

            return;
        }

        if (GameStateManager.Instance.IsNormal())
        {
            HideDeathScreen();
            Time.timeScale = 1f;

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.RestartCurrentDayAfterDeath();
            }

            return;
        }

        HideDeathScreen();
        Time.timeScale = 1f;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RestartEntireGameAfterDeath();
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        HideDeathScreen();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OpenMainMenu();
        }
        else if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetEntireGame();
        }
    }

    private void HideDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }

        if (normalPanel != null)
        {
            normalPanel.SetActive(false);
        }

        if (hardcorePanel != null)
        {
            hardcorePanel.SetActive(false);
        }
    }
}
