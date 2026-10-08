using Unity.Behavior;
using UnityEngine;

public class CableCutterInteractable : WorldInteractable
{
    // =========================================================
    // CABLE CUTTER
    // =========================================================

    [Header("Cable Cutter")]

    [Tooltip(
        "The Cable Cutter item given to the player."
    )]
    [SerializeField]
    private CableCutterItem cableCutterItem;


    [Tooltip(
        "Player inventory that receives the Cable Cutters."
    )]
    [SerializeField]
    private PlayerInventory playerInventory;

    [Tooltip("The MiniBoss AI")]
    [SerializeField]
    private BehaviorGraphAgent agent;

    [Tooltip("The mini AI")]
    [SerializeField]
    private BehaviorGraphAgent agentMini;

    [Tooltip("The Miniboss")]
    [SerializeField]
    private GameObject miniboss;


    // =========================================================
    // LEVEL 2 THEATER
    // =========================================================

    [Header("Level 2 Theater")]

    [Tooltip(
        "The normal PuppetShow Theater that is visible " +
        "before the Cable Cutters are collected."
    )]
    [SerializeField]
    private GameObject theaterOriginal;


    [Tooltip(
        "The destroyed PuppetShow Theater that becomes " +
        "visible after the Cable Cutters are collected."
    )]
    [SerializeField]
    private GameObject theaterDestroyed;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugCableCutter = true;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();


        // -----------------------------------------------------
        // INITIAL THEATER STATE
        // -----------------------------------------------------

        RestoreTheaterInitialState();
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    /// <summary>
    /// 
    /// </summary>
    public override void Interact()
    {
        if (!CanInteract())
        {
            return;
        }


        // -----------------------------------------------------
        // CHECK ITEM
        // -----------------------------------------------------

        if (cableCutterItem == null)
        {
            Debug.LogError(
                "[Cable Cutter] Cable Cutter Item has not " +
                "been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK INVENTORY
        // -----------------------------------------------------
        if (playerInventory == null)
        {
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        }
        if (playerInventory == null)
        {
            Debug.LogError(
                "[Cable Cutter] Player Inventory has not " +
                "been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // ADD TO INVENTORY
        // -----------------------------------------------------

        bool added =
            playerInventory.AddItem(
                cableCutterItem,
                1
            );


        // -----------------------------------------------------
        // INVENTORY FULL
        // -----------------------------------------------------

        if (!added)
        {
            DebugCableCutter(
                "Could not pick up Cable Cutters. " +
                "Inventory is full."
            );

            return;
        }


        // -----------------------------------------------------
        // CHANGE LEVEL 2 MUSIC
        // -----------------------------------------------------

        // The moment the Cable Cutters are successfully collected,
        // switch to the second Level 2 music cue.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Level2AfterCable
            );
        }


        // -----------------------------------------------------
        // COMPLETE INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // CHANGE THEATER
        // -----------------------------------------------------

        if (theaterOriginal != null)
        {
            theaterOriginal.SetActive(false);

            DebugCableCutter(
                "Original PuppetShow Theater hidden."
            );
        }
        else
        {
            Debug.LogWarning(
                "[Cable Cutter] Theater Original is not assigned."
            );
        }


        if (theaterDestroyed != null)
        {
            theaterDestroyed.SetActive(true);

            DebugCableCutter(
                "Destroyed PuppetShow Theater revealed."
            );
        }
        else
        {
            Debug.LogWarning(
                "[Cable Cutter] Theater Destroyed is not assigned."
            );
        }


        // -----------------------------------------------------
        // REMOVE PICKUP
        // -----------------------------------------------------

        DebugCableCutter(
            "Cable Cutters picked up."
        );


        //
        // SET ACTIVE DOLL STATE ON
        //

        if (miniboss != null)
        {
            miniboss.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "[Cable Cutter] Miniboss is not assigned."
            );
        }


        // -----------------------------------------------------
        // Update the behavior graph agent to reflect the new state
        // -----------------------------------------------------

        if (agent != null)
        {
            agent.SetVariableValue(
                "HasCableCutter",
                true
            );

            DebugCableCutter(
                "Behavior graph agents updated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[Cable Cutter] Behavior Graph Agent Miniboss is not assigned."
            );
        }
            gameObject.SetActive(false);
    }


    // =========================================================
    // RESET
    // =========================================================

    protected override void OnResetState()
    {
        base.OnResetState();


        RestoreTheaterInitialState();

        // If the level is restarted/reset, return to the pre-Cable-Cutter
        // music cue. CaseManager will also set this when the case starts.
        if (AudioManager.Instance != null &&
            CaseManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(
                AudioManager.MusicType.Level2BeforeCable
            );
        }


        DebugCableCutter(
            "Cable Cutter and theater state reset."
        );
    }


    // =========================================================
    // RESTORE INITIAL THEATER
    // =========================================================

    private void RestoreTheaterInitialState()
    {
        if (theaterOriginal != null)
        {
            theaterOriginal.SetActive(true);
        }


        if (theaterDestroyed != null)
        {
            theaterDestroyed.SetActive(false);
        }
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Pick Up Cable Cutters";
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCableCutter(string message)
    {
        if (!debugCableCutter)
        {
            return;
        }

        Debug.Log(
            "[Cable Cutter] "
            + message
        );
    }
}