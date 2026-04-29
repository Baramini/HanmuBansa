using UnityEngine;
using UnityEngine.UI;
using BrmnModules.UI;
using BrmnModules.Audio;

public class SettingsPopup : PopupUI
{
    [Header("Sound Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    public override void Initialize()
    {
        base.Initialize();

        SetupSlider(masterSlider);
        SetupSlider(sfxSlider);
        SetupSlider(bgmSlider);

        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);
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

        if (masterSlider != null)
            masterSlider.value = AudioManager.Instance?.GetMasterVolume() ?? 5;
        if (sfxSlider != null)
            sfxSlider.value = AudioManager.Instance?.GetSFXVolume() ?? 5;
        if (bgmSlider != null)
            bgmSlider.value = AudioManager.Instance?.GetBGMVolume() ?? 5;

        masterSlider?.onValueChanged.AddListener(OnMasterChanged);
        sfxSlider?.onValueChanged.AddListener(OnSFXChanged);
        bgmSlider?.onValueChanged.AddListener(OnBGMChanged);

        base.Show();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    private void OnMasterChanged(float value)
        => AudioManager.Instance?.SetMasterVolume((int)value);

    private void OnSFXChanged(float value)
        => AudioManager.Instance?.SetSFXVolume((int)value);

    private void OnBGMChanged(float value)
        => AudioManager.Instance?.SetBGMVolume((int)value);
}