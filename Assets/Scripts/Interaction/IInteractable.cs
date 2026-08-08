public interface IInteractable
{
    // Called when the player interacts with this object.
    void Interact();

    // Name shown by the interaction UI later.
    string GetInteractionPrompt();
}