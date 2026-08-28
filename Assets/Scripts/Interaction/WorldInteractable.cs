using UnityEngine;
using System;

public class WorldInteractable : MonoBehaviour, IInteractable
{
    // =========================================================
    // INTERACTION SETTINGS
    // =========================================================

    [Header("Interaction")]

    [SerializeField]
    private string interactionPrompt = "Interact";


    [SerializeField]
    private bool canInteract = true;


    [SerializeField]
    private bool interactOnlyOnce = false;


    // =========================================================
    // STATE
    // =========================================================

    private bool hasBeenInteractedWith = false;


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action OnInteracted;

    public event Action OnInteractionDisabled;


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
    }
}