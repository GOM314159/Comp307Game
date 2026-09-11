using UnityEngine;

/// <summary>
/// Defines the position and rotation where the player should spawn
/// when entering gameplay or respawning after death.
///
/// This object is the single source of truth for the player's
/// gameplay spawn position.
/// </summary>
public class PlayerRespawnPoint : MonoBehaviour
{
    // =========================================================
    // SPAWN PLAYER
    // =========================================================

    public void SpawnPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning(
                "[Respawn] Cannot spawn player because player is null."
            );

            return;
        }

        // -----------------------------------------------------
        // CHARACTER CONTROLLER
        // -----------------------------------------------------

        CharacterController controller =
            player.GetComponent<CharacterController>();

        // Disable CharacterController before manually moving
        // the player. This prevents it from interfering with
        // the teleport.
        if (controller != null)
        {
            controller.enabled = false;
        }

        // -----------------------------------------------------
        // MOVE PLAYER
        // -----------------------------------------------------

        player.transform.SetPositionAndRotation(
            transform.position,
            transform.rotation
        );

        // -----------------------------------------------------
        // RE-ENABLE CHARACTER CONTROLLER
        // -----------------------------------------------------

        if (controller != null)
        {
            controller.enabled = true;
        }

        Debug.Log(
            "[Respawn] Player moved to PlayerRespawnPoint."
        );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawSphere(
            transform.position,
            0.25f
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            transform.forward * 1.5f
        );
    }
}