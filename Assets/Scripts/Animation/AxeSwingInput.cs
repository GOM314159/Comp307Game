using UnityEngine;

public class AxeSwingInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandAxeAnimator axeAnimator;

    [Header("Weapon Audio Controllers")]
    [SerializeField] private WeaponAudioController axeAudio;
    [SerializeField] private WeaponAudioController katanaAudio;
    [SerializeField] private WeaponAudioController guitarAudio;
    [SerializeField] private WeaponAudioController panAudio;
    [SerializeField] private WeaponAudioController stormBreakerAudio; // <-- Added StormBreaker

    public enum ActiveWeapon
    {
        Axe,
        Katana,
        Guitar,
        Pan,
        StormBreaker,
        Gun
    }
    [Header("Current State")]
    [SerializeField] private ActiveWeapon currentWeapon = ActiveWeapon.Axe;

    private PlayerControls controls;

    // 2. Add inside AxeSwingInput class fields:
    [SerializeField] private WeaponAudioController gunAudio;

    // 3. Add this public function for the Animation Event:
    public void PlayShoot()
    {
        if (currentWeapon == ActiveWeapon.Gun && gunAudio != null)
        {
            gunAudio.PlayShoot();
        }
    }

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Player.Attack.performed += _ => Swing();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Swing()
    {
        // 1. Play the specific sound clip for the currently active weapon
        switch (currentWeapon)
        {
            case ActiveWeapon.Axe:
                if (axeAudio != null) axeAudio.PlaySwing();
                break;
            case ActiveWeapon.Katana:
                if (katanaAudio != null) katanaAudio.PlaySwing();
                break;
            case ActiveWeapon.Guitar:
                if (guitarAudio != null) guitarAudio.PlaySwing();
                break;
            case ActiveWeapon.Pan:
                if (panAudio != null) panAudio.PlaySwing();
                break;
            case ActiveWeapon.StormBreaker:
                if (stormBreakerAudio != null) stormBreakerAudio.PlaySwing();
                break;
        }

        // 2. Play the swing animation
        if (axeAnimator != null)
        {
            axeAnimator.PlaySwing();
        }
    }

    public void SetActiveWeapon(ActiveWeapon newWeapon)
    {
        currentWeapon = newWeapon;
    }

    // Call this from an Animation Event when the hit lands!
    public void PlayImpact()
    {
        switch (currentWeapon)
        {
            case ActiveWeapon.Axe:
                if (axeAudio != null) axeAudio.PlayImpact();
                break;
            case ActiveWeapon.Katana:
                if (katanaAudio != null) katanaAudio.PlayImpact();
                break;
            case ActiveWeapon.Guitar:
                if (guitarAudio != null) guitarAudio.PlayImpact();
                break;
            case ActiveWeapon.Pan:
                if (panAudio != null) panAudio.PlayImpact();
                break;
            case ActiveWeapon.StormBreaker:
                if (stormBreakerAudio != null) stormBreakerAudio.PlayImpact();
                break;
        }
    }
}