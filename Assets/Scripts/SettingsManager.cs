using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider SFXSlider;

    public void ApplyMusicVolume()
    {
        float MusicVolume = musicSlider.value;
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(MusicVolume) * 20); // Chuyển sang dB
    }
    public void ApplySFXVolume()
    {
        float SFXVolume = SFXSlider.value;
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(SFXVolume) * 20);
    }
    public void Back() => gameObject.SetActive(false);
}
