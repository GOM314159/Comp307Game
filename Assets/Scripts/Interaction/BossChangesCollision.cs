using UnityEngine;

/// <summary>
/// Makes the physical objects inside BossChanges block the player's
/// CharacterController. Put this on the BossChanges root.
/// </summary>
public class BossChangesCollision : MonoBehaviour
{
    [Header("Collision")]
    [Tooltip("Automatically add MeshColliders to child MeshFilters that have no Collider.")]
    [SerializeField] private bool automaticallyCreateMeshColliders = true;

    [Tooltip("Force every existing collider under BossChanges to be non-trigger.")]
    [SerializeField] private bool forceNonTrigger = true;

    [Tooltip("Enable every collider under BossChanges when the root becomes active.")]
    [SerializeField] private bool forceCollidersEnabled = true;

    [Tooltip("Log every collider that is prepared. Useful while setting up the seals.")]
    [SerializeField] private bool debugCollisionSetup = false;

    private void Awake()
    {
        SetupColliders();
    }

    private void OnEnable()
    {
        SetupColliders();
    }

    [ContextMenu("Setup Boss Change Colliders")]
    public void SetupColliders()
    {
        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        int prepared = 0;

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null)
                continue;

            Collider[] existing = child.GetComponents<Collider>();

            for (int c = 0; c < existing.Length; c++)
            {
                Collider collider = existing[c];
                if (collider == null)
                    continue;

                if (forceNonTrigger)
                    collider.isTrigger = false;

                if (forceCollidersEnabled)
                    collider.enabled = true;

                prepared++;

                if (debugCollisionSetup)
                {
                    Debug.Log(
                        "[BossChangesCollision] Prepared collider on " +
                        child.name + ". Trigger=" + collider.isTrigger,
                        child
                    );
                }
            }

            if (!automaticallyCreateMeshColliders)
                continue;

            MeshFilter meshFilter =
                child.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (child.GetComponent<Collider>() != null)
                continue;

            MeshCollider meshCollider =
                child.gameObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshCollider.enabled = true;

            prepared++;

            if (debugCollisionSetup)
            {
                Debug.Log(
                    "[BossChangesCollision] Added MeshCollider to " +
                    child.name,
                    child
                );
            }
        }

        if (debugCollisionSetup)
        {
            Debug.Log(
                "[BossChangesCollision] Setup complete. Prepared " +
                prepared + " collider(s).",
                this
            );
        }
    }
}
