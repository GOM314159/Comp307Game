using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    public Image preBar;
    public Image realBar;
    public RectMask2D preMask;
    public RectMask2D realMask;

    [Header("Settings")]
    public Color PreColour = Color.cyan;
    public float preSpeed = 2f;
    public Color RealColour = Color.blue;
    public float realSpeed = 4f;

    [Header("Player Stamina")]
    public PlayerStamina playerStamina;

    private float maskWidth = 0;
    private float pre = 0;
    private float real = 0;

    void Awake()
    {
        if (preMask != null)
            maskWidth = preMask.rectTransform.rect.width;
    }

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

    public float RatioPre()
    {
        if (playerStamina == null || playerStamina.MaxStamina == 0) return 0;
        return Mathf.Clamp01(pre / playerStamina.MaxStamina);
    }

    public float RatioReal()
    {
        if (playerStamina == null || playerStamina.MaxStamina == 0) return 0;
        return Mathf.Clamp01(real / playerStamina.MaxStamina);
    }

    void Update()
    {
        if (playerStamina == null) return;

        // Smoothly interpolate UI fill values
        pre = Mathf.Lerp(pre, playerStamina.CurrentStamina, preSpeed * Time.deltaTime);
        real = Mathf.Lerp(real, playerStamina.CurrentStamina, realSpeed * Time.deltaTime);

        Refresh();
    }

    void Refresh()
    {
        if (preMask == null || realMask == null) return;

        // RectMask2D padding: (Left, Bottom, Right, Top)
        // Adjust the right padding to crop the bar as stamina drops
        float paddingPre = (1f - RatioPre()) * maskWidth;
        preMask.padding = new Vector4(0, 0, paddingPre, 0);

        float paddingReal = (1f - RatioReal()) * maskWidth;
        realMask.padding = new Vector4(0, 0, paddingReal, 0);
    }
}