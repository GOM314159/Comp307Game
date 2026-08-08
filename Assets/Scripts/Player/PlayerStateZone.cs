using UnityEngine;

public class PlayerStateZone : MonoBehaviour
{
    // =========================================================
    // ZONE SETTINGS
    // =========================================================

    [Header("Player State")]

    [SerializeField]
    private PlayerStateManager.PlayerState state =
        PlayerStateManager.PlayerState.NoSprint;


    [Header("Debug")]

    [SerializeField]
    private bool debugZone = true;


    // =========================================================
    // GET STATE
    // =========================================================

    public PlayerStateManager.PlayerState GetState()
    {
        return state;
    }


    // =========================================================
    // PLAYER ENTERS ZONE
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        PlayerStateManager manager =
            other.GetComponent<PlayerStateManager>();

        if (manager == null)
        {
            return;
        }

        manager.RegisterZone(this);

        if (debugZone)
        {
            Debug.Log(
                "[PlayerStateZone] Entered: "
                + state);
        }
    }


    // =========================================================
    // PLAYER LEAVES ZONE
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerStateManager manager =
            other.GetComponent<PlayerStateManager>();

        if (manager == null)
        {
            return;
        }

        manager.UnregisterZone(this);

        if (debugZone)
        {
            Debug.Log(
                "[PlayerStateZone] Exited: "
                + state);
        }
    }
}