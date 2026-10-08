using System.Collections;
using UnityEngine;

public class PlayerDeathManager : MonoBehaviour
{
    public static PlayerDeathManager Instance { get; private set; }

    [Header("Death UI")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject normalPanel;
    [SerializeField] private GameObject hardcorePanel;

    [Header("Death Delay")]
    [Tooltip("Delay before the death screen becomes visible.")]
    [SerializeField, Min(0f)] private float deathScreenDelay = 5f;

    private Coroutine deathRoutine;

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

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }
    }

    private void HandleLoss(GameStateManager.LossReason reason)
    {
        if (reason != GameStateManager.LossReason.PlayerDeath)
        {
            return;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(ShowDeathScreenAfterDelay());
    }

    private IEnumerator ShowDeathScreenAfterDelay()
    {
        if (deathScreenDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(deathScreenDelay);
        }

        deathRoutine = null;

        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[PlayerDeathManager] GameStateManager is missing."
            );
            yield break;
        }

        bool bossFightDeath =
            GameStateManager.Instance.GetGameMode() ==
            GameStateManager.GameMode.BossFight;

        DeathScreen ui =
            FindAnyObjectByType<DeathScreen>(
                FindObjectsInactive.Include
            );

        if (ui != null)
        {
            ui.SetBossFightDeath(bossFightDeath);
            ui.ShowDeathScreen();
            yield break;
        }

        // Fallback for a scene that does not contain the dedicated
        // DeathScreen component.
        ShowDeathScreenFallback(bossFightDeath);
    }

    private void ShowDeathScreenFallback(bool bossFightDeath)
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }

        if (normalPanel != null)
        {
            normalPanel.SetActive(!GameStateManager.Instance.IsHardcore());
        }

        if (hardcorePanel != null)
        {
            hardcorePanel.SetActive(GameStateManager.Instance.IsHardcore());
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
