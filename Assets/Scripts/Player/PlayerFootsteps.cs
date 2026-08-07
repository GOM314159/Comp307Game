using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]

    // Drag the Player Movement component here
    [SerializeField] private PlayerMovement playerMovement;

    // Drag the Main Camera here
    [SerializeField] private Transform playerCamera;

    // Audio source used to play footsteps
    [SerializeField] private AudioSource audioSource;

    // FOOTSTEP SETTINGS

    [Header("Footstep Settings")]

    // How far below the player we check for a surface
    // Adjust ground detection distance here
    [SerializeField] private float groundCheckDistance = 1.5f;

    // Layers that count as walkable surfaces
    [SerializeField] private LayerMask groundMask;

    // Adjust the time between footsteps here
    [SerializeField] private float walkStepInterval = 0.5f;

    // Adjust the time between sprint footsteps here
    [SerializeField] private float sprintStepInterval = 0.32f;

    // Adjust the time between crouching footsteps here
    [SerializeField] private float crouchStepInterval = 0.65f;

    // Adjust how loud footsteps are here
    [SerializeField] private float footstepVolume = 1f;

    // FOOTSTEP AUDIO

    [Header("Concrete Footsteps")]

    [SerializeField] private AudioClip[] concreteFootsteps;


    [Header("Wood Footsteps")]

    [SerializeField] private AudioClip[] woodFootsteps;


    [Header("Grass Footsteps")]

    [SerializeField] private AudioClip[] grassFootsteps;


    [Header("Dirt Footsteps")]

    [SerializeField] private AudioClip[] dirtFootsteps;


    [Header("Metal Footsteps")]

    [SerializeField] private AudioClip[] metalFootsteps;


    [Header("Stone Footsteps")]

    [SerializeField] private AudioClip[] stoneFootsteps;


    [Header("Carpet Footsteps")]

    [SerializeField] private AudioClip[] carpetFootsteps;


    [Header("Water Footsteps")]

    [SerializeField] private AudioClip[] waterFootsteps;

    [Header("Gravel")]

    [SerializeField] private AudioClip[] gravel;

    [Header("Ground")]

    [SerializeField] private AudioClip[] ground;


    // =====================================
    // DEBUG
    // =====================================

    [Header("Debug")]

    [SerializeField] private bool debugFootsteps = true;

    private float footstepTimer;

    private int lastClipIndex = -1;

    private void Start()
    {
        Debug.Log("[Footsteps] PlayerFootsteps script has started.");
    }

    private void Update()
    {
        HandleFootsteps();
    }

    // FOOTSTEP LOGIC


    private void HandleFootsteps()
    {
        // Check whether the player is trying to move
        if (!playerMovement.IsMoving())
        {
            footstepTimer = 0f;
            return;
        }

        // Player isn't on the ground
        if (!playerMovement.IsGrounded())
        {
            footstepTimer = 0f;
            return;
        }

        // Determine step interval

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

    // PLAY FOOTSTEP

    private void PlayFootstep()
    {
        SurfaceType.Surface surface = DetectSurface();

        AudioClip[] clips = GetClipsForSurface(surface);

        if (clips == null || clips.Length == 0)
        {
            if (debugFootsteps)
            {
                Debug.Log(
                    "[Footsteps] No audio clips assigned for: "
                    + surface);
            }

            return;
        }

        AudioClip clip = GetRandomClip(clips);

        audioSource.PlayOneShot(
            clip,
            footstepVolume);

        if (debugFootsteps)
        {
            Debug.Log(
                "[Footsteps] Played: "
                + surface);
        }
    }

    // DETECT SURFACE

    private SurfaceType.Surface DetectSurface()
    {
        RaycastHit hit;

        // Start slightly above the player's feet
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundMask))
        {
            if (debugFootsteps)
            {
                Debug.Log(
                    "[Footsteps] Raycast HIT: "
                    + hit.collider.gameObject.name);
            }

            SurfaceType surfaceType =
                hit.collider.GetComponent<SurfaceType>();

            if (surfaceType != null)
            {
                return surfaceType.GetSurface();
            }

            // Temporary default until we implement
            // the actual surface system.
            return SurfaceType.Surface.Concrete;
        }

        if (debugFootsteps)
        {
            Debug.Log("[Footsteps] Raycast MISSED the ground.");
        }

        return SurfaceType.Surface.Concrete;
    }

    // GET AUDIO CLIPS

    private AudioClip[] GetClipsForSurface(
        SurfaceType.Surface surface)
    {
        switch (surface)
        {
            case SurfaceType.Surface.Concrete:
                return concreteFootsteps;

            case SurfaceType.Surface.Wood:
                return woodFootsteps;

            case SurfaceType.Surface.Grass:
                return grassFootsteps;

            case SurfaceType.Surface.Dirt:
                return dirtFootsteps;

            case SurfaceType.Surface.Metal:
                return metalFootsteps;

            case SurfaceType.Surface.Stone:
                return stoneFootsteps;

            case SurfaceType.Surface.Carpet:
                return carpetFootsteps;

            case SurfaceType.Surface.Water:
                return waterFootsteps;

            case SurfaceType.Surface.Gravel:
                return gravel;

            case SurfaceType.Surface.Ground:
                return ground;
        }

        return concreteFootsteps;
    }
    // RANDOM CLIP

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