using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class ControllerLightManager : MonoBehaviour
{
    public static ControllerLightManager Instance { get; private set; }

    // =========================================================
    // LIGHT COLOURS
    // =========================================================

    [Header("Light Colours")]

    [SerializeField]
    private Color greenColour = Color.green;

    [SerializeField]
    private Color yellowColour = Color.yellow;

    [SerializeField]
    private Color redColour = Color.red;

    [SerializeField]
    private Color purpleColour = new Color(
        0.6f,
        0.0f,
        1.0f
    );

    [SerializeField]
    private Color blueColour = Color.blue;


    // =========================================================
    // CONTROLLER
    // =========================================================

    private DualShockGamepad playStationController;


    // =========================================================
    // INITIALIZATION
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

        FindController();
    }


    private void OnEnable()
    {
        SubscribeToEvents();
    }


    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }


    // =========================================================
    // EVENT SUBSCRIPTION
    // =========================================================

    private void SubscribeToEvents()
    {
        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameModeChanged +=
                HandleGameModeChanged;

            GameStateManager.Instance.OnObjectiveChanged +=
                HandleObjectiveChanged;

            GameStateManager.Instance.OnBossDefeated +=
                HandleBossDefeated;
        }


        // -----------------------------------------------------
        // THEATER
        // -----------------------------------------------------

        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.OnPlayCompleted +=
                HandlePlayCompleted;
        }


        // -----------------------------------------------------
        // CASE
        // -----------------------------------------------------

        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.OnCaseStarted +=
                HandleCaseStarted;

            CaseManager.Instance.OnBossDefeated +=
                HandleCaseBossDefeated;
        }
    }


    private void UnsubscribeFromEvents()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameModeChanged -=
                HandleGameModeChanged;

            GameStateManager.Instance.OnObjectiveChanged -=
                HandleObjectiveChanged;

            GameStateManager.Instance.OnBossDefeated -=
                HandleBossDefeated;
        }


        if (TheaterManager.Instance != null)
        {
            TheaterManager.Instance.OnPlayCompleted -=
                HandlePlayCompleted;
        }


        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.OnCaseStarted -=
                HandleCaseStarted;

            CaseManager.Instance.OnBossDefeated -=
                HandleCaseBossDefeated;
        }
    }


    // =========================================================
    // CONTROLLER
    // =========================================================

    private void FindController()
    {
        playStationController =
            DualShockGamepad.current;

        if (playStationController != null)
        {
            Debug.Log(
                "ControllerLightManager: PlayStation controller detected - "
                + playStationController.displayName
            );
        }
        else
        {
            Debug.Log(
                "ControllerLightManager: No supported PlayStation controller detected."
            );
        }
    }


    // =========================================================
    // SET COLOUR
    // =========================================================

    public void SetColour(Color colour)
    {
        if (playStationController == null)
        {
            FindController();
        }

        if (playStationController == null)
        {
            return;
        }

        playStationController.SetLightBarColor(colour);

        Debug.Log(
            "Controller light changed to: "
            + colour
        );
    }


    // =========================================================
    // PRESET COLOURS
    // =========================================================

    public void SetGreen()
    {
        SetColour(greenColour);
    }


    public void SetYellow()
    {
        SetColour(yellowColour);
    }


    public void SetRed()
    {
        SetColour(redColour);
    }


    public void SetPurple()
    {
        SetColour(purpleColour);
    }


    public void SetBlue()
    {
        SetColour(blueColour);
    }


    // =========================================================
    // PLAY FINISHED
    // =========================================================

    private void HandlePlayCompleted()
    {
        // -----------------------------------------------------
        // Only use green for a play that leads to a case.
        // -----------------------------------------------------

        if (TheaterManager.Instance == null)
        {
            return;
        }

        if (!TheaterManager.Instance.HasCaseAfterCurrentPlay())
        {
            return;
        }

        SetGreen();
    }


    // =========================================================
    // CASE STARTED
    // =========================================================

    private void HandleCaseStarted()
    {
        SetYellow();
    }


    // =========================================================
    // OBJECTIVE CHANGED
    // =========================================================

    private void HandleObjectiveChanged(
        GameStateManager.ObjectiveState objective)
    {
        switch (objective)
        {
            case GameStateManager.ObjectiveState.FindObject:

                SetYellow();

                break;


            case GameStateManager.ObjectiveState.BossUnlocked:

                SetRed();

                break;


            case GameStateManager.ObjectiveState.BossDefeated:

                SetPurple();

                break;


            case GameStateManager.ObjectiveState.Complete:

                SetPurple();

                break;
        }
    }


    // =========================================================
    // BOSS DEFEATED
    // =========================================================

    private void HandleBossDefeated()
    {
        SetPurple();
    }


    private void HandleCaseBossDefeated()
    {
        SetPurple();
    }


    // =========================================================
    // GAME MODE
    // =========================================================

    private void HandleGameModeChanged(
        GameStateManager.GameMode mode)
    {
        switch (mode)
        {
            case GameStateManager.GameMode.BossFight:

                SetRed();

                break;
        }
    }


    // =========================================================
    // RETURN PORTAL
    // =========================================================

    public void OnReturnPortalEntered()
    {
        SetBlue();
    }
}