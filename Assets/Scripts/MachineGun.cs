using UnityEngine;

public class MachineGun : GunBase
{
    [Header("Reload Sound")]
    [SerializeField] public AudioClip reloadSound1;
    [SerializeField] public AudioClip reloadSound2;
    [SerializeField] public AudioClip reloadSound3;
    [SerializeField] public AudioClip reloadSound4;
    [SerializeField] public AudioClip cockSound;

    public void PlayReloadSound1() => shootAudio.PlayOneShot(reloadSound1);
    public void PlayReloadSound2() => shootAudio.PlayOneShot(reloadSound2);
    public void PlayReloadSound3() => shootAudio.PlayOneShot(reloadSound3);
    public void PlayReloadSound4() => shootAudio.PlayOneShot(reloadSound4);
    public void PlayCockSound() => shootAudio.PlayOneShot(cockSound);
}
