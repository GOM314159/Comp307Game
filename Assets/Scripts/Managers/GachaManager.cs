using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    [Tooltip("Scene containing the Dragon Ball ending VideoPlayer.")]
    [SerializeField] private string dragonBallCutsceneSceneName = "GamePlayTest";
    [Tooltip("GameObject name of the Dragon Ball ending VideoPlayer.")]
    [SerializeField] private string dragonBallCutsceneObjectName = "GachaWinCutscene";
    private VideoPlayer dragonBallWinVideoPlayer;
    [Tooltip("Name of the end screen GameObject to enable when the Dragon Ball video finishes.")]
    [SerializeField] private string dragonBallEndScreenObjectName = "EndScreen";
    private bool dragonBallEndingStarted;

    [Header("Debug")]
    [SerializeField] private bool debugGacha = true;

    private GachaReward lastReward;
    private bool delayedRollInProgress;
    private int pendingCompletedLevelReward;
    private bool hasPendingCompletedLevelReward;

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

        // The ending player is authored in GamePlayTest, while this manager
        // survives scene changes. Keep that player (and its assigned clip)
        // alive with the manager instead of retaining a reference to an
        // object Unity will destroy when the source scene unloads.
        ResolveDragonBallVideoPlayer();
        if (dragonBallWinVideoPlayer != null)
        {
            Transform videoTransform = dragonBallWinVideoPlayer.transform;
            videoTransform.SetParent(null, true);
            DontDestroyOnLoad(dragonBallWinVideoPlayer.gameObject);
        }

        if (GachaLevelTimer.Instance == null)
        {
            gameObject.AddComponent<GachaLevelTimer>();
        }
    }

    private void ResolveDragonBallVideoPlayer()
    {
        if (dragonBallWinVideoPlayer != null)
            return;

        // The player is detached and persisted after the first lookup, so
        // subsequent scene changes resolve it by its typed object name.
        foreach (VideoPlayer player in Resources.FindObjectsOfTypeAll<VideoPlayer>())
        {
            if (player != null && player.gameObject.scene.IsValid() &&
                player.gameObject.scene.name == "DontDestroyOnLoad" &&
                player.gameObject.name == dragonBallCutsceneObjectName)
            {
                dragonBallWinVideoPlayer = player;
                return;
            }
        }

        Scene sourceScene = SceneManager.GetSceneByName(dragonBallCutsceneSceneName);
        if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            return;

        foreach (GameObject root in sourceScene.GetRootGameObjects())
        {
            foreach (VideoPlayer player in root.GetComponentsInChildren<VideoPlayer>(true))
            {
                if (player.gameObject.name == dragonBallCutsceneObjectName)
                {
                    dragonBallWinVideoPlayer = player;
                    return;
                }
            }
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
        NormalizeDragonBallRewardIDs();
    }

    private void NormalizeDragonBallRewardIDs()
    {
        // Inspector-authored reward lists can contain stale numeric IDs.
        // Dragon Ball completion uses stable names, so always derive these
        // IDs from the assigned DragonBallItem asset.
        foreach (GachaReward reward in rewards)
        {
            if (reward == null)
                continue;

            DragonBallItem dragonBall = reward.item as DragonBallItem;
            if (dragonBall == null)
                continue;

            reward.rewardID = "DragonBall" + dragonBall.dragonBallNumber;
        }
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
            DebugGacha("All unique rewards have been collected. No repeat reward will be issued.");
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

        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);

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
            DebugGacha("All unique rewards have been collected. Further rolls are disabled.");
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
            FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);

        bool success =
            inventory != null &&
            !IsAlreadyOwned(reward) &&
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

    private bool IsAlreadyOwned(GachaReward reward)
    {
        if (reward == null || reward.item == null || GameStateManager.Instance == null)
            return false;

        if (reward.unique && GameStateManager.Instance.HasGachaRewardBeenCollected(reward.rewardID))
            return true;

        PlayerInventory inventory = FindAnyObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        // Item identity is the final duplicate guard even if two reward IDs
        // happen to point at the same ItemData asset.
        return inventory != null && inventory.HasItem(reward.item);
    }

    public List<GachaReward> GetAvailableRewards()
    {
        List<GachaReward> available = new List<GachaReward>();

        foreach (GachaReward reward in rewards)
        {
            if (reward == null || reward.item == null)
            {
                continue;
            }

            if (IsAlreadyOwned(reward))
                continue;

            available.Add(reward);
        }

        return available;
    }

    private List<GachaReward> GetValidRewardsForFallback()
    {
        List<GachaReward> valid = new List<GachaReward>();
        foreach (GachaReward reward in rewards)
        {
            if (reward != null && reward.item != null)
                valid.Add(reward);
        }
        return valid;
    }

    private GachaReward SelectWeightedReward(List<GachaReward> availableRewards)
    {
        float totalWeight = 0f;

        // A zero weight in the Inspector is a common accidental configuration.
        // If at least one positive weight exists, zero-weight entries stay out
        // of the draw. If all weights are zero, treat each valid item equally.
        bool hasPositiveWeight = false;
        foreach (GachaReward reward in availableRewards)
        {
            if (reward != null && reward.weight > 0f)
            {
                hasPositiveWeight = true;
                totalWeight += reward.weight;
            }
        }

        if (!hasPositiveWeight)
        {
            return availableRewards.Count > 0
                ? availableRewards[UnityEngine.Random.Range(0, availableRewards.Count)]
                : null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (GachaReward reward in availableRewards)
        {
            if (reward == null || reward.weight <= 0f)
                continue;

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

        pendingCompletedLevelReward = reward;
        hasPendingCompletedLevelReward = true;

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

    public bool TryTakeCompletedLevelReward(out int reward)
    {
        reward = pendingCompletedLevelReward;
        if (!hasPendingCompletedLevelReward)
            return false;

        hasPendingCompletedLevelReward = false;
        pendingCompletedLevelReward = 0;
        return true;
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
        if (dragonBallEndingStarted ||
            GameStateManager.Instance == null ||
            !GameStateManager.Instance.HasAllDragonBalls())
            return;

        dragonBallEndingStarted = true;
        DebugGacha("All seven Dragon Balls collected; ending video starts in 5 seconds.");
        StartCoroutine(PlayDragonBallEndingAfterDelay());
    }

    public void ResetDragonBallEndingState()
    {
        dragonBallEndingStarted = false;
    }

    private IEnumerator PlayDragonBallEndingAfterDelay()
    {
        // The final Dragon Ball is awarded inside the gacha callback. Give
        // the inventory/UI and persistent video presentation five seconds to
        // settle before taking over the screen.
        yield return new WaitForSecondsRealtime(5f);
        PlayDragonBallEnding();
    }

    private void PlayDragonBallEnding()
    {
        ResolveDragonBallVideoPlayer();
        if (dragonBallWinVideoPlayer == null)
        {
            Debug.LogError("[GachaManager] Could not find GameObject '" + dragonBallCutsceneObjectName +
                "' in scene '" + dragonBallCutsceneSceneName + "' or among persistent objects.", this);
            return;
        }

        if (dragonBallWinVideoPlayer.clip == null)
        {
            Debug.LogError("[GachaManager] The assigned Dragon Ball VideoPlayer has no VideoClip.", this);
            return;
        }

        // This manager plays the persistent VideoPlayer directly, bypassing
        // GachaWinCutscene.Play(), so close inventory here before taking over
        // the screen and disabling player input.
        PlayerInventory playerInventory = FindAnyObjectByType<PlayerInventory>();
        if (playerInventory != null && playerInventory.IsInventoryOpen())
        {
            playerInventory.CloseInventory();
            Debug.Log("[GachaManager] Closed the open inventory before starting the Dragon Ball ending.", this);
        }

        // Put the game into cutscene mode before playing the video directly.
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Cutscene);

        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.Cutscene);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetGameplayHUDVisible(false);

        dragonBallWinVideoPlayer.playOnAwake = false;
        dragonBallWinVideoPlayer.isLooping = false;
        dragonBallWinVideoPlayer.enabled = true;
        dragonBallWinVideoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        dragonBallWinVideoPlayer.targetCameraAlpha = 1f;
        if (Camera.main != null)
            dragonBallWinVideoPlayer.targetCamera = Camera.main;
        if (!dragonBallWinVideoPlayer.gameObject.activeInHierarchy)
            dragonBallWinVideoPlayer.gameObject.SetActive(true);
        dragonBallWinVideoPlayer.loopPointReached -= OnDragonBallVideoFinished;
        dragonBallWinVideoPlayer.loopPointReached += OnDragonBallVideoFinished;
        dragonBallWinVideoPlayer.Stop();
        dragonBallWinVideoPlayer.targetCameraAlpha = 1f;
        dragonBallWinVideoPlayer.Play();
    }

    private void OnDragonBallVideoFinished(VideoPlayer source)
    {
        if (source != null)
        {
            source.loopPointReached -= OnDragonBallVideoFinished;
            source.Stop();
            source.targetCameraAlpha = 0f;
            source.renderMode = VideoRenderMode.APIOnly;
            source.enabled = false;
            source.gameObject.SetActive(false);
            Debug.Log("[GachaManager] Hid Dragon Ball ending video '" + source.gameObject.name +
                "': activeSelf=" + source.gameObject.activeSelf + ", enabled=" + source.enabled +
                ", cameraAlpha=" + source.targetCameraAlpha, source);
        }

        GameObject endScreen = null;
        if (!string.IsNullOrWhiteSpace(dragonBallEndScreenObjectName))
        {
            endScreen = GameObject.Find(dragonBallEndScreenObjectName);
            if (endScreen == null)
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (Transform item in allTransforms)
                {
                    if (item == null || !item.gameObject.scene.IsValid())
                        continue;

                    if (item.name == dragonBallEndScreenObjectName)
                    {
                        endScreen = item.gameObject;
                        break;
                    }
                }
            }
        }

        if (endScreen != null)
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.SetGameplayHUDVisible(false);
            // The end screen is interactive UI. Leave cutscene mode so the
            // cursor controller no longer locks the mouse to gameplay.
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.SetGameMode(GameStateManager.GameMode.Lobby);
            if (PlayerStateManager.Instance != null)
                PlayerStateManager.Instance.SetState(PlayerStateManager.PlayerState.Cutscene);
            endScreen.SetActive(true);
            Debug.Log("[GachaManager] Dragon Ball end screen activation requested. activeSelf=" +
                endScreen.activeSelf + ", activeInHierarchy=" + endScreen.activeInHierarchy +
                ", parent=" + (endScreen.transform.parent != null ? endScreen.transform.parent.name : "<root>"), this);
            if (GameUICursorController.Instance != null)
                GameUICursorController.Instance.ResetControllerCursorForUI();
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            Debug.LogError("[GachaManager] Dragon Ball video finished, but no EndScreen GameObject named '" + dragonBallEndScreenObjectName + "' was found.", this);
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
