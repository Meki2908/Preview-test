using Unity.VisualScripting;
using UnityEngine;

public class Pistol : GunBase
{
    [Header("Reload Audio")]
    [SerializeField] private AudioClip audioReloadClip1;
    [SerializeField] private AudioClip audioReloadClip2;
    [SerializeField] private AudioClip audioReloadClip3;

    public void PlayAudioReload1() => shootAudio.PlayOneShot(audioReloadClip1);
    public void PlayAudioReload2() => shootAudio.PlayOneShot(audioReloadClip2);
    public void PlayAudioReload3() => shootAudio.PlayOneShot(audioReloadClip3);
}
