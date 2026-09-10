using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HealthBar : MonoBehaviour
{

    [Header("References")]
    public Image preBar;
    public Image realBar;

    [Header("Settings")]
    public Color PreColour = new Color(0.94f, 0.5f, 0.5f);
    public float preSpeed = 1f;
    public Color RealColour = Color.red;
    public float realSpeed = 2f;

    [Header("PlayerHealth")]
    public PlayerHealth playerHealth;

    private float pre = 0;
    private float real = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            pre = playerHealth.GetCurrentHealth();
            real = playerHealth.GetCurrentHealth();
        }
        Init();
    }

    public void Init()
    {
        if (preBar != null) preBar.color = PreColour;
        if (realBar != null) realBar.color = RealColour;
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerHealth == null) return;

        // Smoothly interpolate the backend values
        pre = Mathf.Lerp(pre, playerHealth.GetCurrentHealth(), preSpeed * Time.deltaTime);
        real = Mathf.Lerp(real, playerHealth.GetCurrentHealth(), realSpeed * Time.deltaTime);

        Refresh();
    }

    void Refresh()
    {
        float maxHealth = playerHealth.GetMaxHealth();

        if (playerHealth.GetMaxHealth() <= 0) return;


        if (playerHealth.GetCurrentHealth() > real)
        {
            preSpeed = 2f;
            realSpeed = 1f;
        }
        else
        {
            preSpeed = 1f;
            realSpeed = 2f;
        }

        // This is what actually changes the visual fill!
        if (preBar != null) preBar.fillAmount = Mathf.Clamp01(pre / playerHealth.GetMaxHealth());
        if (realBar != null) realBar.fillAmount = Mathf.Clamp01(real / playerHealth.GetMaxHealth());
    }

}
