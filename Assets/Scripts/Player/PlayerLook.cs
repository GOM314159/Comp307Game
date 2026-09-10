using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{

    [Header("Camera Settings")]

    // Drag the Main Camera here
    [SerializeField] private Transform cameraTransform;

    // Adjust mouse/controller sensitivity here
    [SerializeField] private float sensitivity = 2f;

    // Adjust the maximum up/down look angle here
    [SerializeField] private float maxLookAngle = 80f;

    private PlayerControls controls;

    // Stores mouse/right stick movement
    private Vector2 lookInput;

    // Change vertical rotation
    private float xRotation = 0f;

    private PlayerStateManager stateManager;

    private void Awake()
    {
        controls = new PlayerControls();
        stateManager = GetComponent<PlayerStateManager>();

        if (stateManager == null)
        {
            Debug.LogError(
                "[PlayerLook] PlayerStateManager is missing from the Player!");
        }

        controls.Player.Look.performed += ctx =>
            lookInput = ctx.ReadValue<Vector2>();

        controls.Player.Look.canceled += _ =>
            lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Lock cursor to the centre of the screen
        //Cursor.lockState = CursorLockMode.Locked;
        // Was breaking MainMenu, but altered to work so calma
        // Hide the cursor
        //Cursor.visible = false;
    }

    private void Update()
    {
        Look();
    }

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

        xRotation = Mathf.Clamp(
            xRotation,
            -maxLookAngle,
            maxLookAngle);


        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f);


        // =====================================
        // HORIZONTAL PLAYER ROTATION
        // =====================================

        transform.Rotate(
            Vector3.up * mouseX);
    }
}