using System.Collections.Generic;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public enum AchievementId
    {
        Day1Complete,
        Day2Complete,
        Day3Complete,
        Day4Complete,
        HardcoreFinish,
        SecretEnding1,
        SecretEnding2DragonBalls,
        AllAchievements
    }

    private const string Prefix = "Achievement_";

    private static readonly AchievementId[] MainAchievements =
    {
        AchievementId.Day1Complete,
        AchievementId.Day2Complete,
        AchievementId.Day3Complete,
        AchievementId.Day4Complete,
        AchievementId.HardcoreFinish,
        AchievementId.SecretEnding1,
        AchievementId.SecretEnding2DragonBalls
    };

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

    public void UnlockDayCompletion(int day)
    {
        switch (day)
        {
            case 1: Unlock(AchievementId.Day1Complete); break;
            case 2: Unlock(AchievementId.Day2Complete); break;
            case 3: Unlock(AchievementId.Day3Complete); break;
            case 4: Unlock(AchievementId.Day4Complete); break;
        }
    }

    public void UnlockHardcoreCompletion()
    {
        Unlock(AchievementId.HardcoreFinish);
    }

    public void UnlockSecretEnding1()
    {
        Unlock(AchievementId.SecretEnding1);
    }

    public void UnlockSecretEnding2()
    {
        Unlock(AchievementId.SecretEnding2DragonBalls);
    }

    public bool IsUnlocked(AchievementId id)
    {
        return PlayerPrefs.GetInt(Prefix + id, 0) == 1;
    }

    public void Unlock(AchievementId id)
    {
        if (IsUnlocked(id))
        {
            return;
        }

        PlayerPrefs.SetInt(Prefix + id, 1);
        PlayerPrefs.Save();

        Debug.Log("[Achievement] Unlocked: " + id);
        CheckAllAchievements();
    }

    private void CheckAllAchievements()
    {
        foreach (AchievementId id in MainAchievements)
        {
            if (!IsUnlocked(id))
            {
                return;
            }
        }

        if (!IsUnlocked(AchievementId.AllAchievements))
        {
            PlayerPrefs.SetInt(
                Prefix + AchievementId.AllAchievements,
                1);
            PlayerPrefs.Save();
            Debug.Log("[Achievement] Unlocked: AllAchievements");
        }
    }

    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (AchievementId id in System.Enum.GetValues(typeof(AchievementId)))
        {
            if (IsUnlocked(id)) count++;
        }
        return count;
    }

    public int GetTotalAchievementCount()
    {
        return System.Enum.GetValues(typeof(AchievementId)).Length;
    }

    public void ResetAchievements()
    {
        foreach (AchievementId id in System.Enum.GetValues(typeof(AchievementId)))
        {
            PlayerPrefs.DeleteKey(Prefix + id);
        }
        PlayerPrefs.Save();
    }
}
