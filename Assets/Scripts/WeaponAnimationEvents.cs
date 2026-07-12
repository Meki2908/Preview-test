using System.Collections;
using UnityEngine;

/// <summary>
/// Nhận Animation Events từ animator trên model súng FPS (Select, Reload, Fire...).
/// Tự gắn lên child Animator qua GunBase.EnsureAnimationEventReceivers().
/// </summary>
[DisallowMultipleComponent]
public class WeaponAnimationEvents : MonoBehaviour
{
    [SerializeField] private AudioClip retrieveSound;

    private GunBase gun;
    private Animator localAnimator;

    private void Awake()
    {
        localAnimator = GetComponent<Animator>();
        gun = GetComponentInParent<GunBase>();
    }

    #region Weapon swap

    public void PlayRetrieveSound()
    {
        if (retrieveSound != null && gun != null && gun.shootAudio != null)
            gun.shootAudio.PlayOneShot(retrieveSound);
    }

    public void DisableSelectAnim()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("Select", false);
        localAnimator.SetBool("PutAway", false);
        localAnimator.SetBool("Idle", true);
    }

    public void PlayPutawaySound() { }

    public void Deactivation() { }

    public void PlaySwitchSound() { }

    #endregion

    #region Fire

    public void PlayFireSound()
    {
        if (gun == null || gun.shootAudio == null || gun.gunData == null || gun.gunData.shootSound == null)
            return;

        gun.shootAudio.PlayOneShot(gun.gunData.shootSound);
    }

    public IEnumerator AddSingleFireEffects()
    {
        gun?.PlayMuzzleFlashFromAnimation();
        yield return new WaitForSeconds(0.05f);
    }

    public void SingleFireAmmoCounter() { }

    public void AutoFireAmmoCounter() { }

    public void AltFireToIdle() => ResetFireBools();

    public void AltZoomFireToIdle() => ResetFireBools();

    public void DryFireToIdle() => ResetFireBools();

    private void ResetFireBools()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("Fire", false);
        localAnimator.SetBool("AltFire", false);
    }

    #endregion

    #region Reload

    public void AddAmmo()
    {
        gun?.CompleteReloadFromAnimation();
    }

    public void ReloadToIdle()
    {
        if (gun != null)
        {
            gun.NotifyReloadAnimatorReturnedToIdle();
            return;
        }

        if (localAnimator == null)
            return;

        localAnimator.SetBool("Reload", false);
        localAnimator.SetBool("EmptyReload", false);
        localAnimator.SetBool("ReloadLoop", false);
        localAnimator.SetBool("EndReload", false);
        localAnimator.SetBool("Idle", true);
    }

    public void PlayReloadPart1Sound() => PlayMachineGunReloadSound(1);

    public void PlayReloadPart2Sound() => PlayMachineGunReloadSound(2);

    public void PlayReloadPart3Sound() => PlayMachineGunReloadSound(3);

    public void PlayReloadPart4Sound() => PlayMachineGunReloadSound(4);

    public void PlayReloadPart5Sound() { }

    public void PlayReloadPart6Sound() { }

    public void PlayReloadPart7Sound() { }

    public void PlayReloadPart8Sound() { }

    public void PlayReloadSound1() => PlayMachineGunReloadSound(1);

    public void PlayReloadSound2() => PlayMachineGunReloadSound(2);

    public void PlayReloadSound3() => PlayMachineGunReloadSound(3);

    public void PlayReloadSound4() => PlayMachineGunReloadSound(4);

    public void PlayCockSound()
    {
        MachineGun machineGun = GetComponentInParent<MachineGun>();
        machineGun?.PlayCockSound();
    }

    public void PlayAudioReload1() => GetComponentInParent<Pistol>()?.PlayAudioReload1();

    public void PlayAudioReload2() => GetComponentInParent<Pistol>()?.PlayAudioReload2();

    public void PlayAudioReload3() => GetComponentInParent<Pistol>()?.PlayAudioReload3();

    private void PlayMachineGunReloadSound(int index)
    {
        MachineGun machineGun = GetComponentInParent<MachineGun>();
        if (machineGun == null)
            return;

        switch (index)
        {
            case 1: machineGun.PlayReloadSound1(); break;
            case 2: machineGun.PlayReloadSound2(); break;
            case 3: machineGun.PlayReloadSound3(); break;
            case 4: machineGun.PlayReloadSound4(); break;
        }
    }

    public void PlayReloadLoop() { }

    public void EndReloadToIdle() => ReloadToIdle();

    #endregion

    #region Grenade / melee / locomotion

    public void GrenadeThrowToIdle()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("GrenadeThrow", false);
        localAnimator.SetBool("Idle", true);
    }

    public void PlayRemovingSafetyPin() { }

    public void ThrowGrenade() { }

    public void PlayThrow() { }

    public void MeleeAttackToIdle()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("MeleeAttack", false);
        localAnimator.SetBool("Idle", true);
    }

    public void PlayMeleeSound() { }

    public void CrouchToIdle()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("Crouch", false);
        localAnimator.SetBool("Idle", true);
    }

    public void JumpToIdle()
    {
        if (localAnimator == null)
            return;

        localAnimator.SetBool("Jump", false);
        localAnimator.SetBool("Idle", true);
    }

    public void PlayFootsteps()
    {
        FootstepController footsteps = GetComponentInParent<FootstepController>();
        if (footsteps != null)
            footsteps.PlayStepFromAnimationEvent();
    }

    public void PlayJumpSound() { }

    public void PlayCrouchSound() { }

    #endregion

    #region Misc FPS weapon events

    public void PlayBoltPart1Sound() { }

    public void PlayBoltPart2Sound() { }

    public void PlayFlapPart1Sound() { }

    public void PlayFlapPart2Sound() { }

    public void PlaySlideSound() { }

    public void ShowCollimator() { }

    public void HideCollimator() { }

    public void AltToChangeLayerWeight() { }

    public void AltFromChangeLayerWeight() { }

    public void HitATarget() { }

    #endregion
}
