using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class PlayerAttackController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================
    private enum AttackResultType
    {
        Miss,
        HitNonDamageable,
        HitDamageable,
        KilledTarget
    }

    private struct AttackResult
    {
        public AttackResultType type;

    }
    public event Action OnAttackPerformed;
    public event Action OnWeaponFired;
    public event Action OnAttackHit;
    public event Action OnTargetKilled;

    [Header("References")]

    [SerializeField]
    private PlayerEquipmentManager equipmentManager;

    [SerializeField]
    private PlayerStateManager playerStateManager;

    [Header("Weapon Audio")]

    [Tooltip("AudioSource used for weapon hit sounds. Leave empty to create/find one automatically on the player.")]
    [SerializeField]
    private AudioSource weaponAudioSource;

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
    private float unarmedCooldown = 0.5f;


    // =========================================================
    // ATTACK STATE
    // =========================================================

    private float nextUnarmedAttackTime;

    private float nextMeleeAttackTime;

    private float nextRangedAttackTime;

    private bool isAttacking;


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

        if (weaponAudioSource == null)
        {
            weaponAudioSource =
                GetComponent<AudioSource>();
        }

        if (weaponAudioSource == null)
        {
            weaponAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        weaponAudioSource.playOnAwake = false;
        weaponAudioSource.loop = false;
        weaponAudioSource.spatialBlend = 0f;


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
        
        if (isAttacking)
        {
            return;
        }

        if (playerStateManager == null)
        {
            return;
        }
        if (GameStateManager.Instance != null &&
        GameStateManager.Instance.GetGameMode() ==
        GameStateManager.GameMode.Paused)
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
        isAttacking = true;


        nextUnarmedAttackTime =
            Time.time + unarmedCooldown;
        OnAttackPerformed?.Invoke();

        Debug.Log(
            "[Attack] Unarmed attack performed."
        );


        PerformUnarmedHit();
        isAttacking = false;
    }


    private void PerformUnarmedHit()
    {
        PerformCloseRangeAttack(
        unarmedDamage,
        unarmedRange
        );
    }


    // =========================================================
    // MELEE HIT DETECTION
    // =========================================================

    private void PerformMeleeHit(
        float damage,
        float range,
        MeleeWeaponItem weapon)
    {
        PerformCloseRangeAttack(
            damage,
            range,
            weapon
        );
    }


    private void PerformCloseRangeAttack(
        float damage,
        float range,
        MeleeWeaponItem meleeWeapon = null)
    {
        if (attackOrigin == null)
        {
            Debug.LogWarning(
            "[Attack] Attack origin has not been assigned."
            );

            return;
        }


        RaycastHit hit;


        if (!Physics.Raycast(
                attackOrigin.position,
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


        AttackResult result = TryDamageTarget(hit, damage);

        switch (result.type)
        {
            case AttackResultType.HitDamageable:

                OnAttackHit?.Invoke();
                meleeWeapon?.OnSuccessfulHit(weaponAudioSource);
                Debug.Log(
                    "[Attack] Hit damageable target: "
                    + hit.collider.name
                );

                break;


            case AttackResultType.KilledTarget:

                OnAttackHit?.Invoke();
                OnTargetKilled?.Invoke();
                meleeWeapon?.OnSuccessfulHit(weaponAudioSource);
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
        isAttacking = true;

        nextMeleeAttackTime =
        Time.time + weapon.attackCooldown;
        OnAttackPerformed?.Invoke();
        Debug.Log(
            "[Attack] Melee attack performed: "
            + weapon.itemName
        );

        PerformMeleeHit(
            weapon.GetAttackDamage(),
            weapon.attackRange,
            weapon
        );
        isAttacking = false;
    }



    // =========================================================
    // RANGED
    // =========================================================

    private void TryRangedAttack(RangedWeaponItem weapon)
    {
        if (Time.time < nextRangedAttackTime)
        {
            return;
        }
        isAttacking = true;

        nextRangedAttackTime =
            Time.time + (1f / weapon.fireRate);

        OnAttackPerformed?.Invoke();

        Debug.Log(
            "[Attack] Ranged attack performed: "
            + weapon.itemName
        );

        PerformRangedAttack(weapon);
        isAttacking = false;
    }

    private void PerformRangedAttack(RangedWeaponItem weapon)
    {
        OnWeaponFired?.Invoke();
        // Play firing animation
        // Add effects
        // Play firing sound

        PerformRangedHit(
            weapon.GetAttackDamage(),
            weapon.range
        );
    }

    private void PerformRangedHit(
    float damage,
    float range)
    {
        if (attackOrigin == null)
        {
            Debug.LogWarning(
                "[Attack] Attack origin has not been assigned."
            );

            return;
        }

        RaycastHit hit;

        if (!Physics.Raycast(
                attackOrigin.position,
                attackOrigin.forward,
                out hit,
                range,
                damageableLayers,
                QueryTriggerInteraction.Ignore))
        {
            Debug.Log(
                "[Attack] Ranged attack missed."
            );

            return;
        }

        AttackResult result =
            TryDamageTarget(
                hit,
                damage
            );

        switch (result.type)
        {
            case AttackResultType.HitDamageable:

                OnAttackHit?.Invoke();

                Debug.Log(
                    "[Attack] Ranged attack hit: "
                    + hit.collider.name
                );

                break;


            case AttackResultType.KilledTarget:

                OnAttackHit?.Invoke();

                OnTargetKilled?.Invoke();

                Debug.Log(
                    "[Attack] Ranged attack killed: "
                    + hit.collider.name
                );

                break;
        }
    }

    // =========================================================
    // DAMAGE TARGET
    // =========================================================

    private AttackResult TryDamageTarget(RaycastHit hit, float damage)
    {
        AttackResult result = new AttackResult
        {
            type = AttackResultType.Miss
        };


        if (hit.collider == null)
        {
            return result;
        }


        IDamageable damageable =
            hit.collider.GetComponentInParent<IDamageable>();


        if (damageable == null)
        {
            Debug.Log(
                "[Attack] Hit object is not damageable: "
                + hit.collider.name
            );

            result.type =
                AttackResultType.HitNonDamageable;

            return result;
        }


        if (damageable is MonoBehaviour damageableBehaviour)
        {
            if (damageableBehaviour.transform.root ==
                transform.root)
            {
                Debug.Log(
                    "[Attack] Attack cannot damage the player."
                );

                result.type =
                    AttackResultType.HitNonDamageable;

                return result;
            }
        }


        bool killedTarget =
            damageable.TakeDamage(
                damage
            );


        if (killedTarget)
        {
            result.type =
                AttackResultType.KilledTarget;
        }
        else
        {
            result.type =
                AttackResultType.HitDamageable;
        }


        return result;

    }




}