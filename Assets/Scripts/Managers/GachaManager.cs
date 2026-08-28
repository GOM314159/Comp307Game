using UnityEngine;
using System.Collections.Generic;

public class GachaManager : MonoBehaviour
{
    public static GachaManager Instance { get; private set; }


    // =========================================================
    // RARITY
    // =========================================================

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }


    // =========================================================
    // GACHA REWARD
    // =========================================================

    [System.Serializable]
    public class GachaReward
    {
        public string rewardID;

        public string displayName;

        public Rarity rarity;

        [Min(0f)]
        public float weight = 1f;

        public GameObject itemPrefab;
    }


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Gacha Settings")]

    [SerializeField]
    [Min(1)]
    private int rollCost = 1;


    [SerializeField]
    [Min(1)]
    private int rollsPerPull = 1;


    // =========================================================
    // REWARDS
    // =========================================================

    [Header("Rewards")]

    [SerializeField]
    private List<GachaReward> rewards =
        new List<GachaReward>();


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool debugGacha = true;


    // =========================================================
    // LAST RESULT
    // =========================================================

    private GachaReward lastReward;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // CAN ROLL
    // =========================================================

    public bool CanRoll()
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError(
                "[Gacha] GameStateManager not found."
            );

            return false;
        }

        return GameStateManager.Instance
            .GetGachaCurrency() >= rollCost;
    }


    // =========================================================
    // SINGLE ROLL
    // =========================================================

    public GachaReward Roll()
    {
        if (!CanRoll())
        {
            Debug.Log(
                "[Gacha] Not enough currency."
            );

            return null;
        }

        if (rewards.Count == 0)
        {
            Debug.LogWarning(
                "[Gacha] No rewards configured."
            );

            return null;
        }

        bool spent =
            GameStateManager.Instance
                .SpendGachaCurrency(rollCost);

        if (!spent)
        {
            return null;
        }

        GachaReward reward =
            SelectWeightedReward();

        lastReward = reward;

        DebugGacha(
            "Player received: "
            + reward.displayName
            + " ["
            + reward.rarity
            + "]"
        );

        return reward;
    }


    // =========================================================
    // MULTI ROLL
    // =========================================================

    public List<GachaReward> RollMultiple(
        int amount)
    {
        List<GachaReward> results =
            new List<GachaReward>();

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


    // =========================================================
    // WEIGHTED SELECTION
    // =========================================================

    private GachaReward SelectWeightedReward()
    {
        float totalWeight = 0f;

        foreach (GachaReward reward in rewards)
        {
            if (reward == null)
            {
                continue;
            }

            if (reward.weight <= 0f)
            {
                continue;
            }

            totalWeight += reward.weight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogError(
                "[Gacha] Total reward weight is zero."
            );

            return null;
        }

        float randomValue =
            Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (GachaReward reward in rewards)
        {
            if (reward == null ||
                reward.weight <= 0f)
            {
                continue;
            }

            currentWeight += reward.weight;

            if (randomValue <= currentWeight)
            {
                return reward;
            }
        }

        return rewards[rewards.Count - 1];
    }


    // =========================================================
    // REWARD INFORMATION
    // =========================================================

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
        if (cost <= 0)
        {
            return;
        }

        rollCost = cost;
    }


    // =========================================================
    // CURRENCY
    // =========================================================

    public int GetCurrency()
    {
        if (GameStateManager.Instance == null)
        {
            return 0;
        }

        return GameStateManager.Instance
            .GetGachaCurrency();
    }


    public void AddCurrency(int amount)
    {
        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.Instance
            .AddGachaCurrency(amount);
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugGacha(string message)
    {
        if (!debugGacha)
        {
            return;
        }

        Debug.Log("[Gacha] " + message);
    }
}