using UnityEngine;

public class WeaponAudioController : MonoBehaviour
{
    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swingClip;
    [SerializeField] private AudioClip impactClip; // <-- Added for hits

    [Header("Sound Variance (Optional)")]
    [SerializeField] private bool enablePitchVariation = true;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [SerializeField] private AudioClip shootClip; // Drag your gunshot sound here in Inspector

    public void PlayShoot()
    {
        if (!gameObject.activeInHierarchy || audioSource == null || !audioSource.enabled) return;
        if (shootClip == null) return;

        audioSource.pitch = enablePitchVariation ? Random.Range(minPitch, maxPitch) : 1.0f;
        audioSource.PlayOneShot(shootClip);
    }

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void PlaySwing()
    {
        if (!gameObject.activeInHierarchy || audioSource == null || !audioSource.enabled) return;
        if (swingClip == null) return;

        audioSource.pitch = enablePitchVariation ? Random.Range(minPitch, maxPitch) : 1.0f;
        audioSource.PlayOneShot(swingClip);
    }

    // New method for impact sounds
    public void PlayImpact()
    {
        if (!gameObject.activeInHierarchy || audioSource == null || !audioSource.enabled) return;
        if (impactClip == null) return;

        audioSource.pitch = enablePitchVariation ? Random.Range(minPitch, maxPitch) : 1.0f;
        audioSource.PlayOneShot(impactClip);
    }
}