using UnityEngine;

public class BossNoAttackZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerStateZone stateZone;

    [SerializeField]
    private Collider zoneCollider;

    [SerializeField]
    private PlayerInventory playerInventory;


    [Header("Axe")]
    [SerializeField]
    private AxeItem requiredAxe;


    [Header("Debug")]
    [SerializeField]
    private bool debugZone = true;


    private bool zoneActive;


    private void Awake()
    {
        if (stateZone == null)
        {
            stateZone =
                GetComponent<PlayerStateZone>();
        }

        if (zoneCollider == null)
        {
            zoneCollider =
                GetComponent<Collider>();
        }

        zoneActive = true;
    }


    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory =
                FindAnyObjectByType<PlayerInventory>();
        }

        CheckAxe();
    }


    private void Update()
    {
        CheckAxe();
    }


    private void CheckAxe()
    {
        if (playerInventory == null)
        {
            return;
        }

        if (requiredAxe == null)
        {
            return;
        }


        bool hasAxe =
            playerInventory.GetItemCount(requiredAxe) > 0;


        if (hasAxe && zoneActive)
        {
            DisableZone();
        }
        else if (!hasAxe && !zoneActive)
        {
            EnableZone();
        }
    }


    private void DisableZone()
    {
        zoneActive = false;


        // Remove the state from the player immediately
        // if they are currently inside the zone.
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.ResetToDefault();
        }


        if (zoneCollider != null)
        {
            zoneCollider.enabled = false;
        }


        if (debugZone)
        {
            Debug.Log(
                "[BossNoAttackZone] Axe obtained. "
                + "NoAttack zone disabled."
            );
        }
    }


    private void EnableZone()
    {
        zoneActive = true;


        if (zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }


        if (debugZone)
        {
            Debug.Log(
                "[BossNoAttackZone] Axe not obtained. "
                + "NoAttack zone enabled."
            );
        }
    }
}