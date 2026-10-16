using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;  // Adjust walking speed
    [SerializeField] private float sprintSpeed = 8f; // Adjust sprint speed
    [SerializeField] private float crouchSpeed = 2.5f; // Adjust croutch speed

    [Header("Permanent Speed Boosts")]
    [SerializeField] private float permanentSpeedMultiplier = 1f;

    [Header("Permanent Strength")]

    [SerializeField]
    private float permanentStrength = 0f;

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

    [Header("Main Menu Safety")]
    [Tooltip("Optional point where the persistent player is placed while the Main Menu is active. This prevents an old gameplay position from remaining in the scene.")]
    [SerializeField] private Transform mainMenuSafePoint;


    private CharacterController controller;
    private PlayerControls controls;
    private string currentAnimState = "";

    private Vector2 moveInput;

    private Vector3 velocity;

    private Vector3 horizontalVelocity;

    private bool isGrounded;
    private bool isSprinting;
    private bool isCrouching;

    private float targetHeight;
    private float targetCameraHeight;

    private PlayerStamina stamina;

    private PlayerStateManager stateManager;
    private bool mainMenuPositionApplied = false;

    [Tooltip("Animator component for handling player animations.")]
    [SerializeField]
    private Animator animator;

    [Header("Equipment Anim")]

    [SerializeField]
    private PlayerEquipmentManager equipmentManager;

    [Header("Gun Settings")]
    [SerializeField]
    private WeaponItem pistolItem;

    private PlayerStateManager playerStateManager; 
[SerializeField]
    private PlayerAttackController playerAttackController;

    [Header("Death Animations")]
    [SerializeField] private PlayerHealth playerHealth;

    private bool deathAnimationPlaying = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        stamina = GetComponent<PlayerStamina>();
        stateManager = GetComponent<PlayerStateManager>();

        controls.Player.Sprint.performed += _ => StartSprint();
        controls.Player.Sprint.canceled += _ => StopSprint();

        targetHeight = standingHeight;
        targetCameraHeight = standingCameraHeight;

        controls.Player.Croutch.performed += _ => ToggleCrouch();

        controls.Player.Jump.performed += _ => Jump();
    }

    private void OnEnable()
    {
        controls.Enable();

        if (playerAttackController != null)
        {
            playerAttackController.OnAttackPerformed += TriggerAttackAnimation;
        }
    }

    private void OnDisable()
    {
        controls.Disable();

        if (playerAttackController != null)
        {
            playerAttackController.OnAttackPerformed -= TriggerAttackAnimation;
        }
    }

    public void setAnimator(Animator genderAnimator)
    {

        animator = genderAnimator;

    }

    private void Update()
    {
        // The player object persists across scenes, including the Main Menu.
        // In the Main Menu / Loading state there may be no ground collider at
        // all, so applying CharacterController gravity would make the player
        // fall thousands of units below the scene.
        if (IsMenuOrLoadingState())
        {
            if (IsMainMenuState())
            {
                MoveToMainMenuSafePointOnce();
            }

            velocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            isSprinting = false;
            isCrouching = false;
            return;
        }

        // Never apply gravity while a cutscene/theater play owns player control.
        // Previously the movement script kept applying gravity even when input
        // was locked, allowing the persistent player to fall out of the map.
        if ((GameStateManager.Instance != null &&
             GameStateManager.Instance.CurrentGameMode == GameStateManager.GameMode.Cutscene) ||
            (stateManager != null &&
             stateManager.GetCurrentState() == PlayerStateManager.PlayerState.Cutscene))
        {
            velocity = Vector3.zero;
            moveInput = Vector2.zero;
            isSprinting = false;
            if (controller != null && controller.enabled)
                controller.Move(Vector3.zero);
            return;
        }

        mainMenuPositionApplied = false;

        if (deathAnimationPlaying ||
    (playerHealth != null && playerHealth.IsDead()))
        {
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            horizontalVelocity = Vector3.zero;
            return;
        }

        CheckGround();

        Move();

        ApplyGravity();

        HandleCrouch();

        // =====================================
        // FORCE RESTRICTIONS
        // =====================================

        // If sprinting becomes disabled while
        // already sprinting, immediately stop it.
        if (!stateManager.CanSprint())
        {
            isSprinting = false;
        }

        // If crouching becomes disabled while
        // already crouching, attempt to stand.
        if (!stateManager.CanCrouch())
        {
            isCrouching = false;
        }
    }


    private bool IsMainMenuState()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.CurrentGameMode == GameStateManager.GameMode.MainMenu;
    }

    private void MoveToMainMenuSafePointOnce()
    {
        if (mainMenuPositionApplied)
            return;

        if (mainMenuSafePoint != null && controller != null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;

            transform.SetPositionAndRotation(
                mainMenuSafePoint.position,
                mainMenuSafePoint.rotation);

            controller.enabled = wasEnabled;
        }

        mainMenuPositionApplied = true;
    }

    private bool IsMenuOrLoadingState()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.GameMode mode =
                GameStateManager.Instance.CurrentGameMode;

            if (mode == GameStateManager.GameMode.MainMenu ||
                mode == GameStateManager.GameMode.Loading)
            {
                return true;
            }
        }

        if (stateManager != null &&
            stateManager.GetCurrentState() == PlayerStateManager.PlayerState.MainMenu)
        {
            return true;
        }

        return false;
    }

    private void StartSprint()
    {
        // =====================================
        // SPRINT PERMISSION
        // =====================================

        if (!stateManager.CanSprint())
        {
            Debug.Log(
                "[Player] Sprinting is currently disabled.");

            isSprinting = false; 
            
            if (animator != null)
            {
                animator.SetTrigger("Walk");
            }
            else
            {
                Debug.LogWarning(
                    "[PlayerMovement] Animator component is missing. Walk animation will not play.");
            }

            return;
        }

        // =====================================
        // START SPRINT
        // =====================================
        isSprinting = true;

        if (animator != null)
        {
            animator.SetTrigger("Run");
        }
        else
        {
            Debug.LogWarning(
                "[PlayerMovement] Animator component is missing. Sprint animation will not play.");
        }
    }

    private void ToggleCrouch()
    {
        // =====================================
        // CROUCH PERMISSION
        // =====================================

        if (!stateManager.CanCrouch())
        {
            Debug.Log(
                "[Player] Crouching is currently disabled.");

            return;
        }

        if (isCrouching)
        {
            animator.SetTrigger("Stand");
        }
        else
        {
            animator.SetTrigger("Crouch");
        }

            isCrouching = !isCrouching;
    }

    private void StopSprint()
    {
        isSprinting = false;
    }

    private void Move()
    {
        // =====================================
        // MOVEMENT LOCK
        // =====================================

        if (deathAnimationPlaying ||
    (playerHealth != null && playerHealth.IsDead()))
        {
            moveInput = Vector2.zero;
            horizontalVelocity = Vector3.zero;
            stamina.SetSprinting(false);
            return;
        }

        if (!stateManager.CanMove())
        {
            // Stop horizontal movement
            // when movement is disabled.
            horizontalVelocity = Vector3.zero;
            Vector3 lockedVelocity = controller.velocity;

            lockedVelocity.y = 0f;

            controller.Move(
                -lockedVelocity * Time.deltaTime);

            // Sprinting must also stop.
            stamina.SetSprinting(false);

            if (animator != null)
            {
                animator.SetTrigger("Idle");
            }

            return;
        }

        // =====================================
        // SPRINTING 
        // =====================================

        bool canPlayerSprint = isSprinting &&
                               IsMoving() &&
                               !isCrouching &&
                               stateManager.CanSprint();

        stamina.SetSprinting(canPlayerSprint);

        // =====================================
        // MOVEMENT SPEED
        // =====================================

        float speed = stamina.GetMovementSpeed(
            walkSpeed,
            sprintSpeed,
            crouchSpeed,
            isCrouching);


        speed *= permanentSpeedMultiplier;

        // =====================================
        // MOVEMENT DIRECTION
        // =====================================

        Vector3 direction =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;


        controller.Move(
            direction * speed * Time.deltaTime);
        horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;


        if (animator != null)
        {
            WeaponItem equippedWeapon =
                equipmentManager != null
                    ? equipmentManager.GetCurrentWeapon()
                    : null;

            bool holdingGun =
                equippedWeapon != null &&
                equippedWeapon == pistolItem;

            if (holdingGun)
            {
                // Gun-specific animations
                if (IsMoving() && isCrouching)
                {
                    ChangeAnimationState("CrouchWalk");
                }
                else if (isCrouching)
                {
                    ChangeAnimationState("Crouch");
                }
                else if (canPlayerSprint)
                {
                    ChangeAnimationState("Run");
                }
                else if (IsMoving())
                {
                    ChangeAnimationState("Walk");
                }
                else
                {
                    ChangeAnimationState("HasGun");
                }
            }
            else if (equippedWeapon != null)
            {
                // Melee weapon animations
                if (IsMoving() && isCrouching)
                {
                    ChangeAnimationState("CrouchWalk");
                }
                else if (isCrouching)
                {
                    ChangeAnimationState("Crouch");
                }
                else if (canPlayerSprint)
                {
                    ChangeAnimationState("Run");
                }
                else if (IsMoving())
                {
                    ChangeAnimationState("Walk");
                }
                else
                {
                    ChangeAnimationState("HasWeaponIdle");
                }
            }
            else
            {
                // No weapon equipped
                if (IsMoving() && isCrouching)
                {
                    ChangeAnimationState("CrouchWalk");
                }
                else if (isCrouching)
                {
                    ChangeAnimationState("Crouch");
                }
                else if (canPlayerSprint)
                {
                    ChangeAnimationState("Run");
                }
                else if (IsMoving())
                {
                    ChangeAnimationState("Walk");
                }
                else
                {
                    ChangeAnimationState("Idle");
                }
            }
        }


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
        // If the top of the capsule hits a ceiling while moving up, kill the jump instantly
        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && velocity.y > 0)
        {
            velocity.y = 0f;
        }

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
        // =====================================
        // JUMP PERMISSION
        // =====================================
        if (!stateManager.CanJump())
        {
            return;
        }

        // =====================================
        // GROUND CHECK
        // =====================================
        if (!isGrounded)
        {
            return;
        }

        // =====================================
        // JUMP
        // =====================================
        animator.SetTrigger("Jump");

        // Start the safe Unity Coroutine instead of a background thread
        StartCoroutine(JumpRoutine());
    }

    // Add this new coroutine directly below your Jump() method
    private System.Collections.IEnumerator JumpRoutine()
    {
        // Wait 0.1 seconds for the animation anticipation before jumping
        yield return new WaitForSeconds(0.1f);

        // Apply jump velocity safely on the main thread
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
     //   Vector3 cameraPosition = cameraTransform.localPosition;

     //   cameraPosition.y = Mathf.Lerp(
     //       cameraPosition.y,
     //       targetCameraHeight,
     //       crouchTransitionSpeed * Time.deltaTime);

     //   cameraTransform.localPosition = cameraPosition;
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

    // Headbobbing functions
    public Vector3 GetHorizontalVelocity()
    {
        return horizontalVelocity;
    }

    // Used by the footstep system to determine
    // whether the player is actually trying to move.
    public bool IsMoving()
    {
        return moveInput.sqrMagnitude > 0.01f;
    }

    public bool IsCrouching()
    {
        return isCrouching;
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void ApplyPermanentSpeedBoost(float multiplier)
    {
        if (multiplier <= 0f)
        {
            Debug.LogWarning(
                "[PlayerMovement] Invalid speed multiplier: " + multiplier
            );

            return;
        }

        permanentSpeedMultiplier *= multiplier;

        Debug.Log(
            "[PlayerMovement] Permanent speed boost applied."
            + " | Boost: x" + multiplier
            + " | Current multiplier: x" + permanentSpeedMultiplier
        );
    }

    public void ApplyPermanentStrengthBoost(float amount)
    {
        if (amount <= 0f)
        {
            Debug.LogWarning(
                "[PlayerMovement] Invalid strength increase: "
                + amount
            );

            return;
        }

        permanentStrength += amount;

        Debug.Log(
            "[PlayerMovement] Permanent strength boost applied."
            + " | Increase: +"
            + amount
            + " | Current strength: "
            + permanentStrength
        );
    }

    public float GetStrength()
    {
        return permanentStrength;
    }

    // =========================================================
    // RESET SUPPORT
    // =========================================================

    public float GetPermanentSpeedMultiplier()
    {
        return permanentSpeedMultiplier;
    }


    public void SetPermanentSpeedMultiplier(
        float multiplier)
    {
        if (multiplier <= 0f)
        {
            multiplier = 1f;
        }

        permanentSpeedMultiplier =
            multiplier;
    }


    public void SetPermanentStrength(
        float strength)
    {
        if (strength < 0f)
        {
            strength = 0f;
        }

        permanentStrength =
            strength;
    }

    public void ResetMovementAfterTeleport()
    {
        // Stop all previous movement.
        velocity = Vector3.zero;

        // Stop sprinting.
        isSprinting = false;

        // Return to standing.
        isCrouching = false;

        targetHeight = standingHeight;
        targetCameraHeight = standingCameraHeight;

        // Reset CharacterController height.
        if (controller != null)
        {
            controller.height = standingHeight;
        }

        // Reset camera height.
     //  if (cameraTransform != null)
     //   {
     //       Vector3 cameraPosition =
     //           cameraTransform.localPosition;
     //
     //       cameraPosition.y =
     //           standingCameraHeight;
     //
     //       cameraTransform.localPosition =
     //           cameraPosition;
     //   }

        Debug.Log(
            "[PlayerMovement] Movement reset after teleport."
        );
    }

    private void TriggerAttackAnimation()
    {

        if (deathAnimationPlaying ||
        (playerHealth != null && playerHealth.IsDead()))
            return;

        if (animator == null || equipmentManager == null)
            return;

        WeaponItem equippedWeapon =
            equipmentManager.GetCurrentWeapon();

        if (equippedWeapon == null)
            return;

        // Gun attack
        if (equippedWeapon == pistolItem)
        {
            animator.SetTrigger("FireGun");
        }
        // Melee attack
        else
        {
            animator.SetTrigger("SwingWeapon");
        }
    }



    public void PlayDeathAnimation()
    {
        if (deathAnimationPlaying)
            return;

        if (animator == null)
        {
            Debug.LogWarning(
                "[PlayerMovement] No Animator assigned for death.");
            return;
        }

        deathAnimationPlaying = true;

        // Stop movement input
        moveInput = Vector2.zero;
        horizontalVelocity = Vector3.zero;
        velocity = Vector3.zero;
        isSprinting = false;

        // Clear existing animation triggers
        if (!string.IsNullOrEmpty(currentAnimState))
        {
            animator.ResetTrigger(currentAnimState);
        }

        animator.ResetTrigger("SwingWeapon");
        animator.ResetTrigger("FireGun");

        animator.ResetTrigger("Death 1");
        animator.ResetTrigger("Death 2");
        animator.ResetTrigger("Death 3");

        // Randomly choose a death animation
        int deathNumber = Random.Range(1, 4);

        string deathTrigger = "Death " + deathNumber;

        currentAnimState = deathTrigger;

        animator.SetTrigger(deathTrigger);

        Debug.Log(
            "[PlayerMovement] Playing death animation: " +
            deathTrigger
        );
    }

    public void ResetDeathAnimation()
    {
        deathAnimationPlaying = false;
        currentAnimState = "";

        if (animator != null)
        {
            animator.ResetTrigger("Death1");
            animator.ResetTrigger("Death2");
            animator.ResetTrigger("Death3");
        }
    }


    private void ChangeAnimationState(string newState)
    {

        if (deathAnimationPlaying)
            return;

        // Prevent spamming the same trigger every frame
        if (currentAnimState == newState) return;

        // Reset the previous trigger so it doesn't queue up and fire twice
        if (!string.IsNullOrEmpty(currentAnimState))
        {
            animator.ResetTrigger(currentAnimState);
        }

        // Set the new trigger and update the tracker
        animator.SetTrigger(newState);
        currentAnimState = newState;
    }
}