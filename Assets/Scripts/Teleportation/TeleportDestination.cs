using UnityEngine;

public class TeleportDestination : MonoBehaviour
{
    [Header("Spawn Settings")]

    [Tooltip("Optional offset applied to the player's spawn position.")]
    [SerializeField]
    private Vector3 spawnOffset = Vector3.zero;


    // =========================================================
    // GETTERS
    // =========================================================

    public Vector3 GetSpawnPosition()
    {
        return transform.position + spawnOffset;
    }

    public Quaternion GetSpawnRotation()
    {
        return transform.rotation;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawSphere(
            transform.position,
            0.25f
        );

        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward
        );
    }
}