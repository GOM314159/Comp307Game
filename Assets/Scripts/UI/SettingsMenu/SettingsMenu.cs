using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared settings menu used from either the Main Menu or Pause Menu.
///
/// UI:
/// - Close button
/// - Controls button
/// - Camera sensitivity slider
/// - Master volume slider
///
/// The settings are stored in PlayerPrefs so the values survive scene
/// changes and restarting the game.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    private enum OpenOrigin
    {
        MainMenu,
        PauseMenu
    }

    [Header("Settings UI")]
    [SerializeField] private GameObject settingsRoot;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Sliders")]
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private Slider volumeSlider;

    [Header("Slider Ranges")]
    [SerializeField] private float minimumSensitivity = 0.1f;
    [SerializeField] private float maximumSensitivity = 5f;

    [Header("Optional Return Containers")]
    [Tooltip("Main menu panel that should be shown again when Close is pressed.")]
    [SerializeField] private GameObject mainMenuContainer;

    [Tooltip("Pause menu panel that should be shown again when Close is pressed.")]
    [SerializeField] private GameObject pauseMenuContainer;

    private const string SensitivityKey = "Settings.CameraSensitivity";
    private const string VolumeKey = "Settings.MasterVolume";

    private OpenOrigin openOrigin = OpenOrigin.MainMenu;
    private bool suppressSliderCallbacks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.minValue = minimumSensitivity;
            cameraSensitivitySlider.maxValue = maximumSensitivity;
            cameraSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (settingsRoot != null)
            settingsRoot.SetActive(false);
    }

    private void Start()
    {
        RefreshValuesFromSystems();
    }

    private void OnDestroy()
    {
        if (cameraSensitivitySlider != null)
            cameraSensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (Instance == this)
            Instance = null;
    }

    public void OpenFromMainMenu()
    {
        openOrigin = OpenOrigin.MainMenu;

        if (mainMenuContainer != null)
            mainMenuContainer.SetActive(false);

        if (pauseMenuContainer != null)
            pauseMenuContainer.SetActive(false);

        Open();
    }

    public void OpenFromPauseMenu()
    {
        openOrigin = OpenOrigin.PauseMenu;

        if (mainMenuContainer != null)
            mainMenuContainer.SetActive(false);

        if (pauseMenuContainer != null)
            pauseMenuContainer.SetActive(false);

        Open();
    }

    private void Open()
    {
        RefreshValuesFromSystems();

        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (settingsRoot != null)
            settingsRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Close button. Returns to whichever menu opened Settings.
    /// </summary>
    public void CloseButton()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (settingsRoot != null)
            settingsRoot.SetActive(false);

        if (openOrigin == OpenOrigin.PauseMenu)
        {
            if (pauseMenuContainer != null)
                pauseMenuContainer.SetActive(true);

            // PauseMenu remains paused; do not change Time.timeScale.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (mainMenuContainer != null)
                mainMenuContainer.SetActive(true);

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Opens the separate Controls UI.
    /// </summary>
    public void ControlsButton()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    /// <summary>
    /// Assign this to a Back/Close button on the Controls UI if desired.
    /// </summary>
    public void ControlsBackButton()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    private void RefreshValuesFromSystems()
    {
        suppressSliderCallbacks = true;

        float sensitivity = PlayerPrefs.GetFloat(
            SensitivityKey,
            2f);

        PlayerLook playerLook =
            FindFirstObjectByType<PlayerLook>();

        if (playerLook != null)
            sensitivity = playerLook.GetSensitivity();

        float volume = PlayerPrefs.GetFloat(
            VolumeKey,
            1f);

        if (AudioManager.Instance != null)
            volume = AudioManager.Instance.GetMasterVolume();

        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.value =
                Mathf.Clamp(
                    sensitivity,
                    minimumSensitivity,
                    maximumSensitivity);
        }

        if (volumeSlider != null)
            volumeSlider.value = Mathf.Clamp01(volume);

        suppressSliderCallbacks = false;
    }

    private void OnSensitivityChanged(float value)
    {
        if (suppressSliderCallbacks)
            return;

        float sensitivity =
            Mathf.Clamp(
                value,
                minimumSensitivity,
                maximumSensitivity);

        PlayerPrefs.SetFloat(
            SensitivityKey,
            sensitivity);

        PlayerPrefs.Save();

        PlayerLook playerLook =
            FindFirstObjectByType<PlayerLook>();

        if (playerLook != null)
            playerLook.SetSensitivity(sensitivity);
    }

    private void OnVolumeChanged(float value)
    {
        if (suppressSliderCallbacks)
            return;

        float volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            VolumeKey,
            volume);

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }

    public bool IsOpen()
    {
        return settingsRoot != null && settingsRoot.activeSelf;
    }
}
