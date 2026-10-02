using UnityEngine;

public class Level3Painting : MonoBehaviour
{
    [Header("Painting")]
    [Tooltip("The SpriteRenderer displaying this painting.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Images")]
    [Tooltip("The image/sprite shown before the piano is opened.")]
    [SerializeField]
    private Sprite originalSprite;

    [Tooltip("The image/sprite shown after the piano is opened.")]
    [SerializeField]
    private Sprite secondSprite;

    [Header("Startup")]
    [Tooltip("If enabled, the painting starts on the original image.")]
    [SerializeField]
    private bool applyOriginalSpriteOnStart = true;

    private bool hasChanged = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[Level3Painting] No SpriteRenderer found on " +
                gameObject.name +
                ". Assign one in the Inspector."
            );

            return;
        }

        // If no original sprite was manually assigned, remember
        // whatever sprite the painting already has in the scene.
        if (originalSprite == null)
        {
            originalSprite = spriteRenderer.sprite;
        }

        if (applyOriginalSpriteOnStart)
        {
            ApplyOriginalSprite();
        }
    }


    // =========================================================
    // PAINTING CHANGE
    // =========================================================

    /// <summary>
    /// Changes this painting to its second image.
    /// Called by Level3PianoInteractable when the piano is opened.
    /// </summary>
    public void ChangeToSecondImage()
    {
        if (hasChanged)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[Level3Painting] Cannot change painting because " +
                "SpriteRenderer is missing on " +
                gameObject.name
            );

            return;
        }

        if (secondSprite == null)
        {
            Debug.LogError(
                "[Level3Painting] Second Sprite has not been assigned " +
                "on " +
                gameObject.name +
                "."
            );

            return;
        }

        spriteRenderer.sprite = secondSprite;
        hasChanged = true;

        Debug.Log(
            "[Level3Painting] " +
            gameObject.name +
            " changed to its second image."
        );
    }


    /// <summary>
    /// Restores the original image.
    /// Useful if the level/world is reset.
    /// </summary>
    public void ResetPainting()
    {
        hasChanged = false;
        ApplyOriginalSprite();
    }


    private void ApplyOriginalSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (originalSprite == null)
        {
            Debug.LogWarning(
                "[Level3Painting] Original Sprite is missing on " +
                gameObject.name +
                "."
            );

            return;
        }

        spriteRenderer.sprite = originalSprite;
    }


    // =========================================================
    // STATUS
    // =========================================================

    public bool HasChanged()
    {
        return hasChanged;
    }
}
