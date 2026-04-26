using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// SettingsPanelController: Manages the Settings sub-menu with three tabs:
/// • Audio - Master / Music / SFX volume sliders
/// • Graphics - Resolution dropdown, fullscreen toggle, quality dropdown, VSync toggle
/// • Keybinds - Rebindable action keys (uses Unity's legacy Input System as a base; swap out for the new Input System's InputAction.PerformInteractiveRebinding if you are using that package)
/// 
/// Attach to the Settings Panel GameObject.
/// Wire up all references in the Inspector.
/// </summary>
public class SettingsPanelController : MonoBehaviour
{
    // ── Tab Content GameObjects ───────────────────────────────────────────────
    [Header("Tab Content Panels")]
    public GameObject audioTabContent;
    public GameObject graphicsTabContent;
    public GameObject keybindsTabContent;

    // ── Tab Buttons (to visually highlight the active tab) ───────────────────
    [Header("Tab Buttons")]
    public Button audioTabButton;
    public Button graphicsTabButton;
    public Button keybindsTabButton;

    [Tooltip("Color used for the currently active tab button")]
    public Color activeTabColor = new Color(0.2f, 0.6f, 1f);
    [Tooltip("Color used for inactive tab buttons")]
    public Color inactiveTabColor = new Color(0.15f, 0.15f, 0.15f);

    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("Audio - AudioMixer")]
    [Tooltip("The AudioMixer that exposes 'MasterVolume', 'MusicVolume', SFXVolume' parameters")]
    public AudioMixer audioMixer;

    [Header("Audio - Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Audio - Value Labels (optional)")]
    public TextMeshProUGUI masterVolumeLabel;
    public TextMeshProUGUI musicVolumeLabel;
    public TextMeshProUGUI sfxVolumeLabel;

    // ── Graphics ──────────────────────────────────────────────────────────────
    [Header("Graphics")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;

    // ── Keybinds ──────────────────────────────────────────────────────────────
    [Header("Keybind Rows")]
    [Tooltip("Each entry maps one action name to its KeybindRow UI component")]
    public KeybindRow[] keybindRows;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Resolution[] _resolutions;

    // ─────────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Always start on the Audio tab
        ShowAudioTab();
        LoadAudioSettings();
        LoadGraphicsSettings();
        LoadKeybindSettings();
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Tab Navigation

    public void ShowAudioTab()
    {
        SetTabActive(audioTabContent, audioTabButton, true);
        SetTabActive(graphicsTabContent, graphicsTabButton, false);
        SetTabActive(keybindsTabContent, keybindsTabButton, false);
    }

    public void ShowGraphicsTab()
    {
        SetTabActive(audioTabContent, audioTabButton, false);
        SetTabActive(graphicsTabContent, graphicsTabButton, true);
        SetTabActive(keybindsTabContent, keybindsTabButton, false);
    }

    public void ShowKeybindsTab()
    {
        SetTabActive(audioTabContent, audioTabButton, false);
        SetTabActive(graphicsTabContent, graphicsTabButton, false);
        SetTabActive(keybindsTabContent, keybindsTabButton, true);
    }

    private void SetTabActive(GameObject content, Button tabBtn, bool active)
    {
        if (content != null) content.SetActive(active);
        if (tabBtn != null)
        {
            ColorBlock cb = tabBtn.colors;
            cb.normalColor = active ? activeTabColor : inactiveTabColor;
            cb.highlightedColor = active ? activeTabColor : inactiveTabColor * 1.2f;
            tabBtn.colors = cb;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Audio

    private void LoadAudioSettings()
    {
        float master = PlayerPrefs.GetFloat("Vol_Master", 0.75f);
        float music = PlayerPrefs.GetFloat("Vol_Music", 0.75f);
        float sfx = PlayerPrefs.GetFloat("Vol_SFX", 0.75f);

        SetSlider(masterVolumeSlider, master, OnMasterVolumeChanged);
        SetSlider(musicVolumeSlider, music, OnMusicVolumeChanged);
        SetSlider(sfxVolumeSlider, sfx, OnSFXVolumeChanged);

        ApplyMixerVolume("MasterVolume", master);
        ApplyMixerVolume("MusicVolume", music);
        ApplyMixerVolume("SFXVolume", sfx);

        UpdateVolumeLabel(masterVolumeLabel, master);
        UpdateVolumeLabel(musicVolumeLabel, music);
        UpdateVolumeLabel(sfxVolumeLabel, sfx);
    }

    public void OnMasterVolumeChanged(float value)
    {
        ApplyMixerVolume("MasterVolume", value);
        PlayerPrefs.SetFloat("Vol_Master", value);
        UpdateVolumeLabel(masterVolumeLabel, value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        ApplyMixerVolume("MusicVolume", value);
        PlayerPrefs.SetFloat("Vol_Music", value);
        UpdateVolumeLabel(musicVolumeLabel, value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        ApplyMixerVolume("SFXVolume", value);
        PlayerPrefs.SetFloat("Vol_SFX", value);
        UpdateVolumeLabel(sfxVolumeLabel, value);
    }

    // Converts 0-1 slider to decibels and sets the AudioMixer parameter
    private void ApplyMixerVolume(string paramName, float linearValue)
    {
        if (audioMixer == null) return;
        // Avoid log(0); clamp to a small positive value.
        float db = Mathf.Approximately(linearValue, 0f)
            ? -80f
            : Mathf.Log10(Mathf.Max(linearValue, 0.0001f)) * 20f;
        audioMixer.SetFloat(paramName, db);
    }

    private static void SetSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float>callback)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.value = value;
        slider.onValueChanged.AddListener(callback);
    }

    private static void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Graphics

    private void LoadGraphicsSettings()
    {
        // ── Resolution ────────────────────────────────────────────────────────
        if (resolutionDropdown != null)
        {
            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<string>();
            int currentIndex = 0;

            for (int i = 0; i < _resolutions.Length; i++)
            {
                options.Add($"{_resolutions[i].width} × {_resolutions[i].height} @ {_resolutions[i].refreshRate}Hz");

                if (_resolutions[i].width == Screen.currentResolution.width && _resolutions[i].height == Screen.currentResolution.height) currentIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = PlayerPrefs.GetInt("Gfx_Resolution", currentIndex);
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChagned);
        }

        // ── Fullscreen ────────────────────────────────────────────────────────
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Gfx_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // ── Quality ───────────────────────────────────────────────────────────
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.value = PlayerPrefs.GetInt("Gfx_Quality", QualitySettings.GetQualityLevel());
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        // ── VSync ─────────────────────────────────────────────────────────────
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = PlayerPrefs.GetInt("Gfx_VSync", QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }
    }

    public void OnResolutionChagned(int index)
    {
        if (_resolutions == null || index >= _resolutions.Length) return;
        Resolution r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen, r.refreshRate);
        PlayerPrefs.SetInt("Gfx_Resolution", index);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Gfx_Fullscreen", isFullscreen ? 1 : 0);
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, true);
        PlayerPrefs.SetInt("Gfx_Quality", index);
    }

    public void OnVSyncChanged(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1: 0;
        PlayerPrefs.SetInt("Gfx_VSync", enabled ? 1 : 0);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Keybinds

    private void LoadKeybindSettings()
    {
        if (keybindRows == null) return;
        foreach (var row in keybindRows)
            row?.LoadBinding();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    
    /// <summary>Called by an "Apply" or "Save" button in the Settings panel.</summary>
    public void OnApplySettings()
    {
        PlayerPrefs.Save();
        Debug.Log("[Settings] All settings saved");
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// KeybindRow: Attach to each action row in the Keybinds tab.
/// The row consists of:
/// • An actual label (TextMeshProUGUI showing e.g. "Jumop")
/// • A key label (TextMeshProUGUI showing the current key)
/// • A "Rebind" Button
/// 
/// Uses legacy Input system key names stored in PlayerPrefs.
/// If you switch to new Input System, replace the body with InputAction.PerformInteractiveRebinding().
/// </summary>
[System.Serializable]
public class KeybindRow : MonoBehaviour
{
    [Tooltip("Internal action name used as PlayerPrefs key, e.g. 'Jump'")]
    public string actionName;

    [Tooltip("Default KeyCode name e.g. 'Space'")]
    public string defaultKey = "None";

    [Tooltip("Label displaing the action name")]
    public TextMeshProUGUI actionLabel;

    [Tooltip("Label displaying the currently bound key")]
    public TextMeshProUGUI keyLabel;

    [Tooltip("Button that starts the rebind process")]
    public Button rebindButton;

    private bool _isListening;
    
    private void Awake()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartListening);

        if (actionLabel != null)
            actionLabel.text = actionName;
    }

    public void LoadBinding()
    {
        string saved = PlayerPrefs.GetString("Key_" + actionName, defaultKey);
        if (keyLabel != null) keyLabel.text = saved;
    }

    private void StartListening()
    {
        _isListening = true;
        if (keyLabel != null) keyLabel.text = "Press any key...";
        if (rebindButton != null) rebindButton.interactable = false;
    }

    private void OnGUI()
    {
        if (!_isListening) return;

        Event e = Event.current;
        if (e.isKey && e.keyCode != KeyCode.None && e.type == EventType.KeyDown)
        {
            string keyName = e.keyCode.ToString();
            PlayerPrefs.SetString("Key_" + actionName, keyName);
            if (keyLabel != null) keyLabel.text = keyName;
            if (rebindButton != null) rebindButton.interactable = true;
            _isListening = false;
        }
    }
}