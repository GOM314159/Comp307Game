using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class ControllerLightManager : MonoBehaviour
{
    public static ControllerLightManager Instance { get; private set; }

    [Header("Light Colours")]
    [SerializeField] private Color normalColour = Color.white;
    [SerializeField] private Color dangerColour = Color.red;

    private DualShockGamepad playStationController;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        // Make sure only one ControllerLightManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep the manager between scenes
        DontDestroyOnLoad(gameObject);

        FindController();
    }

    // =========================================================
    // FIND CONTROLLER
    // =========================================================

    private void FindController()
    {
        playStationController = DualShockGamepad.current;

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
                "ControllerLightManager: No supported PlayStation controller detected. "
                + "Light commands will be ignored."
            );
        }
    }

    // =========================================================
    // SET COLOUR
    // =========================================================

    public void SetColour(Color colour)
    {
        // Try to find the controller again if one wasn't detected
        // when the game started.
        if (playStationController == null)
        {
            FindController();
        }

        // No PS4/PS5 controller detected.
        // Do nothing so the game continues normally.
        if (playStationController == null)
        {
            return;
        }

        playStationController.SetLightBarColor(colour);

        Debug.Log("Controller light changed to: " + colour);
    }

    // =========================================================
    // PRESET COMMANDS
    // =========================================================

    public void SetRed()
    {
        SetColour(dangerColour);
    }

    public void SetNormal()
    {
        SetColour(normalColour);
    }
}