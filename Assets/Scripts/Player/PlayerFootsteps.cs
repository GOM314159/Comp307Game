using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep Settings")]
    [SerializeField] private float groundCheckDistance = 2.5f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.32f;
    [SerializeField] private float crouchStepInterval = 0.65f;
    [SerializeField] private float footstepVolume = 1f;

    // =====================================
    // SURFACE AUDIO: FOOTSTEPS & LANDINGS
    // =====================================

    [Header("Concrete")]
    [SerializeField] private AudioClip[] concreteFootsteps;
    [SerializeField] private AudioClip[] concreteLandings;

    [Header("Wood")]
    [SerializeField] private AudioClip[] woodFootsteps;
    [SerializeField] private AudioClip[] woodLandings;

    [Header("Grass")]
    [SerializeField] private AudioClip[] grassFootsteps;
    [SerializeField] private AudioClip[] grassLandings;

    [Header("Dirt")]
    [SerializeField] private AudioClip[] dirtFootsteps;
    [SerializeField] private AudioClip[] dirtLandings;

    [Header("Metal")]
    [SerializeField] private AudioClip[] metalFootsteps;
    [SerializeField] private AudioClip[] metalLandings;

    [Header("Stone")]
    [SerializeField] private AudioClip[] stoneFootsteps;
    [SerializeField] private AudioClip[] stoneLandings;

    [Header("Carpet")]
    [SerializeField] private AudioClip[] carpetFootsteps;
    [SerializeField] private AudioClip[] carpetLandings;

    [Header("Water")]
    [SerializeField] private AudioClip[] waterFootsteps;
    [SerializeField] private AudioClip[] waterLandings;

    [Header("Gravel")]
    [SerializeField] private AudioClip[] gravel;
    [SerializeField] private AudioClip[] gravelLandings;

    [Header("Ground")]
    [SerializeField] private AudioClip[] ground;
    [SerializeField] private AudioClip[] groundLandings;

    [Header("Debug")]
    [SerializeField] private bool debugFootsteps = true;

    private float footstepTimer;
    private int lastClipIndex = -1;
    private PlayerStateManager stateManager;
    private bool wasGrounded = true;

    private void Awake()
    {
        stateManager = GetComponent<PlayerStateManager>();

        if (stateManager == null)
        {
            Debug.LogError("[PlayerFootsteps] PlayerStateManager is missing from the Player!", this);
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }
    }

    private void Update()
    {
        HandleFootsteps();
        CheckLanding();
    }

    // =====================================
    // FOOTSTEP LOGIC
    // =====================================

    private void HandleFootsteps()
    {
        if (stateManager == null || !stateManager.CanMove())
        {
            footstepTimer = 0f;
            return;
        }

        if (playerMovement == null) return;

        Vector3 velocity = playerMovement.GetHorizontalVelocity();
        float movementAmount = velocity.magnitude;

        if (movementAmount < 0.1f)
        {
            footstepTimer = 0f;
            return;
        }

        if (!playerMovement.IsGrounded())
        {
            footstepTimer = 0f;
            return;
        }

        float stepInterval;
        if (playerMovement.IsCrouching())
        {
            stepInterval = crouchStepInterval;
        }
        else if (playerMovement.IsSprinting())
        {
            stepInterval = sprintStepInterval;
        }
        else
        {
            stepInterval = walkStepInterval;
        }

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= stepInterval)
        {
            PlayFootstep();
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        SurfaceType.Surface surface = DetectSurface();
        AudioClip[] clips = GetClipsForSurface(surface);

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = GetRandomClip(clips);
        if (clip == null) return;

        audioSource.PlayOneShot(clip, footstepVolume);

        if (debugFootsteps)
        {
            Debug.Log($"[Footsteps] Played: {surface} ({clip.name})");
        }
    }

    // =====================================
    // LANDING LOGIC
    // =====================================

    private void CheckLanding()
    {
        if (playerMovement == null || stateManager == null) return;
        if (!stateManager.CanMove()) return;

        bool isGroundedNow = playerMovement.IsGrounded();

        // Detect transition: airborne last frame (!wasGrounded) and grounded now (isGroundedNow)
        if (!wasGrounded && isGroundedNow)
        {
            PlayLanding();
        }

        wasGrounded = isGroundedNow;
    }

    private void PlayLanding()
    {
        SurfaceType.Surface surface = DetectSurface();
        AudioClip[] landingClips = GetLandingsForSurface(surface);

        // Fallback: if landing clips aren't assigned for a surface, use normal footstep clips as a fallback
        if (landingClips == null || landingClips.Length == 0)
        {
            landingClips = GetClipsForSurface(surface);
        }

        if (landingClips == null || landingClips.Length == 0) return;

        AudioClip clip = GetRandomClip(landingClips);
        if (clip == null) return;

        // Play landing impact slightly louder
        audioSource.PlayOneShot(clip, footstepVolume * 1.25f);

        if (debugFootsteps)
        {
            Debug.Log($"[Footsteps] LANDED on {surface} ({clip.name})");
        }
    }

    // =====================================
    // SURFACE DETECTION & MAPPING
    // =====================================

    private SurfaceType.Surface DetectSurface()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, groundMask))
        {
            SurfaceType surfaceType = hit.collider.GetComponentInParent<SurfaceType>();

            if (surfaceType != null)
            {
                return surfaceType.GetSurface();
            }

            return SurfaceType.Surface.Concrete;
        }

        return SurfaceType.Surface.Concrete;
    }

    private AudioClip[] GetClipsForSurface(SurfaceType.Surface surface)
    {
        switch (surface)
        {
            case SurfaceType.Surface.Concrete: return concreteFootsteps;
            case SurfaceType.Surface.Wood: return woodFootsteps;
            case SurfaceType.Surface.Grass: return grassFootsteps;
            case SurfaceType.Surface.Dirt: return dirtFootsteps;
            case SurfaceType.Surface.Metal: return metalFootsteps;
            case SurfaceType.Surface.Stone: return stoneFootsteps;
            case SurfaceType.Surface.Carpet: return carpetFootsteps;
            case SurfaceType.Surface.Water: return waterFootsteps;
            case SurfaceType.Surface.Gravel: return gravel;
            case SurfaceType.Surface.Ground: return ground;
        }
        return concreteFootsteps;
    }

    private AudioClip[] GetLandingsForSurface(SurfaceType.Surface surface)
    {
        switch (surface)
        {
            case SurfaceType.Surface.Concrete: return concreteLandings;
            case SurfaceType.Surface.Wood: return woodLandings;
            case SurfaceType.Surface.Grass: return grassLandings;
            case SurfaceType.Surface.Dirt: return dirtLandings;
            case SurfaceType.Surface.Metal: return metalLandings;
            case SurfaceType.Surface.Stone: return stoneLandings;
            case SurfaceType.Surface.Carpet: return carpetLandings;
            case SurfaceType.Surface.Water: return waterLandings;
            case SurfaceType.Surface.Gravel: return gravelLandings;
            case SurfaceType.Surface.Ground: return groundLandings;
        }
        return concreteLandings;
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips.Length == 1)
        {
            return clips[0];
        }

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;

        return clips[index];
    }
}