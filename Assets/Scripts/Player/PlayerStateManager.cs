using UnityEngine;
using System.Collections.Generic;

public class PlayerStateManager : MonoBehaviour
{
    // =========================================================
    // PLAYER STATES
    // =========================================================

    public enum PlayerState
    {
        AllMovement,

        NoCrouch,

        NoSprint,

        NoCrouchOrSprint,

        NoInteract,

        ShopMenu,

        Cutscene,

        NoMovement,

        NoJump,

        NoMovementNoCamera,

        PauseMenu,
        RIP,
        Reading,
        Inventory

    }


    // =========================================================
    // CURRENT STATE
    // =========================================================

    [Header("Current Player State")]

    [SerializeField]
    private PlayerState currentState = PlayerState.AllMovement;

    private List<PlayerStateZone> activeZones =
    new List<PlayerStateZone>();


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugStateChanges = true;


    // =========================================================
    // STATE GETTER
    // =========================================================

    public PlayerState GetCurrentState()
    {
        return currentState;
    }


    // =========================================================
    // CHANGE STATE
    // =========================================================

    public void SetState(PlayerState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (debugStateChanges)
        {
            Debug.Log(
                "[Player State] Changed to: "
                + currentState);
        }
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    public bool CanMove()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.NoJump:

                return true;


            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovement:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }


    // =========================================================
    // CAMERA
    // =========================================================
    public bool CanInteract()
    {
        switch (currentState)
        {
            // ---------------------------------------------
            // INTERACTION ALLOWED
            // ---------------------------------------------

            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.NoJump:

                return true;


            // ---------------------------------------------
            // INTERACTION DISABLED
            // ---------------------------------------------

            case PlayerState.NoInteract:
            case PlayerState.NoMovement:
            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }


    public bool CanOpenInventory()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.NoJump:
            case PlayerState.NoInteract:
            case PlayerState.Inventory:

                return true;

            case PlayerState.Reading:
            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovement:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.RIP:

                return false;
        }

        return false;
    }

    public bool CanAttack()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.NoJump:
                return true;

            case PlayerState.NoMovement:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.NoInteract:
            case PlayerState.PauseMenu:
            case PlayerState.Cutscene:
            case PlayerState.ShopMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:
                return false;

            default:
                return false;
        }
    }
    public bool CanLook()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.NoJump:
            case PlayerState.NoMovement:

                return true;


            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }


    // =========================================================
    // SPRINT
    // =========================================================

    public bool CanSprint()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoJump:
            case PlayerState.NoMovement:

                return true;


            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }


    // =========================================================
    // CROUCH
    // =========================================================

    public bool CanCrouch()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoSprint:
            case PlayerState.NoJump:
            case PlayerState.NoMovement:

                return true;


            case PlayerState.NoCrouch:
            case PlayerState.NoCrouchOrSprint:
            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }


    // =========================================================
    // JUMP
    // =========================================================

    public bool CanJump()
    {
        switch (currentState)
        {
            case PlayerState.AllMovement:
            case PlayerState.NoCrouch:
            case PlayerState.NoSprint:
            case PlayerState.NoCrouchOrSprint:

                return true;


            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.NoMovement:
            case PlayerState.NoJump:
            case PlayerState.NoMovementNoCamera:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.Inventory:

                return false;
        }

        return false;
    }

    public bool CanPause()
    {
        switch (currentState)
        {
            case PlayerState.NoInteract:
            case PlayerState.ShopMenu:
            case PlayerState.Cutscene:
            case PlayerState.PauseMenu:
            case PlayerState.Reading:
            case PlayerState.RIP:
            case PlayerState.Inventory:
                return false;

            default:
                return true;
        }
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    // =========================================================
    // REGISTER STATE ZONE
    // =========================================================

    public void RegisterZone(PlayerStateZone zone)
    {
        if (zone == null)
        {
            return;
        }

        // Prevent the same zone from being registered twice.
        if (activeZones.Contains(zone))
        {
            return;
        }

        activeZones.Add(zone);

        // The most recently entered zone takes priority.
        SetState(zone.GetState());

        Debug.Log(
            "[Player State] Entered zone: "
            + zone.GetState());
    }

    // =========================================================
    // UNREGISTER STATE ZONE
    // =========================================================

    public void UnregisterZone(PlayerStateZone zone)
    {
        if (zone == null)
        {
            return;
        }

        activeZones.Remove(zone);


        // =====================================================
        // ANOTHER ZONE IS STILL ACTIVE
        // =====================================================

        if (activeZones.Count > 0)
        {
            PlayerStateZone latestZone =
                activeZones[activeZones.Count - 1];

            SetState(latestZone.GetState());

            Debug.Log(
                "[Player State] Returned to zone: "
                + latestZone.GetState());

            return;
        }


        // =====================================================
        // NO ZONES ACTIVE
        // =====================================================

        SetState(PlayerState.AllMovement);

        Debug.Log(
            "[Player State] No active zones. "
            + "Returned to AllMovement.");
    }

    public void SetRIP()
    {
        SetState(PlayerState.RIP);
    }

    public bool IsRIP()
    {
        return currentState == PlayerState.RIP;
    }

    public void ResetToDefault()
    {
        activeZones.Clear();

        SetState(PlayerState.AllMovement);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public void DebugSetAllMovement()
    {
        SetState(PlayerState.AllMovement);
    }


    public void DebugSetNoCrouch()
    {
        SetState(PlayerState.NoCrouch);
    }


    public void DebugSetNoSprint()
    {
        SetState(PlayerState.NoSprint);
    }


    public void DebugSetNoInteract()
    {
        SetState(PlayerState.NoInteract);
    }

    public void DebugSetNoCrouchOrSprint()
    {
        SetState(PlayerState.NoCrouchOrSprint);
    }


    public void DebugSetShopMenu()
    {
        SetState(PlayerState.ShopMenu);
    }


    public void DebugSetCutscene()
    {
        SetState(PlayerState.Cutscene);
    }


    public void DebugSetNoMovement()
    {
        SetState(PlayerState.NoMovement);
    }


    public void DebugSetNoJump()
    {
        SetState(PlayerState.NoJump);
    }


    public void DebugSetNoMovementNoCamera()
    {
        SetState(PlayerState.NoMovementNoCamera);
    }


    public void DebugSetPauseMenu()
    {
        SetState(PlayerState.PauseMenu);
    }
}