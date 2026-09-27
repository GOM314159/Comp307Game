using UnityEngine;
using TMPro;

/// <summary>
/// Keeps shared gameplay UI alive while gameplay scenes change.
///
/// This object should exist once in GameTestScene.
/// Do NOT duplicate it into Level1/Level2/Level3.
///
/// It provides shared UI references to scene-local interactables.
/// </summary>
public class PersistentGameplayUI : MonoBehaviour
{
    public static PersistentGameplayUI Instance { get; private set; }


    // =========================================================
    // SHARED UI
    // =========================================================

    [Header("Shared Gameplay UI")]

    [Tooltip(
        "The TextMeshProUGUI object used by all WorldInteractables " +
        "to display the interaction prompt."
    )]
    [SerializeField]
    private TextMeshProUGUI interactionText;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // SINGLETON
        // -----------------------------------------------------

        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        // -----------------------------------------------------
        // PERSIST ACROSS SCENES
        // -----------------------------------------------------

        DontDestroyOnLoad(gameObject);


        // -----------------------------------------------------
        // VALIDATE
        // -----------------------------------------------------

        if (interactionText == null)
        {
            Debug.LogWarning(
                "[PersistentGameplayUI] Interaction Text " +
                "has not been assigned."
            );
        }
    }


    // =========================================================
    // INTERACTION TEXT
    // =========================================================

    public TextMeshProUGUI GetInteractionText()
    {
        return interactionText;
    }
}