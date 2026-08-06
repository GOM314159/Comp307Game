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

    private void Awake()
    {
        controls = new PlayerControls();

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
        Cursor.lockState = CursorLockMode.Locked;

        // Hide the cursor
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
    }

    private void Look()
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime * 100f;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime * 100f;

        // Vertical camera rotation
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(
            xRotation,
            -maxLookAngle,
            maxLookAngle);

        cameraTransform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal player rotation
        transform.Rotate(Vector3.up * mouseX);
    }
}