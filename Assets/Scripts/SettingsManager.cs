using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        AudioVolumeService.EnsureInitialized();
        GameAudioMixerService.EnsureInitialized();
        InitializeSlider(musicSlider, AudioVolumeService.MusicVolume);
        InitializeSlider(sfxSlider, AudioVolumeService.SfxVolume);
        ApplySavedVolumes();
    }

    public void Configure(Slider music, Slider sfx)
    {
        musicSlider = music;
        sfxSlider = sfx;

        AudioVolumeService.EnsureInitialized();
        GameAudioMixerService.EnsureInitialized();
        InitializeSlider(musicSlider, AudioVolumeService.MusicVolume);
        InitializeSlider(sfxSlider, AudioVolumeService.SfxVolume);
        ApplySavedVolumes();
    }

    public void ApplyMusicVolume()
    {
        if (musicSlider == null)
            return;

        AudioVolumeService.SetMusicVolume(Mathf.Clamp01(musicSlider.value));
    }

    public void ApplySFXVolume()
    {
        if (sfxSlider == null)
            return;

        AudioVolumeService.SetSfxVolume(Mathf.Clamp01(sfxSlider.value));
    }

    public void Back()
    {
        AudioVolumeService.Save();

        if (MenuSettingsOverlay.Instance != null && MenuSettingsOverlay.Instance.IsOpen)
        {
            MenuSettingsOverlay.Instance.Close();
            return;
        }

        if (IngameUI.Instance != null)
            IngameUI.Instance.CloseSettings();
        else
            gameObject.SetActive(false);
    }

    private void ApplySavedVolumes()
    {
        GameAudioMixerService.EnsureInitialized();
        if (GameAudioMixerService.Instance != null)
            GameAudioMixerService.Instance.ApplySavedVolumes();
    }

    private static void InitializeSlider(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
    }
}
