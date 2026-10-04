using UnityEngine;
using UnityEngine.UI;

public class Level3Painting : MonoBehaviour
{
    [Header("Painting")]
    [Tooltip("The SpriteRenderer displaying this painting.")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Tooltip(
        "Optional UI Image on the same object. Kept in sync with the " +
        "SpriteRenderer so both use the same art without resizing the frame."
    )]
    [SerializeField]
    private Image uiImage;

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

    private bool hasChanged;

    // Locked display size from the original painting setup.
    private Vector3 lockedLocalScale;
    private Vector2 lockedSpriteLocalSize;
    private Vector2 lockedUiSizeDelta;
    private bool hasLockedSize;


    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (uiImage == null)
        {
            uiImage = GetComponent<Image>();
        }

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogError(
                "[Level3Painting] No SpriteRenderer or UI Image found on " +
                gameObject.name +
                ". Assign one in the Inspector."
            );
            return;
        }

        if (originalSprite == null)
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                originalSprite = spriteRenderer.sprite;
            }
            else if (uiImage != null && uiImage.sprite != null)
            {
                originalSprite = uiImage.sprite;
            }
        }

        if (applyOriginalSpriteOnStart)
        {
            ApplySprite(originalSprite, unlockSize: true);
        }
        else
        {
            CaptureLockedSize(originalSprite);
        }
    }


    public void ChangeToSecondImage()
    {
        if (hasChanged)
        {
            return;
        }

        if (secondSprite == null)
        {
            Debug.LogError(
                "[Level3Painting] Second Sprite has not been assigned on " +
                gameObject.name +
                "."
            );
            return;
        }

        ApplySprite(secondSprite, unlockSize: false);
        hasChanged = true;
    }


    public void ResetPainting()
    {
        hasChanged = false;
        ApplySprite(originalSprite, unlockSize: false);
    }


    private void ApplyOriginalSprite()
    {
        ApplySprite(originalSprite, unlockSize: false);
    }


    private void ApplySprite(Sprite sprite, bool unlockSize)
    {
        if (sprite == null)
        {
            Debug.LogWarning(
                "[Level3Painting] Sprite is missing on " +
                gameObject.name +
                "."
            );
            return;
        }

        if (unlockSize || !hasLockedSize)
        {
            // Apply first, then lock the resulting frame size.
            SetSpriteSources(sprite);
            CaptureLockedSize(sprite);
            return;
        }

        SetSpriteSources(sprite);
        FitToLockedSize(sprite);
    }


    private void SetSpriteSources(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }

        if (uiImage != null)
        {
            // Keep the RectTransform size; stretch the new art into it.
            uiImage.preserveAspect = false;
            uiImage.sprite = sprite;
        }
    }


    private void CaptureLockedSize(Sprite sprite)
    {
        lockedLocalScale = transform.localScale;

        if (sprite != null)
        {
            lockedSpriteLocalSize = sprite.bounds.size;
        }
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            lockedSpriteLocalSize = spriteRenderer.sprite.bounds.size;
        }
        else
        {
            lockedSpriteLocalSize = Vector2.one;
        }

        if (transform is RectTransform rectTransform)
        {
            lockedUiSizeDelta = rectTransform.sizeDelta;
        }

        hasLockedSize = lockedSpriteLocalSize.x > 0.0001f &&
                        lockedSpriteLocalSize.y > 0.0001f;
    }


    private void FitToLockedSize(Sprite sprite)
    {
        if (!hasLockedSize || sprite == null)
        {
            return;
        }

        // Keep the UI frame size stable when an Image is present.
        if (transform is RectTransform rectTransform)
        {
            rectTransform.sizeDelta = lockedUiSizeDelta;
        }

        Vector2 newSpriteSize = sprite.bounds.size;

        if (newSpriteSize.x <= 0.0001f || newSpriteSize.y <= 0.0001f)
        {
            return;
        }

        // Rescale so the SpriteRenderer occupies the same local frame as
        // the original painting, regardless of the new sprite's pixel size.
        transform.localScale = new Vector3(
            lockedLocalScale.x *
                (lockedSpriteLocalSize.x / newSpriteSize.x),
            lockedLocalScale.y *
                (lockedSpriteLocalSize.y / newSpriteSize.y),
            lockedLocalScale.z
        );
    }


    public bool HasChanged()
    {
        return hasChanged;
    }
}
