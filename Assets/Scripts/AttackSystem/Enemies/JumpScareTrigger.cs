using UnityEngine;
using UnityEngine.Events;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Day")]
    [SerializeField]
    private int requiredDay = 1;

    [Header("Trigger")]
    [SerializeField]
    private bool triggerOnce = true;

    [Header("Jumpscare")]
    [SerializeField]
    private UnityEvent onJumpscare;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            return;
        }

        if (GameStateManager.Instance.GetCurrentDay() != requiredDay)
        {
            return;
        }

        hasTriggered = true;

        Debug.Log("- Jumpscare to be added");

        onJumpscare?.Invoke();
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}