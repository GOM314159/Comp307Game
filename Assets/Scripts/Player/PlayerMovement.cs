using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;  // Adjust walking speed
    [SerializeField] private float sprintSpeed = 8f; // Adjust sprint speed
    [SerializeField] private float crouchSpeed = 2.5f; // Adjust croutch speed

    [Header("Jump Settings")]

    // Adjust jump height here
    [SerializeField] private float jumpHeight = 1.5f;

    // Adjust upward gravity here
    [SerializeField] private float gravity = -20f;

    // Extra gravity while falling
    // Higher values make falling feel heavier
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f; // Adjust ground distance
    [SerializeField] private LayerMask groundMask;

    [Header("Ceiling Check")]
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private float ceilingDistance = 0.3f;  // Adjust ceiling detection distance here
    [SerializeField] private LayerMask ceilingMask;  // Select which layers block standing up

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f; // CharacterController height while standing
    [SerializeField] private float crouchingHeight = 1.2f; // CharacterController height while crouching
    [SerializeField] private float standingCameraHeight = 1.7f;  // Camera height while standing
    [SerializeField] private float crouchingCameraHeight = 1.1f;  // Camera height while crouching
    [SerializeField] private float crouchTransitionSpeed = 8f;  // How quickly the transition happens

    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private PlayerControls controls;

    private Vector2 moveInput;

    private Vector3 velocity;

    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    private float targetHeight;
    private float targetCameraHeight;

    private PlayerStamina stamina;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        controls = new PlayerControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        stamina = GetComponent<PlayerStamina>();

        controls.Player.Sprint.performed += _ => isSprinting = true;
        controls.Player.Sprint.canceled += _ => isSprinting = false;

        targetHeight = standingHeight;
        targetCameraHeight = standingCameraHeight;

        controls.Player.Croutch.performed += _ => isCrouching = !isCrouching;

        controls.Player.Jump.performed += _ => Jump();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        CheckGround();

        Move();

        ApplyGravity();

        HandleCrouch();
    }

    private void Move()
    {
        float speed = stamina.GetMovementSpeed(
        walkSpeed,
        sprintSpeed,
        crouchSpeed,
        isCrouching);

        Vector3 direction =
        transform.right * moveInput.x +
        transform.forward * moveInput.y;

        controller.Move(direction * speed * Time.deltaTime);


        stamina.SetSprinting(
        isSprinting &&
        moveInput != Vector2.zero &&
        !isCrouching);

    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
    }

    private bool IsCeilingBlocked()
    {
        return Physics.CheckSphere(
            ceilingCheck.position,
            ceilingDistance,
            ceilingMask);
    }

    private void ApplyGravity()
    {
        if (velocity.y < 0)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (!isGrounded)
            return;

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void HandleCrouch()
    {
        // If the player wants to stand up but something is above them,
        // keep them crouched.
        if (!isCrouching && IsCeilingBlocked())
        {
            targetHeight = crouchingHeight;
            targetCameraHeight = crouchingCameraHeight;
        }
        else
        {
            targetHeight = isCrouching ? crouchingHeight : standingHeight;
            targetCameraHeight = isCrouching ? crouchingCameraHeight : standingCameraHeight;
        }

        // Smoothly change the CharacterController height
        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime);

        // Keep the CharacterController sitting on the ground
        controller.center = new Vector3(
            0,
            controller.height / 2f,
            0);

        // Smoothly move the camera
        Vector3 cameraPosition = cameraTransform.localPosition;

        cameraPosition.y = Mathf.Lerp(
            cameraPosition.y,
            targetCameraHeight,
            crouchTransitionSpeed * Time.deltaTime);

        cameraTransform.localPosition = cameraPosition;
    }

    private void OnDrawGizmosSelected() //Debugging code for ground and ceiling detection
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                groundCheck.position,
                groundDistance);
        }

        if (ceilingCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(
                ceilingCheck.position,
                ceilingDistance);
        }
    }
}