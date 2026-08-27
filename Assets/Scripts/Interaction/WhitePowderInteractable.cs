using UnityEngine;

public class WhitePowderInteractable : WorldInteractable
{
    [Header("Speed Boost")]
    [SerializeField]
    private float speedMultiplier = 1.25f;

    [Header("Player")]
    [SerializeField]
    private PlayerMovement playerMovement;


    public override void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogError(
                "[White Powder] PlayerMovement has not been assigned."
            );

            return;
        }

        // Apply the permanent speed boost
        playerMovement.ApplyPermanentSpeedBoost(
            speedMultiplier
        );

        Debug.Log(
            "[White Powder] Speed boost activated."
            + " | Multiplier: x"
            + speedMultiplier
        );

        // Remove the powder from the level
        Destroy(gameObject);
    }


    public override string GetInteractionPrompt()
    {
        return "Take White Powder";
    }
}