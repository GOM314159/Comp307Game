using UnityEngine;

[CreateAssetMenu(
    fileName = "Calvin's Gibson Les Paul",
    menuName = "Inventory/Items/Weapon/Melee/Calvin's Gibson Les Paul"
)]
public class GuitarItem : MeleeWeaponItem
{
    // =========================================================
    // GUITAR SETTINGS
    // =========================================================

    [Header("Calvin's Gibson Les Paul")]

    [Tooltip("Damage dealt by Calvin's Gibson Les Paul.")]
    [SerializeField]
    private float guitarDamage = 50f;

    [Tooltip("How long before the guitar can be swung again.")]
    [SerializeField]
    private float guitarCooldown = 1f;

    [Header("Hit Sounds")]

    [Tooltip("Total number of sounds that can be randomly selected. " +
             "Set this to the number of populated entries you want used.")]
    [Min(1)]
    [SerializeField]
    private int totalSounds = 4;

    [Tooltip("Hit sounds played at random when the guitar successfully lands a hit. " +
             "Resize this array in the Inspector as needed.")]
    [SerializeField]
    private AudioClip[] hitSounds = new AudioClip[4];

    // =========================================================
    // INITIALIZATION
    // =========================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        weaponType = WeaponType.Melee;
        SetItemName("Calvin's Gibson Les Paul");

        stackable = false;
        maxStackSize = 1;

        weaponDamage = guitarDamage;
        attackCooldown = guitarCooldown;
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public override float GetAttackDamage()
    {
        return guitarDamage;
    }

    // =========================================================
    // HIT SOUND
    // =========================================================

    public override void OnSuccessfulHit(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                "[Guitar] No AudioSource is available to play the hit sound."
            );
            return;
        }

        if (hitSounds == null || hitSounds.Length == 0)
        {
            return;
        }

        int usableSoundCount =
            Mathf.Clamp(
                totalSounds,
                1,
                hitSounds.Length
            );

        // Pick from the configured number of sounds only. Empty entries
        // are skipped by trying each available slot at most once.
        int startIndex =
            Random.Range(0, usableSoundCount);

        for (int i = 0; i < usableSoundCount; i++)
        {
            int index =
                (startIndex + i) % usableSoundCount;

            AudioClip clip = hitSounds[index];

            if (clip == null)
            {
                continue;
            }

            // PlayOneShot preserves the clip's original pitch and length;
            // the AudioSource pitch is deliberately not changed here.
            audioSource.PlayOneShot(clip);
            return;
        }
    }

    // =========================================================
    // USE
    // =========================================================

    public override void Use()
    {
        Debug.Log(
            "[Inventory] Calvin's Gibson Les Paul selected."
        );
    }
}
