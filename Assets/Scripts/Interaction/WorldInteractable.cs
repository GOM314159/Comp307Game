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

    [Tooltip(
        "Optional local interaction text. " +
        "Leave empty when using PersistentGameplayUI."
    )]
    [SerializeField]
    private TextMeshProUGUI interactionText;

    [Tooltip(
        "Automatically finds local text first, then the " +
        "shared PersistentGameplayUI interaction text."
    )]
    [SerializeField]
    private bool automaticallyFindText = true;


    // =========================================================
    // STATE
    // =========================================================

    private bool hasBeenInteractedWith = false;


    // =========================================================
    // ORIGINAL WORLD STATE
    // =========================================================

    private bool originalActiveState;

    private Transform originalParent;

    private Vector3 originalLocalPosition;

    private Quaternion originalLocalRotation;

    private Vector3 originalLocalScale;


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
        ResolveInteractionText();


        // -----------------------------------------------------
        // SAVE ORIGINAL WORLD STATE
        // -----------------------------------------------------

        originalActiveState =
            gameObject.activeSelf;

        originalParent =
            transform.parent;

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;

        originalLocalScale =
            transform.localScale;


        HideInteractionPrompt();
    }


    // =========================================================
    // FIND SHARED UI
    // =========================================================

    private void ResolveInteractionText()
    {
        if (!automaticallyFindText)
        {
            return;
        }


        // -----------------------------------------------------
        // 1. TRY LOCAL TEXT
        // -----------------------------------------------------

        if (interactionText == null)
        {
            interactionText =
                GetComponentInChildren<TextMeshProUGUI>(true);
        }


        // -----------------------------------------------------
        // 2. TRY PERSISTENT GAMEPLAY UI
        // -----------------------------------------------------

        if (interactionText == null)
        {
            if (PersistentGameplayUI.Instance != null)
            {
                interactionText =
                    PersistentGameplayUI.Instance
                        .GetInteractionText();
            }
        }


        // -----------------------------------------------------
        // 3. WARNING
        // -----------------------------------------------------

        if (interactionText == null)
        {
            // Last-resort lookup supports persistent GUI setups where the UI
            // exists but PersistentGameplayUI has not finished Awake yet.
            foreach (TextMeshProUGUI candidate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (candidate != null && candidate.gameObject.scene.IsValid() &&
                    (candidate.name == "InteractionText" || candidate.gameObject.name == "InteractionText"))
                { interactionText = candidate; break; }
            }
        }

        if (interactionText == null)
        {
            Debug.LogWarning(
                "[WorldInteractable] No Interaction Text found " +
                "for: "
                + gameObject.name
                + ". Make sure PersistentGameplayUI exists."
            );
        }
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
        ResolveInteractionText();


        if (interactionText == null)
        {
            return;
        }


        interactionText.text =
            BuildInteractionPrompt();


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
        PlayerControls controls =
            new PlayerControls();


        InputAction interactAction =
            controls.Player.Interact;


        if (GameInputDevice.LastUsedDeviceIsGamepad())
        {
            return GetGamepadBindingName(
                interactAction
            );
        }


        return GetKeyboardMouseBindingName(
            interactAction
        );
    }


    private string GetGamepadBindingName(
        InputAction action)
    {
        for (
            int i = 0;
            i < action.bindings.Count;
            i++)
        {
            InputBinding binding =
                action.bindings[i];


            if (
                binding.isComposite ||
                binding.isPartOfComposite)
            {
                continue;
            }


            if (
                binding.groups != null &&
                binding.groups.Contains("Gamepad"))
            {
                return action.GetBindingDisplayString(
                    i,
                    InputBinding.DisplayStringOptions
                        .DontUseShortDisplayNames
                );
            }
        }


        return "Interact";
    }


    private string GetKeyboardMouseBindingName(
        InputAction action)
    {
        for (
            int i = 0;
            i < action.bindings.Count;
            i++)
        {
            InputBinding binding =
                action.bindings[i];


            if (
                binding.isComposite ||
                binding.isPartOfComposite)
            {
                continue;
            }


            if (
                binding.groups != null &&
                (
                    binding.groups.Contains("Keyboard") ||
                    binding.groups.Contains("Mouse")
                ))
            {
                return action.GetBindingDisplayString(
                    i,
                    InputBinding.DisplayStringOptions
                        .DontUseShortDisplayNames
                );
            }
        }


        return "Interact";
    }


    // =========================================================
    // STATE
    // =========================================================

    public virtual bool CanInteract()
    {
        // The Day 1 tutorial blocks world interaction until its final
        // tutorial audio has finished. Input actions such as Inventory and
        // Attack remain available so the player can demonstrate them.
        if (TutorialManager.IsInteractionLocked)
        {
            return false;
        }

        if (!canInteract)
        {
            return false;
        }

        if (interactOnlyOnce && hasBeenInteractedWith)
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

    public void SetInteractionEnabled(
        bool enabled)
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
        if (transform.parent != originalParent)
        {
            transform.SetParent(
                originalParent
            );
        }


        transform.localPosition =
            originalLocalPosition;


        transform.localRotation =
            originalLocalRotation;


        transform.localScale =
            originalLocalScale;


        gameObject.SetActive(
            originalActiveState
        );


        hasBeenInteractedWith = false;

        canInteract = true;


        HideInteractionPrompt();


        OnResetState();
    }


    protected virtual void OnResetState()
    {
    }
}