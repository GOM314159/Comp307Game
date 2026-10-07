using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    // =========================================================
    // CAMERA SETTINGS
    // =========================================================

    [Header("Camera Settings")]

    // Drag the Main Camera here.
    [SerializeField]
    private Transform cameraTransform;

    // Default FOV when a new game starts.
    // White Powder can permanently change this.
    [SerializeField]
    private float defaultFOV = 45f;

    // Current FOV used by the player camera.
    [SerializeField]
    private float currentFOV = 45f;

    // Adjust mouse/controller sensitivity here.
    [SerializeField]
    private float sensitivity = 2f;

    // Adjust the maximum up/down look angle here.
    [SerializeField]
    private float maxLookAngle = 50f;


    // =========================================================
    // INPUT
    // =========================================================

    private PlayerControls controls;

    // Stores mouse/right stick movement.
    private Vector2 lookInput;


    // =========================================================
    // ROTATION
    // =========================================================

    private float xRotation = 0f;


    // =========================================================
    // PLAYER STATE
    // =========================================================

    private PlayerStateManager stateManager;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        controls = new PlayerControls();

        stateManager =
            GetComponent<PlayerStateManager>();

        if (stateManager == null)
        {
            Debug.LogError(
                "[PlayerLook] PlayerStateManager is missing from the Player!"
            );
        }

        controls.Player.Look.performed +=
            ctx =>
                lookInput =
                    ctx.ReadValue<Vector2>();

        controls.Player.Look.canceled +=
            _ =>
                lookInput =
                    Vector2.zero;


        // -----------------------------------------------------
        // DEFAULT FOV
        // -----------------------------------------------------

        currentFOV =
            defaultFOV;

        // Load the shared settings value. The SettingsMenu can change
        // this at runtime, but the player also needs the value when
        // spawning after a scene change.
        sensitivity =
            PlayerPrefs.GetFloat(
                "Settings.CameraSensitivity",
                sensitivity);

        ApplyFOV();
    }


    private void OnEnable()
    {
        controls.Enable();
    }


    private void OnDisable()
    {
        controls.Disable();
    }

    // CHANGED: Update() changed to LateUpdate() to prevent conflict with Animator
    private void LateUpdate()
    {
        Look();
    }


    // =========================================================
    // LOOK
    // =========================================================

    private void Look()
    {
        // =====================================
        // CAMERA LOOK PERMISSION
        // =====================================

        if (!stateManager.CanLook())
        {
            return;
        }


        // =====================================
        // LOOK INPUT
        // =====================================

        float mouseX =
            lookInput.x *
            sensitivity *
            Time.deltaTime *
            100f;

        float mouseY =
            lookInput.y *
            sensitivity *
            Time.deltaTime *
            100f;


        // =====================================
        // VERTICAL CAMERA ROTATION
        // =====================================

        xRotation -= mouseY;

        xRotation =
            Mathf.Clamp(
                xRotation,
                -maxLookAngle,
                maxLookAngle
            );

        // This correctly rotates the camera locally relative to the head bone's current animated position
        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                1.5f
            );


        // =====================================
        // HORIZONTAL PLAYER ROTATION
        // =====================================

        transform.Rotate(
            Vector3.up * mouseX
        );
    }


    // =========================================================
    // SENSITIVITY SETTINGS
    // =========================================================

    public float GetSensitivity()
    {
        return sensitivity;
    }

    public void SetSensitivity(float value)
    {
        sensitivity = Mathf.Max(0.01f, value);

        PlayerPrefs.SetFloat(
            "Settings.CameraSensitivity",
            sensitivity);

        PlayerPrefs.Save();
    }


    // =========================================================
    // PERMANENT FOV
    // =========================================================

    public void ApplyPermanentFOV(
        float newFOV
    )
    {
        currentFOV =
            Mathf.Max(
                1f,
                newFOV
            );

        ApplyFOV();


        Debug.Log(
            "[PlayerLook] Permanent FOV changed to "
            + currentFOV
        );
    }


    private void ApplyFOV()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning(
                "[PlayerLook] Camera Transform is not assigned."
            );

            return;
        }

        Camera playerCamera =
            cameraTransform.GetComponent<Camera>();

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "[PlayerLook] Camera Transform does not contain a Camera component."
            );

            return;
        }

        playerCamera.fieldOfView =
            currentFOV;
    }


    // =========================================================
    // RESET LOOK
    // =========================================================

    public void ResetLook()
    {
        xRotation = 0f;

        lookInput =
            Vector2.zero;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.identity;
        }

        // Keep the current permanent FOV.
        ApplyFOV();
    }
}