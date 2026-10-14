using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// World interaction for the physical Gacha machine.
/// Attach this to the Gacha machine root.
/// </summary>
public class GachaInteractable : WorldInteractable
{
    [Header("Gacha")]
    [SerializeField, Min(0f)] private float rewardDelay = 10f;

    [Header("Optional Gacha UI")]
    [Tooltip("Optional object shown while the 10-second reward delay is running.")]
    [SerializeField] private GameObject waitingUI;

    [Tooltip("Optional TMP text. Displays the remaining whole seconds.")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Optional object shown after the reward is successfully awarded.")]
    [SerializeField] private GameObject rewardUI;

    [SerializeField] private TMP_Text rewardText;
    [SerializeField, Min(0f)] private float rewardMessageDuration = 5f;

    [Tooltip("If Reward Text is not assigned, look for this TMP object name (e.g. Obtained_Gacha_Item_label).")]
    [SerializeField] private string rewardTextObjectName = "Obtained_Gacha_Item_label";
    private Coroutine rewardMessageRoutine;

    [Header("Debug")]
    [SerializeField] private bool debugGacha = true;

    private bool waitingForReward;

    protected override void Awake()
    {
        base.Awake();
        ResolveRewardText();
        SetWaitingUI(false);
        SetRewardUI(false);
        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.gameObject.SetActive(false);
        }
    }

    public override bool CanInteract()
    {
        if (waitingForReward)
            return false;

        if (GachaManager.Instance != null &&
            GachaManager.Instance.IsDelayedRollInProgress())
        {
            return false;
        }

        return base.CanInteract();
    }

    public override void Interact()
    {
        if (!CanInteract())
            return;

        if (GachaManager.Instance == null)
        {
            Debug.LogWarning(
                "[GachaInteractable] GachaManager is missing.",
                this
            );
            return;
        }

        bool started =
            GachaManager.Instance.TryStartDelayedRoll(
                this,
                rewardDelay,
                HandleRewardComplete
            );

        if (!started)
            return;

        base.Interact();
        waitingForReward = true;

        SetRewardUI(false);
        SetWaitingUI(true);

        StartCoroutine(UpdateCountdown());
    }

    private IEnumerator UpdateCountdown()
    {
        float remaining = rewardDelay;

        while (waitingForReward && remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;

            if (countdownText != null)
            {
                countdownText.text =
                    "Reward in " +
                    Mathf.CeilToInt(Mathf.Max(0f, remaining)) +
                    "...";
            }

            yield return null;
        }
    }

    private void HandleRewardComplete(
        GachaManager.GachaReward reward,
        bool success)
    {
        waitingForReward = false;
        SetWaitingUI(false);

        if (success)
        {
            ResolveRewardText();
            SetRewardUI(true);
            if (rewardMessageRoutine != null) StopCoroutine(rewardMessageRoutine);

            if (rewardText != null && reward != null)
            {
                // The label lives under the persistent Canvas, which may have
                // been hidden by a previous scene/cutscene. Reactivate its
                // hierarchy before showing the won-item message.
                Transform parent = rewardText.transform;
                while (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                        parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }

                rewardText.text = "You received: " + reward.displayName;
                rewardText.gameObject.SetActive(true);
            }

            rewardMessageRoutine = StartCoroutine(HideRewardMessageAfterDelay());

            if (debugGacha)
            {
                Debug.Log(
                    "[GachaInteractable] Reward received: " +
                    reward.displayName
                );
            }
        }
        else
        {
            SetRewardUI(false);

            if (debugGacha)
            {
                Debug.Log(
                    "[GachaInteractable] Reward could not be added; " +
                    "currency was refunded."
                );
            }
        }
    }

    private IEnumerator HideRewardMessageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(rewardMessageDuration);
        SetRewardUI(false);
        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.gameObject.SetActive(false);
        }
        rewardMessageRoutine = null;
    }

    private void ResolveRewardText()
    {
        if (rewardText != null) return;
        if (string.IsNullOrWhiteSpace(rewardTextObjectName)) return;

        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text candidate in allTexts)
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
            if (candidate.gameObject.name == rewardTextObjectName ||
                candidate.name == rewardTextObjectName)
            {
                rewardText = candidate;
                return;
            }
        }
    }

    private void SetWaitingUI(bool active)
    {
        if (waitingUI != null)
            waitingUI.SetActive(active);

        if (!active && countdownText != null)
            countdownText.text = string.Empty;
    }

    private void SetRewardUI(bool active)
    {
        if (rewardUI != null)
            rewardUI.SetActive(active);
    }

    public override void ResetInteraction()
    {
        base.ResetInteraction();

        waitingForReward = false;

        StopAllCoroutines();
        rewardMessageRoutine = null;

        SetWaitingUI(false);
        SetRewardUI(false);
        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.gameObject.SetActive(false);
        }
    }
}
