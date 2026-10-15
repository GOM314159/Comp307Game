using TMPro;
using UnityEngine;

/// <summary>Shows the player's current spendable Gacha Point balance.</summary>
public class GachaCurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private string displayFormat = "GP: {0}";
    [SerializeField] private bool debugUpdates = true;

    private int lastDisplayedPoints = int.MinValue;

    private void Awake()
    {
        if (pointsText == null)
            pointsText = GetComponent<TMP_Text>();

        RefreshDisplay();
    }

    private void Update()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (pointsText == null || GameStateManager.Instance == null)
            return;

        int points = GameStateManager.Instance.GetGachaCurrency();
        if (points == lastDisplayedPoints)
            return;

        lastDisplayedPoints = points;
        pointsText.text = string.Format(displayFormat, points);

        if (debugUpdates)
            Debug.Log("[GachaCurrencyDisplay] Current GP display updated to " +
                      points + ".", this);
    }
}
