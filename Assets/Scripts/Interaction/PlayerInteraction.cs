using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerStateManager stateManager;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private LayerMask interactionMask;

    [Header("Wirecutter Audio Settings")]
    [Tooltip("Plays when cutting cables or using wirecutters ('SFX_Interact_Wirecutter_Snip').")]
    [SerializeField] private AudioClip wirecutterClip;

    [Tooltip("Delay in seconds before playing the snip sound to align with the cut animation.")]
    [SerializeField, Min(0f)] private float wirecutterAudioDelay = 0.15f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField][Range(0f, 1f)] private float audioVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugInteraction = true;

    private PlayerControls controls;
    private IInteractable currentInteractable;
    private IInteractable previousInteractable;

    private void Awake()
    {
        controls = new PlayerControls();
        EnsureAudioSource();

        if (inventory == null)
        {
            inventory = GetComponent<PlayerInventory>();
        }

        if (stateManager == null)
        {
            stateManager = GetComponent<PlayerStateManager>();
        }

        controls.Player.Interact.performed += context =>
        {
            GameInputDevice.SetLastUsedDevice(context.control.device);
            HandleInteract();
        };
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        DetectInteractable();
    }

    private void DetectInteractable()
    {
        previousInteractable = currentInteractable;
        currentInteractable = null;

        if (stateManager != null && !stateManager.CanInteract())
        {
            HideInteractionPrompt(previousInteractable);
            return;
        }

        if (cameraTransform == null)
        {
            HideInteractionPrompt(previousInteractable);
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        bool foundHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        if (!foundHit)
        {
            foundHit = Physics.Raycast(
                ray,
                out hit,
                interactionRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );
        }

        if (foundHit)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable == null)
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }

        if (previousInteractable != currentInteractable)
        {
            HideInteractionPrompt(previousInteractable);
        }

        if (currentInteractable is WorldInteractable currentWorld)
        {
            if (currentWorld.CanInteract())
            {
                currentWorld.ShowInteractionPrompt();
            }
            else
            {
                currentWorld.HideInteractionPrompt();
            }
        }
    }

    private void HideInteractionPrompt(IInteractable interactable)
    {
        if (interactable is WorldInteractable worldInteractable)
        {
            worldInteractable.HideInteractionPrompt();
        }
    }

    private void HandleInteract()
    {
        Debug.Log("[Interaction TEST] HandleInteract was triggered.");

        // =========================================================
        // LETTER IS OPEN
        // =========================================================
        if (stateManager != null &&
            stateManager.GetCurrentState() == PlayerStateManager.PlayerState.Reading)
        {
            if (LetterUIManager.Instance != null)
            {
                LetterUIManager.Instance.CloseLetter();
            }

            return;
        }

        // =========================================================
        // INVENTORY IS OPEN
        // =========================================================
        if (stateManager != null &&
            stateManager.GetCurrentState() == PlayerStateManager.PlayerState.Inventory)
        {
            if (inventory != null)
            {
                inventory.UseSelectedItem();
            }

            return;
        }

        // =========================================================
        // NORMAL INTERACTION CHECK
        // =========================================================
        if (TutorialManager.IsInteractionLocked)
        {
            return;
        }

        if (stateManager != null && !stateManager.CanInteract())
        {
            if (debugInteraction)
            {
                Debug.Log("[Interaction] Interaction disabled.");
            }

            return;
        }

        // =========================================================
        // WORLD INTERACTION
        // =========================================================
        if (currentInteractable != null)
        {
            if (debugInteraction)
            {
                Debug.Log("[Interaction] Interacting with: " + currentInteractable.GetInteractionPrompt());
            }

            CheckAndPlayWirecutterAudio(currentInteractable);
            currentInteractable.Interact();
            return;
        }

        // =========================================================
        // SELECTED INVENTORY ITEM
        // =========================================================
        if (inventory != null)
        {
            inventory.UseSelectedItem();
        }
    }

    private void CheckAndPlayWirecutterAudio(IInteractable interactable)
    {
        if (interactable == null || wirecutterClip == null) return;

        string targetName = interactable.GetType().Name.ToLower();

        if (targetName.Contains("wirecutter") || targetName.Contains("cable"))
        {
            StartCoroutine(PlayWirecutterAudioDelayed());
        }
    }

    private IEnumerator PlayWirecutterAudioDelayed()
    {
        if (wirecutterAudioDelay > 0f)
        {
            yield return new WaitForSeconds(wirecutterAudioDelay);
        }

        EnsureAudioSource();
        if (audioSource != null && wirecutterClip != null)
        {
            audioSource.PlayOneShot(wirecutterClip, audioVolume);
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactionRange);
    }
}