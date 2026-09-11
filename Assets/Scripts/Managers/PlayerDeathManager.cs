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

        if (GameStateManager.Instance.IsNormal())
        {
            if (normalPanel != null)
                normalPanel.SetActive(true);

            Debug.Log(
                "[PlayerDeathManager] Normal death screen shown."
            );
        }
        else if (GameStateManager.Instance.IsHardcore())
        {
            if (hardcorePanel != null)
                hardcorePanel.SetActive(true);

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

        if (!GameStateManager.Instance.IsHardcore())
        {
            Debug.LogWarning(
                "[PlayerDeathManager] Main menu requested outside Hardcore mode."
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