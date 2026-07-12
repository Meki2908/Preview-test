using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "GameAudioMixerConfig", menuName = "Audio/Game Audio Mixer Config")]
public class GameAudioMixerConfig : ScriptableObject
{
    public const string ResourceName = "GameAudioMixerConfig";
    public const string MixerAssetPath = "Assets/Audios/Game audio.mixer";

    public AudioMixer mixer;

    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SFXVolume";
    public const string MusicGroupName = "Music";
    public const string SfxGroupName = "SFX";
}
