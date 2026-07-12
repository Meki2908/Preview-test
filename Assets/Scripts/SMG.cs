using UnityEngine;

public class SMG : GunBase
{
    [Header("Reload sounds")]
    [SerializeField] public AudioClip reloadSound1;
    [SerializeField] public AudioClip reloadSound2;
    [SerializeField] public AudioClip reloadSound3;
    [SerializeField] public AudioClip cockSound;

    public void PlayReloadSound1() => shootAudio.PlayOneShot(reloadSound1);
    public void PlayReloadSound2() => shootAudio.PlayOneShot(reloadSound2);
    public void PlayReloadSound3() => shootAudio.PlayOneShot(reloadSound3);
    public void PlayCockSound() => shootAudio.PlayOneShot(cockSound);
}
