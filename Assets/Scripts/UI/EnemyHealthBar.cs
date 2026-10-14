using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Slider from a Health component (e.g. Level 1 doll).
/// Place on the doll's world-space health Canvas or Slider.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Health health;

    [SerializeField]
    private Slider healthSlider;

    [SerializeField]
    private bool findHealthInParents = true;

    [SerializeField]
    private bool findSliderInChildren = true;

    [SerializeField]
    private bool hideWhenDead = true;

    [Header("Optional boss-fight visibility") ]
    [Tooltip("Enable this on theater puppet health bars so they remain hidden until GameMode becomes BossFight.")]
    [SerializeField] private bool onlyShowDuringBossFight = false;
    private bool bossFightVisibilityApplied;
    [SerializeField] private CanvasGroup visibilityCanvasGroup;


    private void Awake()
    {
        ResolveReferences();
        RefreshImmediate();
        ApplyBossFightVisibility();
    }


    private void LateUpdate()
    {
        ApplyBossFightVisibility();
        if (onlyShowDuringBossFight && !IsBossFightActive()) return;
        if (health == null || healthSlider == null)
        {
            ResolveReferences();

            if (health == null || healthSlider == null)
            {
                return;
            }
        }

        float max = health.GetMaxHealth();

        if (max <= 0f)
        {
            return;
        }

        healthSlider.value = Mathf.Clamp01(
            health.GetCurrentHealth() / max
        );

        if (hideWhenDead && health.IsDead())
        {
            gameObject.SetActive(false);
        }
    }


    private bool IsBossFightActive()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.GetGameMode() == GameStateManager.GameMode.BossFight;
    }

    private void ApplyBossFightVisibility()
    {
        if (!onlyShowDuringBossFight) return;
        bool shouldShow = IsBossFightActive();
        if (visibilityCanvasGroup == null)
        {
            visibilityCanvasGroup = GetComponent<CanvasGroup>();
            if (visibilityCanvasGroup == null) visibilityCanvasGroup = GetComponentInParent<CanvasGroup>();
            if (visibilityCanvasGroup == null) visibilityCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }
        if (visibilityCanvasGroup != null)
        {
            visibilityCanvasGroup.alpha = shouldShow ? 1f : 0f;
            visibilityCanvasGroup.interactable = shouldShow;
            visibilityCanvasGroup.blocksRaycasts = shouldShow;
        }
        else
        {
            // Do not deactivate this component's GameObject: it must keep
            // running so it can detect when the boss fight begins.
            foreach (UnityEngine.UI.Graphic graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.enabled = shouldShow;
        }
        bossFightVisibilityApplied = true;
    }

    private void ResolveReferences()
    {
        if (health == null && findHealthInParents)
        {
            health = GetComponentInParent<Health>();
        }

        if (healthSlider == null && findSliderInChildren)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponent<Slider>();
        }
    }


    private void RefreshImmediate()
    {
        if (health == null || healthSlider == null)
        {
            return;
        }

        float max = health.GetMaxHealth();

        if (max <= 0f)
        {
            return;
        }

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = Mathf.Clamp01(
            health.GetCurrentHealth() / max
        );
    }
}
