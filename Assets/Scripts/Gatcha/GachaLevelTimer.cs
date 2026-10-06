using UnityEngine;

public class GachaLevelTimer : MonoBehaviour
{
    public static GachaLevelTimer Instance { get; private set; }

    [Header("Level point settings")]
    [SerializeField] private int startingLevelPoints = 100;
    [SerializeField] private int pointsLostPerMinute = 10;

    private float elapsedSeconds;
    private bool running;

    public int StartingLevelPoints => startingLevelPoints;
    public int PointsLostPerMinute => pointsLostPerMinute;
    public bool IsRunning => running;

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

    private void Update()
    {
        if (!running)
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            return;
        }

        GameStateManager.GameMode mode =
            GameStateManager.Instance.GetGameMode();

        if (mode != GameStateManager.GameMode.Case &&
            mode != GameStateManager.GameMode.BossFight)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
    }

    public void SetSettings(int newStartingPoints, int newPointsLostPerMinute)
    {
        startingLevelPoints = Mathf.Max(0, newStartingPoints);
        pointsLostPerMinute = Mathf.Max(0, newPointsLostPerMinute);
    }

    public void BeginLevel()
    {
        elapsedSeconds = 0f;
        running = true;
    }

    public int GetCurrentPoints()
    {
        return GachaPointCalculator.GetRemainingLevelPoints(
            elapsedSeconds,
            startingLevelPoints,
            pointsLostPerMinute);
    }

    public float GetElapsedSeconds()
    {
        return elapsedSeconds;
    }

    public int FinishLevel()
    {
        int points = GetCurrentPoints();
        running = false;
        return points;
    }

    public void CancelLevel()
    {
        running = false;
        elapsedSeconds = 0f;
    }
}
