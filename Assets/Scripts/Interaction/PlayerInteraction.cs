using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{

    [Header("References")]

    // Main camera used for the interaction raycast.
    [SerializeField] private Transform cameraTransform;

    // Player inventory.
    [SerializeField] private PlayerInventory inventory;

    // Player state manager.
    [SerializeField] private PlayerStateManager stateManager;



    [Header("Interaction Settings")]

    // Adjust interaction range here.
    [SerializeField] private float interactionRange = 3f;

    // Which layers can contain interactable objects.
    [SerializeField] private LayerMask interactionMask;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool debugInteraction = true;




    private PlayerControls controls;



    private IInteractable currentInteractable;
    private IInteractable previousInteractable;


    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Interact.performed +=
    context =>
    {
        GameInputDevice.SetLastUsedDevice(
            context.control.device
        );

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


        if (cameraTransform == null)
        {
            return;
        }


        Ray ray = new Ray(
            cameraTransform.position,
            cameraTransform.forward
        );


        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionMask))
        {
            IInteractable interactable =
                hit.collider.GetComponent<IInteractable>();


            // Child collider support
            if (interactable == null)
            {
                interactable =
                    hit.collider.GetComponentInParent<IInteractable>();
            }


            if (interactable != null)
            {
                currentInteractable = interactable;
            }
        }


        // =========================================================
        // UPDATE PROMPT
        // =========================================================

        if (previousInteractable != currentInteractable)
        {
            // Hide previous prompt
            if (previousInteractable is WorldInteractable previousWorld)
            {
                previousWorld.HideInteractionPrompt();
            }


            // Show new prompt
            if (currentInteractable is WorldInteractable currentWorld)
            {
                if (currentWorld.CanInteract())
                {
                    currentWorld.ShowInteractionPrompt();
                }
            }
        }
    }



    private void HandleInteract()
    {
        Debug.Log("[Interaction TEST] HandleInteract was triggered.");
        if (stateManager != null &&
            !stateManager.CanInteract())
        {
            if (debugInteraction)
            {
                Debug.Log(
                    "[Interaction] Interaction disabled.");
            }

            return;
        }



        if (currentInteractable != null)
        {
            if (debugInteraction)
            {
                Debug.Log(
                    "[Interaction] Interacting with: "
                    + currentInteractable.GetInteractionPrompt());
            }


            currentInteractable.Interact();

            return;
        }



        if (inventory != null)
        {
            inventory.UseSelectedItem();
        }
    }



    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }


    // =========================================================
    // DEBUG RAY
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null)
        {
            return;
        }


        Gizmos.color = Color.yellow;


        Gizmos.DrawRay(
            cameraTransform.position,
            cameraTransform.forward * interactionRange);
    }
}