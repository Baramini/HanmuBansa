using UnityEngine;
using UnityEngine.UI;
using BrmnModules.UI;

public class SettingsPopup : PopupUI
{
    [Header("Sound Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    public override void Initialize()
    {
        base.Initialize();

        // -- Set default values --
        if (masterSlider != null) masterSlider.value = 0.5f;
        if (sfxSlider != null) sfxSlider.value = 0.5f;
        if (bgmSlider != null) bgmSlider.value = 0.5f;

        // -- Register slider callbacks --
        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);
    }

    public override void OnCloseButton()
    {
        SaveSettings();
        base.OnCloseButton();
    }

    private void OnMasterChanged(float value)
    {
        // -- TODO: AudioManager.Instance?.SetMasterVolume(value) --
    }

    private void OnSFXChanged(float value)
    {
        // -- TODO: AudioManager.Instance?.SetSFXVolume(value) --
    }

    private void OnBGMChanged(float value)
    {
        // -- TODO: AudioManager.Instance?.SetBGMVolume(value) --
    }

    private void SaveSettings()
    {
        // -- Save to PlayerPrefs for persistence --
        if (masterSlider != null) PlayerPrefs.SetFloat("MasterVolume", masterSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        if (bgmSlider != null) PlayerPrefs.SetFloat("BGMVolume", bgmSlider.value);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (bgmSlider != null) bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
    }

    public override void Show()
    {
        LoadSettings();  // -- Load saved values on open --
        base.Show();
    }
}