using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class WorldInteractable : MonoBehaviour, IInteractable
{
    // =========================================================
    // INTERACTION SETTINGS
    // =========================================================

    [Header("Interaction")]

    [Tooltip("Text describing what the player will do.")]
    [SerializeField]
    private string interactionPrompt = "Interact";

    [SerializeField]
    private bool canInteract = true;

    [SerializeField]
    private bool interactOnlyOnce = false;


    // =========================================================
    // PROMPT UI
    // =========================================================

    [Header("Interaction Prompt")]

    [Tooltip("Text object used to display the interaction prompt.")]
    [SerializeField]
    private TextMeshProUGUI interactionText;

    [Tooltip("Automatically searches this object and its children for the TextMeshPro UI.")]
    [SerializeField]
    private bool automaticallyFindText = true;


    // =========================================================
    // STATE
    // =========================================================

    private bool hasBeenInteractedWith = false;


    // =========================================================
    // EVENTS
    // =========================================================

    public event System.Action OnInteracted;
    public event System.Action OnInteractionDisabled;


    // =========================================================
    // UNITY
    // =========================================================

    protected virtual void Awake()
    {
        if (automaticallyFindText && interactionText == null)
        {
            interactionText =
                GetComponentInChildren<TextMeshProUGUI>(true);
        }

        HideInteractionPrompt();
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    public virtual void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        Debug.Log(
            "[Interaction] "
            + gameObject.name
            + " interacted with."
        );

        hasBeenInteractedWith = true;

        HideInteractionPrompt();

        OnInteracted?.Invoke();

        if (interactOnlyOnce)
        {
            canInteract = false;

            OnInteractionDisabled?.Invoke();
        }
    }


    // =========================================================
    // PROMPT
    // =========================================================

    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }


    public void ShowInteractionPrompt()
    {
        if (interactionText == null)
        {
            return;
        }

        interactionText.text = BuildInteractionPrompt();

        interactionText.gameObject.SetActive(true);
    }


    public void HideInteractionPrompt()
    {
        if (interactionText == null)
        {
            return;
        }

        interactionText.gameObject.SetActive(false);
    }


    private string BuildInteractionPrompt()
    {
        string interactionButton =
            GetInteractionButtonText();

        return interactionButton
               + " to "
               + GetInteractionPrompt();
    }


    // =========================================================
    // INPUT DEVICE
    // =========================================================

    private string GetInteractionButtonText()
    {
        PlayerControls controls = new PlayerControls();

        InputAction interactAction =
            controls.Player.Interact;

        // -----------------------------------------------------
        // CONTROLLER
        // -----------------------------------------------------

        if (GameInputDevice.LastUsedDeviceIsGamepad())
        {
            return GetGamepadBindingName(interactAction);
        }


        // -----------------------------------------------------
        // KEYBOARD / MOUSE
        // -----------------------------------------------------

        return GetKeyboardMouseBindingName(interactAction);
    }


    private string GetGamepadBindingName(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite ||
                binding.isPartOfComposite)
            {
                continue;
            }

            if (binding.groups != null &&
                binding.groups.Contains("Gamepad"))
            {
                return action.GetBindingDisplayString(
                    i,
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames
                );
            }
        }

        return "Interact";
    }


    private string GetKeyboardMouseBindingName(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite ||
                binding.isPartOfComposite)
            {
                continue;
            }

            if (binding.groups != null &&
                (binding.groups.Contains("Keyboard") ||
                 binding.groups.Contains("Mouse")))
            {
                return action.GetBindingDisplayString(
                    i,
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames
                );
            }
        }

        return "Interact";
    }


    // =========================================================
    // STATE
    // =========================================================

    public bool CanInteract()
    {
        if (!canInteract)
        {
            return false;
        }

        if (interactOnlyOnce &&
            hasBeenInteractedWith)
        {
            return false;
        }

        return true;
    }


    public bool HasBeenInteractedWith()
    {
        return hasBeenInteractedWith;
    }


    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    public void SetInteractionEnabled(bool enabled)
    {
        canInteract = enabled;

        if (!enabled)
        {
            HideInteractionPrompt();
        }
    }


    public bool IsInteractionEnabled()
    {
        return canInteract;
    }


    // =========================================================
    // RESET
    // =========================================================

    public virtual void ResetInteraction()
    {
        hasBeenInteractedWith = false;
        canInteract = true;

        HideInteractionPrompt();
    }
}