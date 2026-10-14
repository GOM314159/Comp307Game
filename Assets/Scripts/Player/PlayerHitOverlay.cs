using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHitOverlay : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image overlayImage;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.65f;
    [SerializeField] private Color hitColor = new Color(1f, 0.03f, 0.03f, 0.62f);
    [SerializeField] private bool forceFullScreenOverlay = true;
    [SerializeField] private int overlaySortingOrder = 32760;

    private float lastHealth = -1f;
    private Coroutine routine;

    private void Awake()
    {
        ResolveReferences();
        HideOverlay();
    }

    private void OnEnable()
    {
        ResolveReferences();
        lastHealth = playerHealth != null ? playerHealth.GetCurrentHealth() : -1f;
    }

    private void Update()
    {
        if (playerHealth == null)
            ResolveReferences();

        if (playerHealth == null)
            return;

        float health = playerHealth.GetCurrentHealth();
        if (lastHealth >= 0f && health < lastHealth)
            Flash();

        lastHealth = health;
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerHealth = player.GetComponentInChildren<PlayerHealth>(true);
            }
        }

        if (overlayImage == null)
        {
            // Common setup: this component is attached directly to the Image
            // object. Previously that Image was explicitly excluded below,
            // leaving overlayImage null and making every hit flash invisible.
            overlayImage = GetComponent<Image>();
        }

        if (overlayImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image candidate in images)
            {
                if (candidate != null && candidate.gameObject != gameObject)
                {
                    overlayImage = candidate;
                    break;
                }
            }
        }

        if (overlayImage == null)
            return;

        overlayImage.raycastTarget = false;
        if (forceFullScreenOverlay)
        {
            RectTransform rect = overlayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;

            Canvas canvas = overlayImage.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
                canvas.overrideSorting = true;
                canvas.sortingOrder = overlaySortingOrder;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
    }

    public void Flash()
    {
        ResolveReferences();
        if (overlayImage == null)
        {
            Debug.LogWarning("[PlayerHitOverlay] No overlay Image found. Assign the hit-overlay Image to Overlay Image.", this);
            return;
        }

        // A persistent UI parent can be disabled during scene transitions.
        // Reactivate the image hierarchy before attempting to display a hit.
        Transform parent = overlayImage.transform;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
                parent.gameObject.SetActive(true);
            parent = parent.parent;
        }

        Canvas canvas = overlayImage.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = overlaySortingOrder;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        overlayImage.enabled = true;
        overlayImage.color = hitColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        HideOverlay();
        routine = null;
    }

    private void HideOverlay()
    {
        if (overlayImage == null)
            return;

        Color color = hitColor;
        color.a = 0f;
        overlayImage.color = color;
        overlayImage.enabled = false;
    }
}
