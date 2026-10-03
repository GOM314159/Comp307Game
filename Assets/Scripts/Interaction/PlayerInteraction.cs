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
    [SerializeField] private float interactionRange = 2f;

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

        // Automatically find persistent Player references.
        if (inventory == null)
        {
            inventory =
                GetComponent<PlayerInventory>();
        }

        if (stateManager == null)
        {
            stateManager =
                GetComponent<PlayerStateManager>();
        }

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


        // First respect the configured interaction mask.
        bool foundHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            interactionMask);

        // If the layer is misconfigured, perform a fallback raycast against
        // all layers. This prevents an otherwise valid piano/guitar/pill
        // interactable from silently losing its prompt because of a layer
        // assignment. The nearest collider still wins.
        if (!foundHit)
        {
            foundHit = Physics.Raycast(
                ray,
                out hit,
                interactionRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }

        if (foundHit)
        {
            IInteractable interactable =
                hit.collider.GetComponent<IInteractable>();

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

        // Hide the previous interactable's prompt
        if (previousInteractable != currentInteractable)
        {
            if (previousInteractable is WorldInteractable previousWorld)
            {
                previousWorld.HideInteractionPrompt();
            }
        }

        // Keep the current prompt in sync with whether
        // the object is currently interactable.
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



    private void HandleInteract()
    {
        Debug.Log(
            "[Interaction TEST] HandleInteract was triggered."
        );


        // =========================================================
        // LETTER IS OPEN
        // =========================================================

        if (stateManager != null &&
            stateManager.GetCurrentState() ==
            PlayerStateManager.PlayerState.Reading)
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
        //
        // Allow the interaction button to use the selected item.
        //
        // This is what allows:
        //
        // Right Mouse Button
        // Circle
        //
        // to read the selected letter.
        // =========================================================

        if (stateManager != null &&
            stateManager.GetCurrentState() ==
            PlayerStateManager.PlayerState.Inventory)
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

        if (stateManager != null &&
            !stateManager.CanInteract())
        {
            if (debugInteraction)
            {
                Debug.Log(
                    "[Interaction] Interaction disabled."
                );
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
                Debug.Log(
                    "[Interaction] Interacting with: "
                    + currentInteractable.GetInteractionPrompt()
                );
            }

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