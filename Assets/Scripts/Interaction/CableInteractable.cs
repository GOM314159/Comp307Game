using Unity.Behavior;
using UnityEngine;
using UnityEngine.Video;

public class CableInteractable : WorldInteractable
{
    // =========================================================
    // CABLE
    // =========================================================

    [Header("Cable")]

    [Tooltip(
        "Whether the cable can currently be cut."
    )]
    [SerializeField]
    private bool canBeCut = true;


    [Tooltip(
        "Optional object to hide when the cable has been cut. " +
        "If left empty, this Cable GameObject will be hidden " +
        "after the cutscene finishes."
    )]
    [SerializeField]
    private GameObject objectToDisable;


    // =========================================================
    // LEVEL 2 CUTSCENE
    // =========================================================

    [Header("Level 2 Boss Cutscene")]

    [Tooltip(
        "The VideoPlayer used for the Level 2 boss/death cutscene. " +
        "Do NOT use TheaterPlayVideo here."
    )]
    [SerializeField]
    private VideoPlayer bossCutsceneVideo;


    // =========================================================
    // LEVEL 2 LAMPS
    // =========================================================

    [Header("Level 2 Lamps")]

    [Tooltip(
        "The lamp that is visible before the cable is cut."
    )]
    [SerializeField]
    private GameObject lamp2_0;


    [Tooltip(
        "The lamp that becomes visible after the cutscene."
    )]
    [SerializeField]
    private GameObject lamp3_0;


    // =========================================================
    // LEVEL 2 RETURN PORTAL
    // =========================================================

    [Header("Level 2 Return Portal")]

    [Tooltip(
        "The Level 2 return portal. It becomes visible " +
        "after the cutscene finishes."
    )]
    [SerializeField]
    private GameObject level2ReturnPortal;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugCable = true;

    //
    //
    //
    [Header("Level 2 Behaviour tree")]

    [Tooltip(
       "The Boss behaviour update in tree")]
    [SerializeField]
    private BehaviorGraphAgent behaviorGraphAgent;


    // =========================================================
    // STATE
    // =========================================================

    private bool cutscenePlaying = false;

    private bool sequenceFinished = false;


    // =========================================================
    // UNITY
    // =========================================================

    protected override void Awake()
    {
        base.Awake();

        // -----------------------------------------------------
        // RESTORE INITIAL LEVEL 2 STATE
        // -----------------------------------------------------

        RestoreLevel2InitialState();

        // -----------------------------------------------------
        // SET UP VIDEO PLAYER
        // -----------------------------------------------------

        SetupCutsceneVideo();
    }


    // =========================================================
    // VIDEO SETUP
    // =========================================================

    private void SetupCutsceneVideo()
    {
        if (bossCutsceneVideo == null)
        {
            Debug.LogWarning(
                "[Cable] Level 2 boss cutscene VideoPlayer " +
                "has not been assigned."
            );

            return;
        }


        // -----------------------------------------------------
        // VIDEO SETTINGS
        // -----------------------------------------------------

        bossCutsceneVideo.playOnAwake = false;

        bossCutsceneVideo.isLooping = false;

        bossCutsceneVideo.waitForFirstFrame = true;

        bossCutsceneVideo.skipOnDrop = false;


        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        SetupCutsceneCamera();


        // -----------------------------------------------------
        // EVENTS
        // -----------------------------------------------------

        bossCutsceneVideo.prepareCompleted +=
            OnCutscenePrepared;

        bossCutsceneVideo.loopPointReached +=
            OnCutsceneFinished;

        bossCutsceneVideo.errorReceived +=
            OnCutsceneError;


        // -----------------------------------------------------
        // MAKE SURE VIDEO IS NOT ALREADY PLAYING
        // -----------------------------------------------------

        bossCutsceneVideo.Stop();
    }


    // =========================================================
    // CAMERA SETUP
    // =========================================================

    private void SetupCutsceneCamera()
    {
        if (bossCutsceneVideo == null)
        {
            return;
        }


        Camera mainCamera =
            Camera.main;


        if (mainCamera == null)
        {
            Debug.LogWarning(
                "[Cable] Main Camera was not found while " +
                "setting up the Level 2 cutscene."
            );

            return;
        }


        // -----------------------------------------------------
        // RENDER VIDEO DIRECTLY TO CAMERA
        // -----------------------------------------------------

        bossCutsceneVideo.renderMode =
            VideoRenderMode.CameraNearPlane;

        bossCutsceneVideo.targetCamera =
            mainCamera;

        bossCutsceneVideo.targetCameraAlpha =
            1f;
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
        // PREVENT REPEATED INTERACTION
        // -----------------------------------------------------

        if (cutscenePlaying ||
            sequenceFinished)
        {
            return;
        }


        // -----------------------------------------------------
        // CHECK CABLE
        // -----------------------------------------------------

        if (!canBeCut)
        {
            return;
        }


        // -----------------------------------------------------
        // FIND PLAYER INVENTORY
        // -----------------------------------------------------

        PlayerInventory inventory =
            FindFirstObjectByType<PlayerInventory>();


        if (inventory == null)
        {
            Debug.LogError(
                "[Cable] PlayerInventory not found."
            );

            return;
        }


        // -----------------------------------------------------
        // GET SELECTED INVENTORY SLOT
        // -----------------------------------------------------

        InventorySlot selectedSlot =
            inventory.GetSelectedSlot();


        if (
            selectedSlot == null ||
            selectedSlot.IsEmpty()
        )
        {
            DebugCable(
                "No item equipped."
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK FOR CABLE CUTTERS
        // -----------------------------------------------------

        if (!(selectedSlot.item is CableCutterItem))
        {
            DebugCable(
                "You need a Cable Cutter to cut this cable."
            );

            return;
        }


        // -----------------------------------------------------
        // CUT CABLE
        // -----------------------------------------------------

        CutCable();
    }


    // =========================================================
    // CUT CABLE
    // =========================================================

    private void CutCable()
    {
        if (!canBeCut)
        {
            return;
        }


        canBeCut = false;


        DebugCable(
            "Cable has been cut."
        );

        // Stop the Level 2 music immediately when the cable is cut.
        // The following cutscene therefore begins in silence unless it
        // has its own audio/video audio track.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusicImmediate();
        }


        // -----------------------------------------------------
        // REGISTER NORMAL INTERACTION
        // -----------------------------------------------------

        base.Interact();


        // -----------------------------------------------------
        // REGISTER BOSS DEFEAT
        // -----------------------------------------------------

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.DefeatBoss();

            DebugCable(
                "Level 2 boss defeated."
            );
        }
        else
        {
            Debug.LogWarning(
                "[Cable] CaseManager.Instance is missing."
            );
        }
        if (behaviorGraphAgent != null)
        {
         behaviorGraphAgent.SetVariableValue(
          "ChandelierIsCut",
          true
         );
        }
        else
        {
            Debug.LogWarning(
                "[Cable] BehaviorGraphAgent is missing."
            );
        }


            Debug.Log("[Cable] Set ChandelierIsCut to true in the behavior graph agent's blackboard.");

        // -----------------------------------------------------
        // START CUTSCENE
        // -----------------------------------------------------

        StartBossCutscene();
    }


    // =========================================================
    // START CUTSCENE
    // =========================================================

    private void StartBossCutscene()
    {
        cutscenePlaying = true;


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.Cutscene
            );
        }


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Cutscene
            );
        }


        // -----------------------------------------------------
        // HIDE GAMEPLAY HUD
        // -----------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                false
            );
        }


        // -----------------------------------------------------
        // LOCK CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // -----------------------------------------------------
        // CHECK VIDEO
        // -----------------------------------------------------

        if (bossCutsceneVideo == null)
        {
            Debug.LogWarning(
                "[Cable] No Level 2 cutscene VideoPlayer " +
                "has been assigned."
            );

            FinishBossCutscene();

            return;
        }


        // -----------------------------------------------------
        // SET CAMERA
        // -----------------------------------------------------

        SetupCutsceneCamera();


        // -----------------------------------------------------
        // PREPARE VIDEO
        // -----------------------------------------------------

        bossCutsceneVideo.Stop();

        bossCutsceneVideo.Prepare();


        DebugCable(
            "Preparing Level 2 boss cutscene."
        );
    }


    // =========================================================
    // VIDEO PREPARED
    // =========================================================

    private void OnCutscenePrepared(
        VideoPlayer player)
    {
        if (!cutscenePlaying)
        {
            return;
        }


        DebugCable(
            "Level 2 boss cutscene prepared. Playing."
        );


        player.Play();
    }


    // =========================================================
    // VIDEO FINISHED
    // =========================================================

    private void OnCutsceneFinished(
        VideoPlayer player)
    {
        if (!cutscenePlaying)
        {
            return;
        }


        DebugCable(
            "Level 2 boss cutscene finished."
        );


        FinishBossCutscene();
    }


    // =========================================================
    // VIDEO ERROR
    // =========================================================

    private void OnCutsceneError(
        VideoPlayer player,
        string message)
    {
        Debug.LogError(
            "[Cable] Level 2 boss cutscene error: "
            + message
        );


        FinishBossCutscene();
    }


    private void FinishBossCutscene()
    {
        if (!cutscenePlaying)
        {
            return;
        }

        // ---------------------------------------------------------
        // STOP CUTSCENE VIDEO
        // ---------------------------------------------------------

        if (bossCutsceneVideo != null)
        {
            bossCutsceneVideo.Stop();

            // Remove the video from the camera immediately.
            bossCutsceneVideo.targetCameraAlpha = 0f;
        }


        cutscenePlaying = false;
        sequenceFinished = true;


        // ---------------------------------------------------------
        // HIDE CABLE
        // ---------------------------------------------------------

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }


        // ---------------------------------------------------------
        // LAMP 2 OFF
        // ---------------------------------------------------------

        if (lamp2_0 != null)
        {
            lamp2_0.SetActive(false);
        }


        // ---------------------------------------------------------
        // LAMP 3 ON
        // ---------------------------------------------------------

        if (lamp3_0 != null)
        {
            lamp3_0.SetActive(true);
        }


        // ---------------------------------------------------------
        // SHOW LEVEL 2 RETURN PORTAL
        // ---------------------------------------------------------

        if (level2ReturnPortal != null)
        {
            level2ReturnPortal.SetActive(true);

            DebugCable(
                "Level 2 return portal revealed."
            );
        }
        else
        {
            Debug.LogError(
                "[Cable] Level 2 Return Portal has not been assigned."
            );
        }


        // ---------------------------------------------------------
        // RESTORE GAME MODE
        // ---------------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetGameMode(
                GameStateManager.GameMode.Case
            );
        }


        // ---------------------------------------------------------
        // RESTORE PLAYER MOVEMENT
        // ---------------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.SetState(
                PlayerStateManager.PlayerState.AllMovement
            );
        }


        // ---------------------------------------------------------
        // RESTORE CURSOR
        // ---------------------------------------------------------

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;


        // ---------------------------------------------------------
        // SHOW GAMEPLAY HUD
        // ---------------------------------------------------------

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetGameplayHUDVisible(
                true
            );
        }


        DebugCable(
            "Level 2 cutscene finished. " +
            "Player control restored."
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    protected override void OnResetState()
    {
        base.OnResetState();


        // -----------------------------------------------------
        // RESET INTERNAL STATE
        // -----------------------------------------------------

        canBeCut = true;

        cutscenePlaying = false;

        sequenceFinished = false;


        // -----------------------------------------------------
        // STOP VIDEO
        // -----------------------------------------------------

        if (bossCutsceneVideo != null)
        {
            bossCutsceneVideo.Stop();
        }


        // -----------------------------------------------------
        // RESTORE LEVEL 2 OBJECTS
        // -----------------------------------------------------

        RestoreLevel2InitialState();


        DebugCable(
            "Level 2 cable sequence reset."
        );
    }


    // =========================================================
    // INITIAL LEVEL 2 STATE
    // =========================================================

    private void RestoreLevel2InitialState()
    {
        // -----------------------------------------------------
        // LAMP 2
        // -----------------------------------------------------

        if (lamp2_0 != null)
        {
            lamp2_0.SetActive(true);
        }


        // -----------------------------------------------------
        // LAMP 3
        // -----------------------------------------------------

        if (lamp3_0 != null)
        {
            lamp3_0.SetActive(false);
        }


        // -----------------------------------------------------
        // RETURN PORTAL
        // -----------------------------------------------------

        if (level2ReturnPortal != null)
        {
            level2ReturnPortal.SetActive(false);
        }
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public override string GetInteractionPrompt()
    {
        return "Cut Cable";
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugCable(string message)
    {
        if (!debugCable)
        {
            return;
        }

        Debug.Log(
            "[Cable] "
            + message
        );
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (bossCutsceneVideo == null)
        {
            return;
        }


        bossCutsceneVideo.prepareCompleted -=
            OnCutscenePrepared;

        bossCutsceneVideo.loopPointReached -=
            OnCutsceneFinished;

        bossCutsceneVideo.errorReceived -=
            OnCutsceneError;
    }
}