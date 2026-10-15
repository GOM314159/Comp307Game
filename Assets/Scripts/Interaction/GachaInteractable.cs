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
    [Tooltip("How long (real seconds) the 'You received: ...' text stays on screen.")]
    [SerializeField, Min(1f)] private float rewardDisplaySeconds = 5f;

    [Tooltip("If Reward Text is not assigned, look for this TMP object name (e.g. Obtained_Gacha_Item_label).")]
    [SerializeField] private string rewardTextObjectName = "Obtained_Gacha_Item_label";
    private Coroutine rewardMessageRoutine;
    private MonoBehaviour rewardMessageRunner;

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
            StopRewardMessageRoutine();
            string rewardMessage = reward != null
                ? "You received: " + reward.displayName
                : string.Empty;

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

                    Canvas canvas = parent.GetComponent<Canvas>();
                    if (canvas != null && !IsLoadingScreenActive())
                        canvas.enabled = true;

                    CanvasGroup group = parent.GetComponent<CanvasGroup>();
                    if (group != null)
                    {
                        group.alpha = 1f;
                    }
                    parent = parent.parent;
                }

                rewardText.enabled = true;
                Color textColor = rewardText.color;
                textColor.a = 1f;
                rewardText.color = textColor;
                rewardText.text = rewardMessage;
                rewardText.gameObject.SetActive(true);
                rewardText.transform.SetAsLastSibling();

                RectTransform textRect = rewardText.rectTransform;
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = new Vector2(0f, -350f);
                textRect.sizeDelta = new Vector2(900f, 90f);
                rewardText.alignment = TextAlignmentOptions.Center;
                rewardText.enableAutoSizing = true;
                rewardText.fontSizeMin = 18f;
                rewardText.fontSizeMax = 36f;
                rewardText.enableWordWrapping = false;
                rewardText.raycastTarget = false;
                rewardText.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
            }

            // Run on the persistent UI (it survives scene loads and cannot be
            // stopped by this machine being disabled). The routine also keeps
            // the label alive for the full duration, so nothing else can hide
            // it after a fraction of a second.
            rewardMessageRunner = PersistentGameplayUI.Instance != null
                ? (MonoBehaviour)PersistentGameplayUI.Instance
                : this;
            rewardMessageRoutine = rewardMessageRunner.StartCoroutine(
                KeepRewardMessageVisible(rewardMessage));

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

    private IEnumerator KeepRewardMessageVisible(string message)
    {
        float endTime = Time.unscaledTime + Mathf.Max(1f, rewardDisplaySeconds);

        while (Time.unscaledTime < endTime)
        {
            // While a loading screen is up all GUI is hidden, so do not let
            // the 5 seconds run out unseen: push the end time back.
            if (IsLoadingScreenActive())
            {
                endTime += Time.unscaledDeltaTime;
                yield return null;
                continue;
            }

            if (rewardText != null)
            {
                // Re-assert every frame: another script (HUD refresh, scene
                // change, overlay toggling) may deactivate the label.
                Transform node = rewardText.transform;
                while (node != null)
                {
                    if (!node.gameObject.activeSelf)
                        node.gameObject.SetActive(true);
                    node = node.parent;
                }

                if (!rewardText.enabled)
                    rewardText.enabled = true;

                if (rewardText.text != message)
                    rewardText.text = message;

                if (rewardText.color.a < 1f)
                {
                    Color c = rewardText.color;
                    c.a = 1f;
                    rewardText.color = c;
                }
            }

            yield return null;
        }

        SetRewardUI(false);

        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.gameObject.SetActive(false);
        }

        rewardMessageRoutine = null;
        rewardMessageRunner = null;
    }

    private static bool IsLoadingScreenActive()
    {
        return LoadingScreenManager.Instance != null &&
               LoadingScreenManager.Instance.AreGameplayCanvasesHidden;
    }

    private void StopRewardMessageRoutine()
    {
        if (rewardMessageRoutine != null && rewardMessageRunner != null)
            rewardMessageRunner.StopCoroutine(rewardMessageRoutine);

        rewardMessageRoutine = null;
        rewardMessageRunner = null;
    }

    private void ResolveRewardText()
    {
        if (PersistentGameplayUI.Instance != null)
        {
            TMP_Text persistentText = PersistentGameplayUI.Instance.FindText(rewardTextObjectName);
            if (persistentText != null)
            {
                rewardText = persistentText;
                return;
            }
        }
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
        if (rewardUI == null)
            return;

        // RewardUI is sometimes assigned to a shared HUD/container instead
        // of a dedicated reward panel. Never disable a container that also
        // owns the day counter, level timer, objective, or player bars.
        if (!active && ContainsOtherGameplayHUD(rewardUI.transform))
        {
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);
            return;
        }

        rewardUI.SetActive(active);
    }

    private bool ContainsOtherGameplayHUD(Transform root)
    {
        if (root == null)
            return false;

        if (dayOrGameplayHudText(root))
            return true;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            if (rewardText != null && texts[i] == rewardText)
                continue;

            string objectName = texts[i].gameObject.name.ToLowerInvariant();
            string labelName = texts[i].name.ToLowerInvariant();
            if (IsGameplayHudName(objectName) || IsGameplayHudName(labelName))
                return true;
        }

        return false;
    }

    private bool dayOrGameplayHudText(Transform root)
    {
        if (root.name == "DayText" || root.name == "ObjectiveText")
            return true;

        return false;
    }

    private bool IsGameplayHudName(string value)
    {
        return value.Contains("daytext") || value.Contains("daycounter") ||
               value.Contains("timer") || value.Contains("objective") ||
               value.Contains("healthbar") || value.Contains("staminabar") ||
               value.Contains("toolbar");
    }

    public override void ResetInteraction()
    {
        base.ResetInteraction();

        waitingForReward = false;

        StopAllCoroutines();
        StopRewardMessageRoutine();

        SetWaitingUI(false);
        SetRewardUI(false);
        if (rewardText != null)
        {
            rewardText.text = string.Empty;
            rewardText.gameObject.SetActive(false);
        }
    }
}
