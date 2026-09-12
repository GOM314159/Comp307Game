using UnityEngine;

public class PlayerDamageObject : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 25f;

    [Header("Contact Settings")]
    [SerializeField] private bool damageOncePerContact = true;

    
    private bool playerInContact;

    private void OnCollisionEnter(Collision collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsPlayer(collision.gameObject))
        {
            playerInContact = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            playerInContact = false;
        }
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (!IsPlayer(target))
            return;

        if (damageOncePerContact && playerInContact)
            return;

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("[PlayerDamageObject] PlayerHealth not found on player.");
            return;
        }

        playerInContact = true;

        Debug.Log("[PlayerDamageObject] Damaging player for " + damage);

        playerHealth.TakeDamage(damage);
    }

    private bool IsPlayer(GameObject target)
    {
        return target.CompareTag("Player");
    }
}