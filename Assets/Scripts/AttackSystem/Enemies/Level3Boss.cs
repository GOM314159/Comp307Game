using UnityEngine;

/// <summary>
/// Level 3 boss completion. The exit portal is hidden at the start and only
/// becomes active when the boss objective is completed.
/// </summary>
public class Level3Boss : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private Health bossHealth;

    [Header("Exit Portal")]
    [SerializeField] private GameObject exitPortal;

    [Header("Debug")]
    [SerializeField] private bool debugBoss = true;

    private bool deathHandled;

    private void Awake()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<Health>();
        }

        HideExitPortal();
    }

    private void OnEnable()
    {
        if (bossHealth == null)
        {
            bossHealth = GetComponent<Health>();
        }

        HideExitPortal();

        if (bossHealth != null)
        {
            bossHealth.OnDied += HandleBossDied;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied -= HandleBossDied;
        }
    }

    private void HandleBossDied()
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.DefeatBoss();
        }
        else
        {
            Debug.LogError(
                "[Level3Boss] CaseManager.Instance is missing."
            );
        }

        if (exitPortal != null)
        {
            exitPortal.SetActive(true);
            DebugBoss("Level 3 task completed. Exit portal activated.");
        }
        else
        {
            Debug.LogWarning(
                "[Level3Boss] Exit portal is not assigned."
            );
        }
    }

    public void ResetBossState()
    {
        deathHandled = false;
        HideExitPortal();
    }

    private void HideExitPortal()
    {
        if (exitPortal != null)
        {
            exitPortal.SetActive(false);
        }
    }

    private void DebugBoss(string message)
    {
        if (!debugBoss)
        {
            return;
        }

        Debug.Log("[Level3Boss] " + message, this);
    }
}
