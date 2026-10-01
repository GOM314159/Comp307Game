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

    [Tooltip("The AI Behavior Agent.")]
    [SerializeField]
    private BehaviorGraphAgent agent;


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
                "Behavior graph agent updated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[Cable Cutter] Behavior Graph Agent is not assigned."
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