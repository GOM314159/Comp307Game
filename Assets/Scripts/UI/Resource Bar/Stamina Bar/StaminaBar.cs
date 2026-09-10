using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    public Image preBar;
    public Image realBar;

    [Header("Settings")]
    public Color PreColour = Color.cyan;
    public float preSpeed = 1f;
    public Color RealColour = Color.blue;
    public float realSpeed = 2f;

    [Header("Player Stamina")]
    public PlayerStamina playerStamina;

    private float pre = 0;
    private float real = 0;

    void Start()
    {
        if (playerStamina == null)
            playerStamina = FindFirstObjectByType<PlayerStamina>();

        if (playerStamina != null)
        {
            pre = playerStamina.CurrentStamina;
            real = playerStamina.CurrentStamina;
        }

        Init();
    }

    public void Init()
    {
        if (preBar != null) preBar.color = PreColour;
        if (realBar != null) realBar.color = RealColour;
        Refresh();
    }

    void Update()
    {
        if (playerStamina == null) return;

        // Smoothly interpolate the backend values

        if (playerStamina.CurrentStamina > real)
        {
            preSpeed = 2f;
            realSpeed = 1f;
        }
        else
        {
            preSpeed = 1f;
            realSpeed = 2f;
        }
            pre = Mathf.Lerp(pre, playerStamina.CurrentStamina, preSpeed * Time.deltaTime);
        real = Mathf.Lerp(real, playerStamina.CurrentStamina, realSpeed * Time.deltaTime);

        Refresh();
    }

    void Refresh()
    {
        float maxStamina = playerStamina.MaxStamina;

        if (maxStamina <= 0) return;

        // This is what actually changes the visual fill!
        if (preBar != null) preBar.fillAmount = Mathf.Clamp01(pre / maxStamina);
        if (realBar != null) realBar.fillAmount = Mathf.Clamp01(real / maxStamina);
    }
}