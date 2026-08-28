using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================
    private enum AttackResult
    {
        Miss,
        HitNonDamageable,
        HitDamageable,
        KilledTarget
    }

    [Header("References")]

    [SerializeField]
    private PlayerEquipmentManager equipmentManager;

    [SerializeField]
    private PlayerStateManager playerStateManager;

    [Header("Attack Origin")]

    [SerializeField]
    private Transform attackOrigin;

    // =========================================================
    // UNARMED ATTACK
    // =========================================================

    [Header("Unarmed Attack")]

    [SerializeField]
    private float unarmedDamage = 1f;
    [Header("Attack Targeting")]

    [SerializeField]
    private LayerMask damageableLayers; // For targetting targets


    [SerializeField]
    private float unarmedRange = 1f;


    [SerializeField]
    private float unarmedHitRadius = 0.3f;


    [SerializeField]
    private float unarmedCooldown = 0.5f;

    [SerializeField]
    private float meleeHitRadius = 0.3f;


    // =========================================================
    // ATTACK STATE
    // =========================================================

    private float nextUnarmedAttackTime;

    private float nextMeleeAttackTime;


    // =========================================================
    // INPUT
    // =========================================================

    private PlayerControls controls;


    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        controls = new PlayerControls();


        // -----------------------------------------------------
        // FIND REFERENCES
        // -----------------------------------------------------

        if (equipmentManager == null)
        {
            equipmentManager =
                GetComponent<PlayerEquipmentManager>();
        }


        if (playerStateManager == null)
        {
            playerStateManager =
                GetComponent<PlayerStateManager>();
        }


        // -----------------------------------------------------
        // ATTACK INPUT
        // -----------------------------------------------------

        controls.Player.Attack.performed +=
            _ => TryAttack();


        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        if (equipmentManager == null)
        {
            Debug.LogWarning(
                "[Attack] PlayerEquipmentManager "
                + "not found on Player."
            );
        }


        if (playerStateManager == null)
        {
            Debug.LogWarning(
                "[Attack] PlayerStateManager "
                + "not found on Player."
            );
        }
    }


    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    private void OnEnable()
    {
        if (controls != null)
        {
            controls.Enable();
        }
    }


    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
        }
    }


    // =========================================================
    // TRY ATTACK
    // =========================================================

    private void TryAttack()
    {
        // -----------------------------------------------------
        // STATE CHECK
        // -----------------------------------------------------

        if (playerStateManager == null)
        {
            return;
        }


        if (!playerStateManager.CanAttack())
        {
            return;
        }


        // -----------------------------------------------------
        // EQUIPMENT CHECK
        // -----------------------------------------------------

        if (equipmentManager == null)
        {
            return;
        }


        WeaponItem weapon =
            equipmentManager.GetCurrentWeapon();


        // -----------------------------------------------------
        // NO WEAPON
        // -----------------------------------------------------

        if (weapon == null)
        {
            TryUnarmedAttack();

            return;
        }




        // -----------------------------------------------------
        // MELEE WEAPON
        // -----------------------------------------------------

        if (weapon is MeleeWeaponItem meleeWeapon)
        {
            TryMeleeAttack(
                meleeWeapon
            );

            return;
        }


        // -----------------------------------------------------
        // RANGED WEAPON
        // -----------------------------------------------------

        if (weapon is RangedWeaponItem rangedWeapon)
        {
            TryRangedAttack(
                rangedWeapon
            );

            return;
        }


        // -----------------------------------------------------
        // FALLBACK
        // -----------------------------------------------------

        Debug.LogWarning(
            "[Attack] Equipped weapon type "
            + "is not supported: "
            + weapon.itemName
        );
    }
    // =========================================================
    // UNARMED
    // =========================================================

    private void TryUnarmedAttack()
    {
        if (Time.time < nextUnarmedAttackTime)
        {
            return;
        }


        nextUnarmedAttackTime =
            Time.time + unarmedCooldown;


        Debug.Log(
            "[Attack] Unarmed attack performed."
        );


        PerformUnarmedHit();
    }

    // =========================================================
    // UNARMED HIT DETECTION
    // =========================================================

    // =========================================================
    // UNARMED HIT DETECTION
    // =========================================================

    private void PerformUnarmedHit()
    {
        PerformCloseRangeAttack(
        unarmedDamage,
        unarmedRange,
        meleeHitRadius
        );
    }


    // =========================================================
    // MELEE HIT DETECTION
    // =========================================================

    private void PerformMeleeHit(float damage,float range,float hitRadius)
    {
        PerformCloseRangeAttack(
        damage,
        range,
        hitRadius
        );
    }


    private void PerformCloseRangeAttack(float damage,float range,float hitRadius)
    {
        if (attackOrigin == null)
        {
            Debug.LogWarning(
            "[Attack] Attack origin has not been assigned."
            );

            return;
        }


        RaycastHit hit;


        if (!Physics.SphereCast(
                attackOrigin.position,
                hitRadius,
                attackOrigin.forward,
                out hit,
                range,
                damageableLayers,
                QueryTriggerInteraction.Ignore))
        {
            Debug.Log(
                "[Attack] Close-range attack missed."
            );

            return;
        }


        AttackResult result =
            TryDamageTarget(
                hit.collider,
                damage
            );


        switch (result)
        {
            case AttackResult.HitDamageable:

                Debug.Log(
                    "[Attack] Hit damageable target: "
                    + hit.collider.name
                );

                break;


            case AttackResult.KilledTarget:

                Debug.Log(
                    "[Attack] Killed target: "
                    + hit.collider.name
                );

                break;
        }

    }


    private void TryMeleeAttack(MeleeWeaponItem weapon)
    {
        if (Time.time < nextMeleeAttackTime)
        {
            return;
        }

        nextMeleeAttackTime =
        Time.time + weapon.attackCooldown;

        Debug.Log(
            "[Attack] Melee attack performed: "
            + weapon.itemName
        );

        PerformMeleeHit(
            weapon.GetAttackDamage(),
            weapon.attackRange,
            meleeHitRadius
        );
    }



    // =========================================================
    // RANGED
    // =========================================================

    private void TryRangedAttack(
        RangedWeaponItem weapon)
    {
        Debug.Log(
            "[Attack] Ranged attack requested: "
            + weapon.itemName
        );


        // Actual ranged attack execution
        // will be added in the ranged step.
    }

    // =========================================================
    // DAMAGE TARGET
    // =========================================================

    private AttackResult TryDamageTarget(Collider targetCollider, float damage)
    {
        if (targetCollider == null)
        {
            return AttackResult.Miss;
        }

        IDamageable damageable =
            targetCollider.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Debug.Log(
                "[Attack] Hit object is not damageable: "
                + targetCollider.name
            );

            return AttackResult.HitNonDamageable;
        }

        bool killedTarget =
            damageable.TakeDamage(
                damage
            );

        if (killedTarget)
        {
            return AttackResult.KilledTarget;
        }

        return AttackResult.HitDamageable;

    }


}