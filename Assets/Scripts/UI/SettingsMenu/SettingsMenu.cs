using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    private enum OpenOrigin
    {
        MainMenu,
        PauseMenu
    }

    [Header("Settings UI")]

    [Tooltip("The Settings GameObject itself.")]
    [SerializeField] private GameObject settingsRoot;

    [Header("Sliders")]

    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private Slider volumeSlider;

    [Header("Slider Ranges")]

    [SerializeField] private float minimumSensitivity = 0.1f;
    [SerializeField] private float maximumSensitivity = 5f;

    [Header("Optional Return Containers")]

    [Tooltip("Main menu panel/container that should be shown when Settings is closed.")]
    [SerializeField] private GameObject mainMenuContainer;

    [Tooltip("Pause menu panel/container that should be shown when Settings is closed.")]
    [SerializeField] private GameObject pauseMenuContainer;

    [Header("Optional Controls Panel")]

    [Tooltip("Optional controls panel. Leave empty if you do not currently have one.")]
    [SerializeField] private GameObject controlsPanel;

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

        // Make sure the sliders are configured correctly.
        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.minValue = minimumSensitivity;
            cameraSensitivitySlider.maxValue = maximumSensitivity;

            cameraSensitivitySlider.onValueChanged.AddListener(
                OnSensitivityChanged
            );
        }

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;

            volumeSlider.onValueChanged.AddListener(
                OnVolumeChanged
            );
        }

        // Settings starts closed.
        if (settingsRoot != null)
        {
            settingsRoot.SetActive(false);
        }

        // Controls panel should also start hidden if one exists.
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        RefreshValuesFromSystems();
    }

    private void OnDestroy()
    {
        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.onValueChanged.RemoveListener(
                OnSensitivityChanged
            );
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(
                OnVolumeChanged
            );
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================
    // OPEN SETTINGS
    // =========================================================

    public void OpenFromMainMenu()
    {
        openOrigin = OpenOrigin.MainMenu;

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }

        if (pauseMenuContainer != null)
        {
            pauseMenuContainer.SetActive(false);
        }

        Open();
    }

    public void OpenFromPauseMenu()
    {
        openOrigin = OpenOrigin.PauseMenu;

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(false);
        }

        if (pauseMenuContainer != null)
        {
            pauseMenuContainer.SetActive(false);
        }

        Open();
    }

    private void Open()
    {
        RefreshValuesFromSystems();

        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        if (settingsRoot != null)
        {
            settingsRoot.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================================================
    // CLOSE SETTINGS
    // =========================================================
    public void CloseButton()
    {
        if (controlsPanel != null)
        {

            controlsPanel.SetActive(false);
        }

        if (settingsRoot != null)
        {
            settingsRoot.SetActive(false);
        }

        if (openOrigin == OpenOrigin.PauseMenu)
        {
            // Return to the Pause Menu.
            if (pauseMenuContainer != null)
            {
                pauseMenuContainer.SetActive(true);
            }

            // Keep the game paused.
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {

            if (mainMenuContainer != null)
            {
                mainMenuContainer.SetActive(true);
            }

            if (pauseMenuContainer != null)
            {
                pauseMenuContainer.SetActive(true);
            }

            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }


    // =========================================================
    // CONTROLS
    // =========================================================

    public void ControlsButton()
    {
        if (controlsPanel == null)
        {
            Debug.LogWarning(
                "SettingsMenu: View Controls was pressed, but no Controls Panel is assigned."
            );

            return;
        }

        settingsRoot.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void ControlsBackButton()
    {
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        if (settingsRoot != null)
        {
            settingsRoot.SetActive(true);
        }
    }

    // =========================================================
    // LOAD SETTINGS
    // =========================================================

    private void RefreshValuesFromSystems()
    {
        suppressSliderCallbacks = true;

        // Camera sensitivity
        float sensitivity = PlayerPrefs.GetFloat(
            SensitivityKey,
            2f
        );

        PlayerLook playerLook =
            FindFirstObjectByType<PlayerLook>();

        if (playerLook != null)
        {
            sensitivity = playerLook.GetSensitivity();
        }

        // Master volume
        float volume = PlayerPrefs.GetFloat(
            VolumeKey,
            1f
        );

        if (AudioManager.Instance != null)
        {
            volume = AudioManager.Instance.GetMasterVolume();
        }

        // Apply sensitivity slider
        if (cameraSensitivitySlider != null)
        {
            cameraSensitivitySlider.value =
                Mathf.Clamp(
                    sensitivity,
                    minimumSensitivity,
                    maximumSensitivity
                );
        }

        // Apply volume slider
        if (volumeSlider != null)
        {
            volumeSlider.value =
                Mathf.Clamp01(volume);
        }

        suppressSliderCallbacks = false;
    }

    // =========================================================
    // CAMERA SENSITIVITY
    // =========================================================

    private void OnSensitivityChanged(float value)
    {
        if (suppressSliderCallbacks)
        {
            return;
        }

        float sensitivity =
            Mathf.Clamp(
                value,
                minimumSensitivity,
                maximumSensitivity
            );

        PlayerPrefs.SetFloat(
            SensitivityKey,
            sensitivity
        );

        PlayerPrefs.Save();

        PlayerLook playerLook =
            FindFirstObjectByType<PlayerLook>();

        if (playerLook != null)
        {
            playerLook.SetSensitivity(sensitivity);
        }
    }

    // =========================================================
    // MASTER VOLUME
    // =========================================================

    private void OnVolumeChanged(float value)
    {
        if (suppressSliderCallbacks)
        {
            return;
        }

        float volume =
            Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(
            VolumeKey,
            volume
        );

        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }

    // =========================================================
    // STATUS
    // =========================================================

    public bool IsOpen()
    {
        return settingsRoot != null &&
               settingsRoot.activeSelf;
    }
}