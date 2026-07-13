using UnityEngine;

public class Shotgun : GunBase
{
    [Header("Shotgun Reload Sound")]
    [SerializeField] private AudioClip shotgunReloadSound1;
    [SerializeField] private AudioClip shotgunReloadSound2;
    [SerializeField] private AudioClip shotgunReloadSound3;
    [SerializeField] private AudioClip cockSound;

    [Header("Shotgun Shooting Settings")]
    public int pelletCount = 6;
    public float spreadAngle = 10f;

    public override bool TryShoot()
    {
        Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;
        return TryShoot(direction);
    }

    public override bool TryShoot(Vector3 direction)
    {
        if (gunData == null)
            return false;

        if (isReloading || Time.time < nextFireTime)
            return false;

        if (currentAmmo <= 0)
        {
            if (!isReloading)
                Reload();
            return false;
        }

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        direction.Normalize();
        direction = CalculateAimAssistDirection(direction);

        Quaternion aimRotation = Quaternion.LookRotation(direction, Vector3.up);

        for (int i = 0; i < pelletCount; i++)
        {
            Quaternion spread = aimRotation * Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0f);
            Vector3 pelletDirection = spread * Vector3.forward;

            if (gunData.isHitscan)
            {
                FireRaycast(pelletDirection);
            }
            else if (gunData.bulletPrefab != null && firePoint != null)
            {
                GameObject pellet = Instantiate(gunData.bulletPrefab, firePoint.position, spread);
                ConfigureSpawnedBullet(pellet, pelletDirection);
            }
        }

        nextFireTime = Time.time + gunData.fireRate;

        if (driveAnimatorInternally && animator != null)
            animator.Play("Fire", 0, 0f);

        shootAudio?.PlayOneShot(gunData.shootSound);
        SpawnMuzzleFlash();

        currentAmmo--;
        if (currentAmmo <= 0)
            Reload();

        return true;
    }

    public void PlayReloadSound1() => shootAudio.PlayOneShot(shotgunReloadSound1);
    public void PlayReloadSound2() => shootAudio.PlayOneShot(shotgunReloadSound2);
    public void PlayReloadSound3() => shootAudio.PlayOneShot(shotgunReloadSound3);
    public void PlayCockSound() => shootAudio.PlayOneShot(cockSound);
}
