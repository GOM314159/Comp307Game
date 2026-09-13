using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles resetting gameplay state.
///
/// DAY RESET:
/// - Restores inventory to the beginning of the current day.
/// - Restores player permanent upgrades to the beginning of the day.
/// - Resets CaseManager progress.
/// - Resets world interactables.
/// - Keeps the current day.
/// - GameFlowManager then returns the player to the lobby.
///
/// FULL GAME RESET:
/// - Clears inventory.
/// - Restores player upgrades to their original game-start values.
/// - Resets case progress.
/// - Resets world interactables.
/// - Resets the game progression.
/// </summary>
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

    /// <summary>
    /// Creates the original game-state snapshot.
    ///
    /// This should happen when a completely new game starts,
    /// after the player has been initialized.
    /// </summary>
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

    /// <summary>
    /// Saves the player's exact inventory and permanent upgrades
    /// at the beginning of a day.
    ///
    /// This is what allows us to undo only what happened during
    /// that particular day.
    /// </summary>
    public void CreateDaySnapshot()
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

        snapshotDay =
            GameStateManager.Instance.GetCurrentDay();


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

    /// <summary>
    /// Restores the player and gameplay to the beginning of
    /// the current day.
    ///
    /// The day number itself does NOT change.
    /// </summary>
    public void ResetCurrentDay()
    {
        Debug.Log(
            "[Reset] Resetting current day."
        );


        // -----------------------------------------------------
        // RESET INVENTORY
        // -----------------------------------------------------

        RestoreDayInventory();


        // -----------------------------------------------------
        // RESET PLAYER UPGRADES
        // -----------------------------------------------------

        RestoreDayPlayerUpgrades();


        // -----------------------------------------------------
        // RESET WORLD
        // -----------------------------------------------------

        ResetWorldInteractables();

        ResetBosses();

        // -----------------------------------------------------
        // RESET CASE
        // -----------------------------------------------------

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


        Debug.Log(
            "[Reset] Current day reset complete."
        );
    }


    // =========================================================
    // RESET ENTIRE GAME
    // =========================================================

    /// <summary>
    /// Completely resets gameplay progression.
    ///
    /// This does NOT choose difficulty or gender.
    /// Those should be selected again by the Main Menu.
    /// </summary>
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
        // RESET PLAYER UPGRADES
        // -----------------------------------------------------

        RestoreGameStartPlayerUpgrades();


        // -----------------------------------------------------
        // RESET WORLD
        // -----------------------------------------------------

        ResetWorldInteractables();

        ResetBosses();


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
    // INVENTORY RESTORE
    // =========================================================

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

    private void ResetBosses()
    {
        BossDeath[] bosses =
            FindObjectsByType<BossDeath>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BossDeath boss in bosses)
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

        Debug.Log(
            "[Reset] Bosses reset: "
            + bosses.Length
        );
    }
}