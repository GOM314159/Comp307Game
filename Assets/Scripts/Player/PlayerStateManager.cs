using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateManager : MonoBehaviour
{
    // =========================================
    // PLAYER MODES
    // =========================================
    // These describe WHY the player's controls
    // may currently be restricted.
    public enum PlayerMode
    {
        Normal,
        ShopMenu,
        Cutscene,
        NoCrouchZone,
        NoSprintZone,
        NoCrouchOrSprintZone,
        PauseMenu,
        NoMovementZone,
        NoJumpZone,
        NoMovementOrCameraZone
    }


    // =========================================
    // MOVEMENT STATES
    // =========================================
    // These describe WHAT the player is doing.
    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching,
        Jumping,
        Falling
    }


    // =========================================
    // CURRENT STATES
    // =========================================

    private PlayerMode currentMode = PlayerMode.Normal;

    private MovementState currentMovementState =
        MovementState.Idle;


    // =========================================
    // REFERENCES
    // =========================================

    [Header("References")]

    [SerializeField]
    private PlayerMovement playerMovement;


    // =========================================
    // DEBUG
    // =========================================

    [Header("Debug")]

    [SerializeField]
    private bool debugStateChanges = true;


    // =========================================
    // STATE CHANGE TRACKING
    // =========================================

    private PlayerMode previousMode;

    private MovementState previousMovementState;


    // =========================================
    // UNITY
    // =========================================

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        previousMode = currentMode;
        previousMovementState = currentMovementState;
    }


    private void Update()
    {
        DetermineMovementState();

        CheckForStateChanges();

        // TEMPORARY TESTING
        TestStates();
    }


    // =========================================
    // DETERMINE MOVEMENT STATE
    // =========================================

    private void DetermineMovementState()
    {
        // -------------------------------------
        // PLAYER CANNOT MOVE
        // -------------------------------------

        if (!CanMove())
        {
            currentMovementState = MovementState.Idle;
            return;
        }


        // -------------------------------------
        // AIRBORNE
        // -------------------------------------

        if (!playerMovement.IsGrounded())
        {
            currentMovementState = MovementState.Falling;
            return;
        }


        // -------------------------------------
        // CROUCHING
        // -------------------------------------

        if (playerMovement.IsCrouching())
        {
            currentMovementState = MovementState.Crouching;
            return;
        }


        // -------------------------------------
        // SPRINTING
        // -------------------------------------

        if (CanSprint()
            && playerMovement.IsSprinting()
            && playerMovement.IsMoving())
        {
            currentMovementState = MovementState.Sprinting;
            return;
        }


        // -------------------------------------
        // WALKING
        // -------------------------------------

        if (playerMovement.IsMoving())
        {
            currentMovementState = MovementState.Walking;
            return;
        }


        // -------------------------------------
        // IDLE
        // -------------------------------------

        currentMovementState = MovementState.Idle;
    }


    // =========================================
    // CONTROL PERMISSIONS
    // =========================================

    public bool CanMove()
    {
        switch (currentMode)
        {
            case PlayerMode.ShopMenu:
            case PlayerMode.Cutscene:
            case PlayerMode.PauseMenu:
            case PlayerMode.NoMovementZone:
            case PlayerMode.NoMovementOrCameraZone:

                return false;
        }

        return true;
    }


    public bool CanLook()
    {
        switch (currentMode)
        {
            case PlayerMode.ShopMenu:
            case PlayerMode.Cutscene:
            case PlayerMode.PauseMenu:
            case PlayerMode.NoMovementOrCameraZone:

                return false;
        }

        return true;
    }


    public bool CanJump()
    {
        switch (currentMode)
        {
            case PlayerMode.ShopMenu:
            case PlayerMode.Cutscene:
            case PlayerMode.PauseMenu:
            case PlayerMode.NoMovementZone:
            case PlayerMode.NoJumpZone:
            case PlayerMode.NoMovementOrCameraZone:

                return false;
        }

        return true;
    }


    public bool CanCrouch()
    {
        switch (currentMode)
        {
            case PlayerMode.ShopMenu:
            case PlayerMode.Cutscene:
            case PlayerMode.PauseMenu:
            case PlayerMode.NoCrouchZone:
            case PlayerMode.NoCrouchOrSprintZone:
            case PlayerMode.NoMovementZone:
            case PlayerMode.NoMovementOrCameraZone:

                return false;
        }

        return true;
    }


    public bool CanSprint()
    {
        switch (currentMode)
        {
            case PlayerMode.ShopMenu:
            case PlayerMode.Cutscene:
            case PlayerMode.PauseMenu:
            case PlayerMode.NoSprintZone:
            case PlayerMode.NoCrouchOrSprintZone:
            case PlayerMode.NoMovementZone:
            case PlayerMode.NoMovementOrCameraZone:

                return false;
        }

        return true;
    }


    // =========================================
    // CHANGE PLAYER MODE
    // =========================================

    public void SetPlayerMode(PlayerMode newMode)
    {
        if (currentMode == newMode)
            return;

        currentMode = newMode;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Player Mode] Changed to: "
                + currentMode);
        }
    }


    // =========================================
    // RETURN TO NORMAL
    // =========================================

    public void ReturnToNormal()
    {
        SetPlayerMode(PlayerMode.Normal);
    }


    // =========================================
    // GET CURRENT MODE
    // =========================================

    public PlayerMode GetPlayerMode()
    {
        return currentMode;
    }


    // =========================================
    // GET MOVEMENT STATE
    // =========================================

    public MovementState GetMovementState()
    {
        return currentMovementState;
    }


    // =========================================
    // STATE CHECKS
    // =========================================

    public bool IsNormal()
    {
        return currentMode == PlayerMode.Normal;
    }


    public bool IsInShopMenu()
    {
        return currentMode == PlayerMode.ShopMenu;
    }


    public bool IsInCutscene()
    {
        return currentMode == PlayerMode.Cutscene;
    }


    public bool IsPaused()
    {
        return currentMode == PlayerMode.PauseMenu;
    }


    public bool IsInNoCrouchZone()
    {
        return currentMode == PlayerMode.NoCrouchZone;
    }


    public bool IsInNoSprintZone()
    {
        return currentMode == PlayerMode.NoSprintZone;
    }


    public bool IsInNoCrouchOrSprintZone()
    {
        return currentMode ==
               PlayerMode.NoCrouchOrSprintZone;
    }


    public bool IsInNoMovementZone()
    {
        return currentMode ==
               PlayerMode.NoMovementZone;
    }


    public bool IsInNoJumpZone()
    {
        return currentMode ==
               PlayerMode.NoJumpZone;
    }


    public bool IsInNoMovementOrCameraZone()
    {
        return currentMode ==
               PlayerMode.NoMovementOrCameraZone;
    }


    // =========================================
    // STATE CHANGE DEBUGGING
    // =========================================

    private void CheckForStateChanges()
    {
        if (currentMode != previousMode)
        {
            if (debugStateChanges)
            {
                Debug.Log(
                    "[Player Mode] "
                    + previousMode
                    + " → "
                    + currentMode);
            }

            previousMode = currentMode;
        }


        if (currentMovementState !=
            previousMovementState)
        {
            if (debugStateChanges)
            {
                Debug.Log(
                    "[Movement State] "
                    + previousMovementState
                    + " → "
                    + currentMovementState);
            }

            previousMovementState =
                currentMovementState;
        }
    }

    private void TestStates()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.Normal);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoCrouchZone);
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoSprintZone);
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoCrouchOrSprintZone);
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoMovementZone);
        }

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoJumpZone);
        }

        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.NoMovementOrCameraZone);
        }

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.ShopMenu);
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.Cutscene);
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            SetPlayerMode(PlayerMode.PauseMenu);
        }
    }
}