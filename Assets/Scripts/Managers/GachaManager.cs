using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public class GachaReward
    {
        public string rewardID;
        public string displayName;
        public Rarity rarity;

        [Min(0f)]
        public float weight = 1f;

        [Tooltip("The actual item that goes into the player's inventory.")]
        public ItemData item;

        [Tooltip("Turn this off for things that can be rolled more than once.")]
        public bool unique = true;
    }

    [Header("Gacha Settings")]
    [SerializeField, Min(1)] private int rollCost = 30;

    [Header("Rewards")]
    [SerializeField] private List<GachaReward> rewards = new List<GachaReward>();

    [Header("Level point settings")]
    [SerializeField, Min(0)] private int startingLevelPoints = 100;
    [SerializeField, Min(0)] private int pointsLostPerMinute = 10;
    [SerializeField, Min(0)] private int levelCompletionReward = 50;
    [SerializeField, Min(0)] private int normalDeathPenalty = 10;

    [Header("Dragon Ball win")]
    [SerializeField] private GachaWinCutscene dragonBallWinCutscene;

    [Header("Debug")]
    [SerializeField] private bool debugGacha = true;

    private GachaReward lastReward;
    private bool delayedRollInProgress;

    public bool IsDelayedRollInProgress() => delayedRollInProgress;

    public int StartingLevelPoints => startingLevelPoints;
    public int PointsLostPerMinute => pointsLostPerMinute;
    public int LevelCompletionReward => levelCompletionReward;
    public int NormalDeathPenalty => normalDeathPenalty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GachaLevelTimer.Instance == null)
        {
            gameObject.AddComponent<GachaLevelTimer>();
        }
    }

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss += OnLoss;
        }
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -= OnLoss;
            GameStateManager.Instance.OnLoss += OnLoss;
        }

        SyncLevelTimerSettings();
        BuildDefaultRewardsIfNeeded();
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnLoss -= OnLoss;
        }
    }

    private void SyncLevelTimerSettings()
    {
        GachaLevelTimer timer = GachaLevelTimer.Instance;

        if (timer == null)
        {
            return;
        }

        timer.SetSettings(startingLevelPoints, pointsLostPerMinute);
    }

    private void BuildDefaultRewardsIfNeeded()
    {
        if (rewards.Count > 0)
        {
            return;
        }

        ItemData[] items = Resources.LoadAll<ItemData>("Gacha");

        foreach (ItemData item in items)
        {
            if (item == null)
            {
                continue;
            }

            string rewardID = item.name;
            Rarity rarity = Rarity.Rare;
            float weight = 1f;

            DragonBallItem dragonBall = item as DragonBallItem;

            if (dragonBall != null)
            {
                rewardID = "DragonBall" + dragonBall.dragonBallNumber;
                rarity = Rarity.Legendary;
                weight = 1f;
            }
            else if (item is KatanaItem)
            {
                rewardID = "Katana";
                rarity = Rarity.Epic;
            }
            else if (item is PanItem)
            {
                rewardID = "Pan";
                rarity = Rarity.Legendary;
            }
            else if (item is StormbreakerItem)
            {
                rewardID = "Stormbreaker";
                rarity = Rarity.Legendary;
            }
            else if (item is OneRingItem)
            {
                rewardID = "TheOneRing";
                rarity = Rarity.Legendary;
            }

            rewards.Add(new GachaReward
            {
                rewardID = rewardID,
                displayName = item.itemName,
                rarity = rarity,
                weight = weight,
                item = item,
                unique = true
            });
        }

        DebugGacha("Loaded " + rewards.Count + " default rewards.");
    }


    public bool CanRoll()
    {
        return GameStateManager.Instance != null &&
               GameStateManager.Instance.GetGachaCurrency() >= rollCost &&
               GetAvailableRewards().Count > 0;
    }

    public GachaReward Roll()
    {
        if (GameStateManager.Instance == null)
        {
            return null;
        }

        List<GachaReward> availableRewards = GetAvailableRewards();

        if (availableRewards.Count == 0)
        {
            Debug.LogWarning("[Gacha] Nothing left to roll.");
            return null;
        }

        if (GameStateManager.Instance.GetGachaCurrency() < rollCost)
        {
            DebugGacha("Not enough points for a spin.");
            return null;
        }

        GachaReward reward = SelectWeightedReward(availableRewards);

        if (reward == null || reward.item == null)
        {
            Debug.LogWarning("[Gacha] Reward is missing its item.");
            return null;
        }

        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("[Gacha] Could not find the player inventory.");
            return null;
        }

        if (!inventory.AddItem(reward.item, 1))
        {
            DebugGacha("Inventory is full, so the spin was cancelled.");
            return null;
        }

        if (!GameStateManager.Instance.SpendGachaCurrency(rollCost))
        {
            inventory.RemoveItemData(reward.item);
            return null;
        }

        if (reward.unique)
        {
            GameStateManager.Instance.MarkGachaRewardCollected(reward.rewardID);
        }

        lastReward = reward;

        DebugGacha(
            "Player got " + reward.displayName +
            " for " + rollCost + " points."
        );

        CheckDragonBalls();

        return reward;
    }

    /// <summary>
    /// Starts the interaction-based gacha roll. Currency is paid immediately,
    /// but the item is not added to the inventory until the delay completes.
    /// </summary>
    public bool TryStartDelayedRoll(
        MonoBehaviour runner,
        float delaySeconds,
        Action<GachaReward, bool> completed)
    {
        if (runner == null || delayedRollInProgress)
        {
            return false;
        }

        if (GameStateManager.Instance == null)
        {
            return false;
        }

        List<GachaReward> availableRewards = GetAvailableRewards();

        if (availableRewards.Count == 0)
        {
            DebugGacha("No rewards remain.");
            return false;
        }

        if (GameStateManager.Instance.GetGachaCurrency() < rollCost)
        {
            DebugGacha("Not enough points for a spin.");
            return false;
        }

        GachaReward reward = SelectWeightedReward(availableRewards);

        if (reward == null || reward.item == null)
        {
            return false;
        }

        if (!GameStateManager.Instance.SpendGachaCurrency(rollCost))
        {
            return false;
        }

        delayedRollInProgress = true;

        runner.StartCoroutine(
            CompleteDelayedRoll(
                reward,
                delaySeconds,
                completed
            )
        );

        DebugGacha(
            "Gacha started. Reward '" +
            reward.displayName +
            "' will be awarded after " +
            delaySeconds +
            " seconds."
        );

        return true;
    }

    private IEnumerator CompleteDelayedRoll(
        GachaReward reward,
        float delaySeconds,
        Action<GachaReward, bool> completed)
    {
        float remaining = Mathf.Max(0f, delaySeconds);

        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        delayedRollInProgress = false;

        PlayerInventory inventory =
            FindAnyObjectByType<PlayerInventory>();

        bool success =
            inventory != null &&
            inventory.AddItem(reward.item, 1);

        if (!success)
        {
            // Do not make the player lose their currency if the inventory
            // becomes full while waiting for the ten-second reward.
            GameStateManager.Instance.AddGachaCurrency(rollCost);

            DebugGacha(
                "Delayed reward could not be added. Currency refunded."
            );

            completed?.Invoke(reward, false);
            yield break;
        }

        if (reward.unique)
        {
            GameStateManager.Instance.MarkGachaRewardCollected(
                reward.rewardID
            );
        }

        lastReward = reward;

        DebugGacha(
            "Delayed gacha reward awarded: " +
            reward.displayName
        );

        CheckDragonBalls();

        completed?.Invoke(reward, true);
    }


    public List<GachaReward> RollMultiple(int amount)
    {
        List<GachaReward> results = new List<GachaReward>();

        if (amount <= 0)
        {
            return results;
        }

        for (int i = 0; i < amount; i++)
        {
            GachaReward reward = Roll();

            if (reward == null)
            {
                break;
            }

            results.Add(reward);
        }

        return results;
    }

    public List<GachaReward> GetAvailableRewards()
    {
        List<GachaReward> available = new List<GachaReward>();

        foreach (GachaReward reward in rewards)
        {
            if (reward == null || reward.item == null || reward.weight <= 0f)
            {
                continue;
            }

            if (reward.unique &&
                GameStateManager.Instance != null &&
                GameStateManager.Instance.HasGachaRewardBeenCollected(reward.rewardID))
            {
                continue;
            }

            available.Add(reward);
        }

        return available;
    }

    private GachaReward SelectWeightedReward(List<GachaReward> availableRewards)
    {
        float totalWeight = 0f;

        foreach (GachaReward reward in availableRewards)
        {
            totalWeight += reward.weight;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (GachaReward reward in availableRewards)
        {
            currentWeight += reward.weight;

            if (randomValue <= currentWeight)
            {
                return reward;
            }
        }

        return availableRewards[availableRewards.Count - 1];
    }

    public void BeginLevel()
    {
        SyncLevelTimerSettings();

        if (GachaLevelTimer.Instance != null)
        {
            GachaLevelTimer.Instance.BeginLevel();
        }
    }

    public int CompleteLevel()
    {
        if (GachaLevelTimer.Instance == null ||
            !GachaLevelTimer.Instance.IsRunning)
        {
            return 0;
        }

        int timeBonus = GachaLevelTimer.Instance.FinishLevel();
        int reward = GachaPointCalculator.GetLevelCompletionReward(
            timeBonus,
            levelCompletionReward);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.AddGachaCurrency(reward);
        }

        DebugGacha(
            "Level finished. +" + levelCompletionReward +
            " completion and +" + timeBonus +
            " time bonus = +" + reward + "."
        );

        return reward;
    }

    public int GetCurrentLevelPoints()
    {
        if (GachaLevelTimer.Instance == null)
        {
            return startingLevelPoints;
        }

        return GachaLevelTimer.Instance.GetCurrentPoints();
    }

    private void OnLoss(GameStateManager.LossReason reason)
    {
        if (reason == GameStateManager.LossReason.PlayerDeath &&
            GameStateManager.Instance != null &&
            GameStateManager.Instance.IsNormal())
        {
            int current = GameStateManager.Instance.GetGachaCurrency();
            int newValue = GachaPointCalculator.ApplyDeathPenalty(
                current,
                normalDeathPenalty);

            GameStateManager.Instance.SetGachaCurrency(newValue);

            DebugGacha(
                "Normal death. Lost " +
                (current - newValue) + " points."
            );
        }

        if (GachaLevelTimer.Instance != null)
        {
            GachaLevelTimer.Instance.CancelLevel();
        }
    }

    private void CheckDragonBalls()
    {
        if (GameStateManager.Instance == null ||
            !GameStateManager.Instance.HasAllDragonBalls())
        {
            return;
        }

        if (dragonBallWinCutscene != null)
        {
            dragonBallWinCutscene.Play();
        }
        else if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGachaWin();
        }
    }

    public GachaReward GetLastReward()
    {
        return lastReward;
    }

    public List<GachaReward> GetRewards()
    {
        return rewards;
    }

    public int GetRollCost()
    {
        return rollCost;
    }

    public void SetRollCost(int cost)
    {
        rollCost = Mathf.Max(0, cost);
    }

    public int GetCurrency()
    {
        return GameStateManager.Instance != null
            ? GameStateManager.Instance.GetGachaCurrency()
            : 0;
    }

    public void AddCurrency(int amount)
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.AddGachaCurrency(amount);
        }
    }

    private void DebugGacha(string message)
    {
        if (debugGacha)
        {
            Debug.Log("[Gacha] " + message);
        }
    }
}
