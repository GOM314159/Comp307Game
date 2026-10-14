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


    private void Awake()
    {
        ResolveReferences();
        RefreshImmediate();
    }


    private void LateUpdate()
    {
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
