using UnityEngine;
using UnityEngine.UI;
using BrmnModules.UI;
using BrmnModules.Audio;
using TMPro;

public class SettingsPopup : PopupUI
{
    [Header("Sound Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI masterText;
    [SerializeField] private TextMeshProUGUI sfxText;
    [SerializeField] private TextMeshProUGUI bgmText;

    [Header("Display (EXE Only)")]
    [SerializeField] private GameObject displaySection;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button res1920Button;
    [SerializeField] private Button res1280Button;

    public override void Initialize()
    {
        base.Initialize();

        SetupSlider(masterSlider);
        SetupSlider(sfxSlider);
        SetupSlider(bgmSlider);

        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);

        bool isDesktop = Application.platform == RuntimePlatform.WindowsPlayer
            || Application.platform == RuntimePlatform.OSXPlayer
            || Application.platform == RuntimePlatform.LinuxPlayer;
 
        displaySection?.SetActive(isDesktop);
 
        if (isDesktop)
        {
            fullscreenToggle?.onValueChanged.AddListener(OnFullscreenChanged);
            res1920Button?.onClick.AddListener(() => SetResolution(1920, 1080));
            res1280Button?.onClick.AddListener(() => SetResolution(1280, 720));
        }
    }

    private void SetupSlider(Slider slider)
    {
        if (slider == null) return;
        slider.minValue = 0;
        slider.maxValue = 10;
        slider.wholeNumbers = true;
    }

    public override void Show()
    {
        masterSlider?.onValueChanged.RemoveListener(OnMasterChanged);
        sfxSlider?.onValueChanged.RemoveListener(OnSFXChanged);
        bgmSlider?.onValueChanged.RemoveListener(OnBGMChanged);

        if (masterSlider != null) masterSlider.value = AudioManager.Instance?.GetMasterVolume() ?? 5;
        if (sfxSlider != null) sfxSlider.value = AudioManager.Instance?.GetSFXVolume() ?? 5;
        if (bgmSlider != null) bgmSlider.value = AudioManager.Instance?.GetBGMVolume() ?? 5;
        RefreshVolumeTexts();

        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);

        RefreshDisplayUI();

        base.Show();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    private void OnMasterChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume((int)value);
        masterText.text = ((int)value).ToString();
    }

    private void OnSFXChanged(float value) 
    {
        AudioManager.Instance?.SetSFXVolume((int)value);
        sfxText.text = ((int)value).ToString();
    }

    private void OnBGMChanged(float value) 
    {
        AudioManager.Instance?.SetBGMVolume((int)value);
        bgmText.text = ((int)value).ToString();
    }

    private void RefreshVolumeTexts()
    {
        masterText.text = (AudioManager.Instance?.GetMasterVolume() ?? 5).ToString();
        sfxText.text = (AudioManager.Instance?.GetSFXVolume() ?? 5).ToString();
        bgmText.text = (AudioManager.Instance?.GetBGMVolume() ?? 5).ToString();
    }

    private void RefreshDisplayUI()
    {
        if (displaySection == null || !displaySection.activeSelf) return;
 
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    private void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Screen.fullScreen);
 
        PlayerPrefs.SetInt("ResWidth", width);
        PlayerPrefs.SetInt("ResHeight", height);
        PlayerPrefs.Save();
    }
 
    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
 
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}