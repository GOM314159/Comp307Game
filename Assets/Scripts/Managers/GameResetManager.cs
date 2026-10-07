using System.Collections.Generic;
using UnityEngine;


public class GameResetManager : MonoBehaviour
{
    public static GameResetManager Instance { get; private set; }


    // =========================================================
    // INVENTORY SNAPSHOT
    // =========================================================

    private List<PlayerInventory.InventorySnapshotSlot>
    dayInventorySnapshot =
        new List<PlayerInventory.InventorySnapshotSlot>();


    // =========================================================
    // PLAYER UPGRADE SNAPSHOTS
    // =========================================================

    private float gameStartSpeedMultiplier = 1f;
    private float gameStartStrength = 0f;
    private float gameStartMaxStamina = 100f;

    private float dayStartSpeedMultiplier = 1f;
    private float dayStartStrength = 0f;
    private float dayStartMaxStamina = 100f;

    private bool[] dayExodiaSnapshot =
        new bool[5];


    // =========================================================
    // CASE-ENTRY CHECKPOINT
    // =========================================================
    //
    // This is the exact state immediately BEFORE entering a
    // level/case portal. Death restores this checkpoint so items
    // collected inside the case (axe, Exodia pieces, etc.) cannot
    // leak into the next attempt.
    //
    // =========================================================

    private List<PlayerInventory.InventorySnapshotSlot>
    caseEntryInventorySnapshot =
        new List<PlayerInventory.InventorySnapshotSlot>();

    private float caseEntrySpeedMultiplier = 1f;
    private float caseEntryStrength = 0f;
    private float caseEntryMaxStamina = 100f;

    private bool[] caseEntryExodiaSnapshot =
        new bool[5];

    private bool caseEntrySnapshotCreated = false;


    // =========================================================
    // STATE
    // =========================================================

    private bool gameStartSnapshotCreated = false;
    private bool daySnapshotCreated = false;

    private int snapshotDay = -1;


    // =========================================================
    // UNITY
    // =========================================================

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


    // =========================================================
    // GAME START SNAPSHOT
    // =========================================================

    public void CreateGameStartSnapshot()
    {
        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();


        if (movement != null)
        {
            gameStartSpeedMultiplier =
                movement.GetPermanentSpeedMultiplier();

            gameStartStrength =
                movement.GetStrength();
        }


        if (stamina != null)
        {
            gameStartMaxStamina =
                stamina.MaxStamina;
        }


        gameStartSnapshotCreated = true;


        Debug.Log(
            "[Reset] Game-start snapshot created."
        );
    }


    public bool HasGameStartSnapshot()
    {
        return gameStartSnapshotCreated;
    }


    // =========================================================
    // DAY SNAPSHOT
    // =========================================================

    public void CreateDaySnapshot(bool forceOverwrite = false)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning(
                "[Reset] Cannot create day snapshot. " +
                "GameStateManager is missing."
            );

            return;
        }


        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();


        // -----------------------------------------------------
        // DAY
        // -----------------------------------------------------

        int currentDay =
            GameStateManager.Instance.GetCurrentDay();

        // A day-start snapshot must NEVER be overwritten while restarting
        // the same day. This is the key protection against a case item such
        // as the Day 1 axe becoming part of the death baseline.
        if (!forceOverwrite &&
            daySnapshotCreated &&
            snapshotDay == currentDay)
        {
            Debug.Log(
                "[Reset] Day " + currentDay +
                " snapshot already exists. Keeping the current checkpoint."
            );

            return;
        }

        snapshotDay = currentDay;


        // -----------------------------------------------------
        // INVENTORY
        // -----------------------------------------------------

        dayInventorySnapshot.Clear();


        if (inventory != null)
        {
            dayInventorySnapshot =
                inventory.CreateSnapshot();
        }
        else
        {
            dayInventorySnapshot.Clear();
        }


        // -----------------------------------------------------
        // PLAYER UPGRADES
        // -----------------------------------------------------

        if (movement != null)
        {
            dayStartSpeedMultiplier =
                movement.GetPermanentSpeedMultiplier();

            dayStartStrength =
                movement.GetStrength();
        }


        if (stamina != null)
        {
            dayStartMaxStamina =
                stamina.MaxStamina;
        }


        // -----------------------------------------------------
        // EXODIA
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            dayExodiaSnapshot =
                GameStateManager.Instance
                    .GetExodiaCollectionSnapshot();
        }


        daySnapshotCreated = true;


        Debug.Log(
            "[Reset] Day "
            + snapshotDay
            + " snapshot created."
        );
    }


    // =========================================================
    // RESET CURRENT DAY
    // =========================================================

    public void ResetCurrentDay()
    {
        Debug.Log(
            "[Reset] Resetting current day."
        );


        // -----------------------------------------------------
        // RESTORE THE EXACT CHECKPOINT FOR THIS ATTEMPT
        // -----------------------------------------------------
        //
        // If the player is/was inside a case, the case-entry
        // checkpoint is the exact state from before entering the
        // level portal. Otherwise use the latest day checkpoint
        // (which is updated whenever a case is successfully returned).
        // -----------------------------------------------------

        if (caseEntrySnapshotCreated)
        {
            RestoreCaseEntrySnapshot();
            ClearCaseEntrySnapshot();

            Debug.Log(
                "[Reset] Restored the pre-case-entry checkpoint."
            );
        }
        else
        {
            RestoreDayInventory();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance
                    .RestoreExodiaCollectionSnapshot(
                        dayExodiaSnapshot
                    );
            }

            RestoreDayPlayerUpgrades();

            Debug.Log(
                "[Reset] Restored the latest day checkpoint."
            );
        }

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.ResetForDay(
                GameStateManager.Instance.GetCurrentDay()
            );
        }

        ResetWorldInteractables();


        ResetBosses();


        JumpscareTrigger.ResetAllJumpscares();


        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.ResetForDay();
        }


        // -----------------------------------------------------
        // RESET GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetCurrentDay();

            GameStateManager.Instance.ClearLossState();
        }


        // -----------------------------------------------------
        // RESET BEHAVIOUR TREE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetBehaviourTree();
        }


        Debug.Log(
            "[Reset] Current day reset complete."
        );
    }


    // =========================================================
    // RESET ENTIRE GAME
    // =========================================================

    public void ResetEntireGame()
    {
        Debug.Log(
            "[Reset] Resetting entire game."
        );


        // -----------------------------------------------------
        // RESET INVENTORY
        // -----------------------------------------------------

        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();


        if (inventory != null)
        {
            inventory.ClearInventory();
        }


        // -----------------------------------------------------
        // RESET PLAYER HEALTH
        // -----------------------------------------------------

        PlayerHealth playerHealth =
            FindAnyObjectByType<PlayerHealth>(
                FindObjectsInactive.Include
            );

        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }


        // -----------------------------------------------------
        // RESET PLAYER UPGRADES
        // -----------------------------------------------------

        RestoreGameStartPlayerUpgrades();


        // -----------------------------------------------------
        // RESET WORLD INTERACTABLES
        // -----------------------------------------------------

        ResetWorldInteractables();


        // -----------------------------------------------------
        // RESET BOSSES
        // -----------------------------------------------------

        ResetBosses();


        // -----------------------------------------------------
        // RESET EXODIA
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetExodiaCollection();
        }


        // -----------------------------------------------------
        // RESET CASE MANAGER
        // -----------------------------------------------------

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.ResetForGame();
        }


        // -----------------------------------------------------
        // RESET THEATER
        // -----------------------------------------------------

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.ResetForGame();
        }


        // -----------------------------------------------------
        // CLEAR SNAPSHOTS
        // -----------------------------------------------------

        dayInventorySnapshot.Clear();

        daySnapshotCreated = false;

        snapshotDay = -1;

        ClearCaseEntrySnapshot();


        // -----------------------------------------------------
        // RESET PORTAL PROGRESSION
        // -----------------------------------------------------

        if (PortalProgressionManager.Instance != null)
        {
            PortalProgressionManager.Instance.ResetForGame();
        }


        // -----------------------------------------------------
        // RESET GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetEntireGame();
        }


        gameStartSnapshotCreated = false;

        gameStartSpeedMultiplier = 1f;
        gameStartStrength = 0f;
        gameStartMaxStamina = 100f;


        Debug.Log(
            "[Reset] Entire game reset complete."
        );
    }


    // =========================================================
    // CASE ENTRY CHECKPOINT
    // =========================================================

    /// <summary>
    /// Captures the exact player progression immediately before
    /// entering a case/level portal.
    /// </summary>
    public void CreateCaseEntrySnapshot()
    {
        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();

        caseEntryInventorySnapshot.Clear();

        if (inventory != null)
        {
            caseEntryInventorySnapshot =
                inventory.CreateSnapshot();
        }

        if (movement != null)
        {
            caseEntrySpeedMultiplier =
                movement.GetPermanentSpeedMultiplier();

            caseEntryStrength =
                movement.GetStrength();
        }

        if (stamina != null)
        {
            caseEntryMaxStamina =
                stamina.MaxStamina;
        }

        if (GameStateManager.Instance != null)
        {
            caseEntryExodiaSnapshot =
                GameStateManager.Instance
                    .GetExodiaCollectionSnapshot();
        }

        caseEntrySnapshotCreated = true;

        Debug.Log(
            "[Reset] Pre-case-entry checkpoint captured."
        );
    }

    public bool HasCaseEntrySnapshot()
    {
        return caseEntrySnapshotCreated;
    }

    public void ClearCaseEntrySnapshot()
    {
        caseEntryInventorySnapshot.Clear();

        caseEntrySpeedMultiplier = 1f;
        caseEntryStrength = 0f;
        caseEntryMaxStamina = 100f;

        caseEntryExodiaSnapshot =
            new bool[5];

        caseEntrySnapshotCreated = false;
    }

    private void RestoreCaseEntrySnapshot()
    {
        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        if (inventory != null)
        {
            inventory.RestoreSnapshot(
                caseEntryInventorySnapshot
            );
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance
                .RestoreExodiaCollectionSnapshot(
                    caseEntryExodiaSnapshot
                );
        }

        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        if (movement != null)
        {
            movement.SetPermanentSpeedMultiplier(
                caseEntrySpeedMultiplier
            );

            movement.SetPermanentStrength(
                caseEntryStrength
            );
        }

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();

        if (stamina != null)
        {
            stamina.SetMaxStamina(
                caseEntryMaxStamina
            );
        }
    }


    // =========================================================
    // INVENTORY RESTORE
    // =========================================================

    private void RemoveCurrentDayCaseItem()
    {
        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        if (inventory == null || GameStateManager.Instance == null)
            return;

        int day = GameStateManager.Instance.GetCurrentDay();
        ItemData caseItem = FindCaseItemForDay(day);

        if (caseItem == null)
            return;

        if (inventory.RemoveItemData(caseItem))
        {
            Debug.Log(
                "[Reset] Removed Day " + day +
                " case item from inventory after death: " +
                caseItem.itemName
            );
        }
    }

    private ItemData FindCaseItemForDay(int day)
    {
        if (day == 1)
        {
            AxeItem[] items =
                Resources.FindObjectsOfTypeAll<AxeItem>();

            if (items.Length > 0)
                return items[0];
        }
        else if (day == 2)
        {
            CableCutterItem[] items =
                Resources.FindObjectsOfTypeAll<CableCutterItem>();

            if (items.Length > 0)
                return items[0];
        }
        else if (day == 3)
        {
            GunItem[] items =
                Resources.FindObjectsOfTypeAll<GunItem>();

            if (items.Length > 0)
                return items[0];
        }

        return null;
    }

    private void RestoreDayInventory()
    {
        if (!daySnapshotCreated)
        {
            Debug.LogWarning(
                "[Reset] No day inventory snapshot exists."
            );

            return;
        }


        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();


        if (inventory == null)
        {
            Debug.LogWarning(
                "[Reset] PlayerInventory not found."
            );

            return;
        }


        inventory.RestoreSnapshot(
            dayInventorySnapshot
        );


        Debug.Log(
            "[Reset] Day inventory restored."
        );
    }


    // =========================================================
    // PLAYER UPGRADE RESTORE
    // =========================================================

    private void RestoreDayPlayerUpgrades()
    {
        if (!daySnapshotCreated)
        {
            return;
        }


        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();


        if (movement != null)
        {
            movement.SetPermanentSpeedMultiplier(
                dayStartSpeedMultiplier
            );

            movement.SetPermanentStrength(
                dayStartStrength
            );
        }


        if (stamina != null)
        {
            stamina.SetMaxStamina(
                dayStartMaxStamina
            );
        }
    }


    public void ApplyDaySnapshotToCurrentPlayer()
    {
        if (!daySnapshotCreated)
        {
            Debug.LogWarning(
                "[Reset] No day snapshot exists to apply."
            );

            return;
        }


        Debug.Log(
            "[Reset] Applying beginning-of-day snapshot "
            + "to current Player."
        );


        RestoreDayInventory();

        RestoreDayPlayerUpgrades();
    }


    private void RestoreGameStartPlayerUpgrades()
    {
        PlayerMovement movement =
            FindAnyObjectByType<PlayerMovement>();

        PlayerStamina stamina =
            FindAnyObjectByType<PlayerStamina>();


        if (movement != null)
        {
            movement.SetPermanentSpeedMultiplier(
                gameStartSpeedMultiplier
            );

            movement.SetPermanentStrength(
                gameStartStrength
            );
        }


        if (stamina != null)
        {
            stamina.SetMaxStamina(
                gameStartMaxStamina
            );
        }
    }


    // =========================================================
    // WORLD RESET
    // =========================================================

    private void ResetWorldInteractables()
    {
        WorldInteractable[] interactables =
            FindObjectsByType<WorldInteractable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (
            WorldInteractable interactable
            in interactables
        )
        {
            if (interactable == null)
            {
                continue;
            }


            interactable.ResetInteraction();
        }


        Debug.Log(
            "[Reset] World interactables reset: "
            + interactables.Length
        );
    }


    // =========================================================
    // BOSS RESET
    // =========================================================

    private void ResetBosses()
    {
        // -----------------------------------------------------
        // ORIGINAL BOSS SYSTEM
        // -----------------------------------------------------

        BossDeath[] bosses =
            FindObjectsByType<BossDeath>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (
            BossDeath boss
            in bosses
        )
        {
            if (boss == null)
            {
                continue;
            }


            Health health =
                boss.GetComponent<Health>();


            if (health != null)
            {
                health.ResetHealth();
            }


            boss.ResetBossState();
        }
   
    }


    // =========================================================
    // DAY CHECKPOINT
    // =========================================================

    public void CommitCurrentProgressAsDayCheckpoint()
    {
        Debug.Log(
            "[Reset] Committing current progress "
            + "as the new day checkpoint."
        );

        // Force the overwrite. A normal CreateDaySnapshot call protects
        // the original day-start snapshot from accidental recapture.
        CreateDaySnapshot(true);

        // Once the case has been successfully returned from, its
        // pre-entry checkpoint is no longer needed.
        ClearCaseEntrySnapshot();
    }
}