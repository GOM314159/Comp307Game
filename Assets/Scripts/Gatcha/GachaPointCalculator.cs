public static class GachaPointCalculator
{
    public static int GetRemainingLevelPoints(
        float elapsedSeconds,
        int startingPoints,
        int pointsLostPerMinute)
    {
        if (startingPoints <= 0)
        {
            return 0;
        }

        if (pointsLostPerMinute <= 0)
        {
            return startingPoints;
        }

        int minutesPassed =
            elapsedSeconds > 0f
                ? (int)(elapsedSeconds / 60f)
                : 0;

        int pointsLost =
            minutesPassed * pointsLostPerMinute;

        return UnityEngine.Mathf.Max(
            0,
            startingPoints - pointsLost);
    }

    public static int GetLevelCompletionReward(
        int remainingTimePoints,
        int completionReward)
    {
        return UnityEngine.Mathf.Max(0, remainingTimePoints)
            + UnityEngine.Mathf.Max(0, completionReward);
    }

    public static int ApplyDeathPenalty(
        int currentCurrency,
        int deathPenalty)
    {
        return UnityEngine.Mathf.Max(
            0,
            currentCurrency - UnityEngine.Mathf.Max(0, deathPenalty));
    }
}
