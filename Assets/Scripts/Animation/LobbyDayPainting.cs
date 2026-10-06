using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyDayPainting : MonoBehaviour
{
    [Header("Painting")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField]
    private Sprite originalSprite;

    [SerializeField]
    private Sprite day4Sprite;

    private GameStateManager gameStateManager;
    private bool subscribed = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // Automatically find the SpriteRenderer if one was not
        // assigned in the Inspector.
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[LobbyDayPainting] No SpriteRenderer found on " +
                gameObject.name +
                ". Assign the SpriteRenderer in the Inspector."
            );

            return;
        }

        // If no original sprite was manually assigned,
        // remember whatever sprite the painting starts with.
        if (originalSprite == null)
        {
            originalSprite = spriteRenderer.sprite;
        }
    }


    private void Start()
    {
        ConnectToGameStateManager();

        ApplyCurrentDay();
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnsubscribeFromGameStateManager();
    }


    // =========================================================
    // GAME STATE MANAGER
    // =========================================================

    private void ConnectToGameStateManager()
    {
        if (subscribed)
        {
            return;
        }

        gameStateManager = GameStateManager.Instance;

        // Persistent manager should normally already exist.
        // This fallback handles cases where it has not been
        // initialized yet.
        if (gameStateManager == null)
        {
            gameStateManager =
                FindAnyObjectByType<GameStateManager>();
        }

        if (gameStateManager == null)
        {
            Debug.LogError(
                "[LobbyDayPainting] Could not find the persistent " +
                "GameStateManager."
            );

            return;
        }

        gameStateManager.OnDayChanged += OnDayChanged;

        subscribed = true;

        Debug.Log(
            "[LobbyDayPainting] Connected to GameStateManager. " +
            "Current Day: " +
            gameStateManager.GetCurrentDay()
        );
    }


    private void UnsubscribeFromGameStateManager()
    {
        if (!subscribed || gameStateManager == null)
        {
            return;
        }

        gameStateManager.OnDayChanged -= OnDayChanged;

        subscribed = false;
        gameStateManager = null;
    }


    // =========================================================
    // SCENE LOADING
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        // The painting may be recreated when the Lobby loads.
        // Reconnect to the persistent GameStateManager and make
        // sure the correct sprite is displayed for the current day.
        ConnectToGameStateManager();

        ApplyCurrentDay();
    }


    // =========================================================
    // DAY
    // =========================================================

    private void OnDayChanged(int day)
    {
        Debug.Log(
            "[LobbyDayPainting] Day changed to Day " +
            day +
            ". Updating painting."
        );

        ApplyDay(day);
    }


    private void ApplyCurrentDay()
    {
        if (gameStateManager == null)
        {
            ConnectToGameStateManager();
        }

        if (gameStateManager == null)
        {
            Debug.LogError(
                "[LobbyDayPainting] Cannot apply painting because " +
                "GameStateManager is unavailable."
            );

            return;
        }

        int currentDay =
            gameStateManager.GetCurrentDay();

        Debug.Log(
            "[LobbyDayPainting] Applying painting for Day " +
            currentDay
        );

        ApplyDay(currentDay);
    }


    private void ApplyDay(int day)
    {
        if (spriteRenderer == null)
        {
            Debug.LogError(
                "[LobbyDayPainting] SpriteRenderer is missing."
            );

            return;
        }


        // =====================================================
        // DAY 4
        // =====================================================

        if (day == 4)
        {
            if (day4Sprite == null)
            {
                Debug.LogError(
                    "[LobbyDayPainting] Day 4 Sprite has NOT been " +
                    "assigned in the Inspector."
                );

                return;
            }

            spriteRenderer.sprite = day4Sprite;

            Debug.Log(
                "[LobbyDayPainting] SUCCESS: Day 4 painting sprite applied."
            );

            return;
        }


        // =====================================================
        // DAYS 1-3
        // =====================================================

        if (originalSprite == null)
        {
            Debug.LogError(
                "[LobbyDayPainting] Original Sprite is missing."
            );

            return;
        }

        spriteRenderer.sprite = originalSprite;

        Debug.Log(
            "[LobbyDayPainting] Original painting applied for Day " +
            day
        );
    }
}