using UnityEngine;

public class BossDeath : MonoBehaviour
{
    // =========================================================
    // BOSS
    // =========================================================

    [Header("Boss")]

    [Tooltip(
        "Health component belonging to this boss."
    )]
    [SerializeField]
    private Health bossHealth;


    // =========================================================
    // RETURN PORTAL
    // =========================================================

    [Header("Return Portal")]

    [Tooltip(
        "Portal that becomes visible after this boss dies."
    )]
    [SerializeField]
    private GameObject returnPortal;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugBossDeath = true;


    // =========================================================
    // STATE
    // =========================================================

    private bool deathRegistered;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (bossHealth == null)
        {
            bossHealth =
                GetComponent<Health>();
        }

        // -----------------------------------------------------
        // HIDE RETURN PORTAL AT START
        // -----------------------------------------------------

        if (returnPortal != null)
        {
            returnPortal.SetActive(false);
        }
    }


    private void OnEnable()
    {
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


    // =========================================================
    // BOSS DEATH
    // =========================================================

    private void HandleBossDied()
    {
        if (deathRegistered)
        {
            return;
        }

        deathRegistered = true;


        // -----------------------------------------------------
        // REGISTER WITH CASE MANAGER
        // -----------------------------------------------------

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.DefeatBoss();

            DebugBoss(
                "Boss death registered with CaseManager."
            );
        }
        else
        {
            Debug.LogError(
                "[BossDeath] CaseManager.Instance "
                + "is missing."
            );
        }


        // -----------------------------------------------------
        // SHOW RETURN PORTAL
        // -----------------------------------------------------

        if (returnPortal != null)
        {
            returnPortal.SetActive(true);

            DebugBoss(
                "Return portal activated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[BossDeath] No return portal assigned."
            );
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetBossState()
    {
        deathRegistered = false;

        if (returnPortal != null)
        {
            returnPortal.SetActive(false);
        }

        DebugBoss(
            "Boss death state reset."
        );
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugBoss(string message)
    {
        if (!debugBossDeath)
        {
            return;
        }

        Debug.Log(
            "[BossDeath] "
            + message
        );
    }
}