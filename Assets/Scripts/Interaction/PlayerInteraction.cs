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


    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Interact.performed +=
            _ => HandleInteract();
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
        currentInteractable = null;


        if (cameraTransform == null)
        {
            return;
        }


        Ray ray = new Ray(
            cameraTransform.position,
            cameraTransform.forward);


        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionMask))
        {
            // Try to find an IInteractable on the object that was hit.
            IInteractable interactable =
                hit.collider.GetComponent<IInteractable>();


            // If the collider is a child of the interactable object, check the parent too.
            if (interactable == null)
            {
                interactable =
                    hit.collider.GetComponentInParent<IInteractable>();
            }


            if (interactable != null)
            {
                currentInteractable = interactable;


                if (debugInteraction)
                {
                    Debug.Log(
                        "[Interaction] Looking at: "
                        + interactable.GetInteractionPrompt());
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