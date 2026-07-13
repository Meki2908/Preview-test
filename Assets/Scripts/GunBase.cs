using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FireMode { Projectile, Raycast }
public class GunBase : MonoBehaviour
{
    [Header("Gun main parameters")]
    [SerializeField] public AudioSource shootAudio;

    [SerializeField] public Animator animator;
    [SerializeField] public InputActionReference reloadAction;
    [SerializeField] public Transform muzzlePoint;
    [SerializeField] private Transform aimingCamera;
    [SerializeField] private float hitscanRange = 100f;
    [SerializeField] private LayerMask hitscanMask = ~0;

    [Header("Smart Aim (top-down slope assist)")]
    [SerializeField] private bool aimAssistEnabled = true;
    [SerializeField] private float aimAssistRadius = 2.5f;
    [SerializeField] private bool requireLineOfSight = true;

    private static readonly RaycastHit[] AimAssistHitBuffer = new RaycastHit[32];

    [Header("Gun data")]
    public GunData gunData;
    public Transform firePoint;
    public int CurrentAmmo => currentAmmo;
    public int MagSize => currentMagsize;
    public bool HasInfiniteReserve => gunData != null && gunData.infiniteReserve;

    public string AmmoDisplayText
    {
        get
        {
            if (HasInfiniteReserve)
                return $"{currentAmmo} / ∞";

            return $"{currentAmmo} / {currentMagsize}";
        }
    }

    protected int currentMagsize;
    protected int currentAmmo;
    protected float nextFireTime;
    protected bool isReloading;
    protected bool driveAnimatorInternally = true;
    private Animator weaponAnimator;
    private Coroutine reloadAnimCoroutine;
    private Coroutine reloadAnimatorFallbackCoroutine;
    private bool reloadAnimatorResetReceived;

    public bool IsReloading => isReloading;

    protected virtual void Awake()
    {
        EnsureAnimationEventReceivers();

        if (weaponAnimator == null && animator != null)
            weaponAnimator = animator;
    }

    public void EnsureAnimationEventReceivers()
    {
        foreach (Animator childAnimator in GetComponentsInChildren<Animator>(true))
        {
            if (childAnimator.GetComponent<WeaponAnimationEvents>() == null)
                childAnimator.gameObject.AddComponent<WeaponAnimationEvents>();
        }
    }

    public void ConfigureForSoldier(Animator bodyAnimator)
    {
        if (weaponAnimator == null)
        {
            if (animator != null && animator != bodyAnimator)
                weaponAnimator = animator;
            else
                weaponAnimator = GetComponentInChildren<Animator>();
        }

        animator = bodyAnimator;
        driveAnimatorInternally = false;
        EnsureAnimationEventReceivers();
        EnsureWeaponAnimatorIdle();
    }

    public void EnsureWeaponAnimatorIdle()
    {
        ResetWeaponAnimatorToIdle();
    }

    /// <summary>Gọi từ Animation Event ReloadToIdle / EndReloadToIdle trên model súng.</summary>
    public void NotifyReloadAnimatorReturnedToIdle()
    {
        reloadAnimatorResetReceived = true;
        CancelReloadAnimatorFallback();
        ResetWeaponAnimatorToIdle();
    }

    protected virtual void Start()
    {
        if (gunData == null)
            return;

        currentAmmo = gunData.maxAmmo;
        currentMagsize = gunData.magSize;
    }
    protected virtual void Update()
    {
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame() && !isReloading && currentAmmo < gunData.maxAmmo)
        {
            Reload();
        }
        if (Time.timeScale == 0 && shootAudio != null)
            shootAudio.Pause();
    }

    public virtual bool TryShoot()
    {
        Vector3 direction = firePoint != null ? firePoint.forward : transform.forward;
        return TryShoot(direction);
    }

    public virtual bool TryShoot(Vector3 direction)
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

        if (gunData.isHitscan)
            FireRaycast(direction);
        else
            SpawnBullet(direction);

        nextFireTime = Time.time + gunData.fireRate;

        if (driveAnimatorInternally && animator != null)
            animator.Play("Fire", 0, 0f);

        if (shootAudio != null && gunData.shootSound != null)
            shootAudio.PlayOneShot(gunData.shootSound);

        SpawnMuzzleFlash();

        currentAmmo--;
        if (currentAmmo <= 0)
            Reload();

        return true;
    }

    public virtual void Shoot()
    {
        TryShoot();
    }

    protected void SpawnBullet(Vector3 direction)
    {
        if (gunData == null || gunData.bulletPrefab == null || firePoint == null)
            return;

        GameObject bullet = Instantiate(
            gunData.bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(direction, Vector3.up));
        ConfigureSpawnedBullet(bullet, direction);
    }

    protected Vector3 CalculateAimAssistDirection(Vector3 flatDirection)
    {
        if (!aimAssistEnabled || flatDirection.sqrMagnitude < 0.001f)
            return flatDirection;

        Transform originTransform = firePoint != null ? firePoint : transform;
        Vector3 origin = originTransform.position;
        Vector3 scanDirection = flatDirection.normalized;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            aimAssistRadius,
            scanDirection,
            AimAssistHitBuffer,
            hitscanRange,
            hitscanMask,
            QueryTriggerInteraction.Collide);

        Collider bestCollider = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = AimAssistHitBuffer[i].collider;
            if (col == null || !IsValidAimAssistTarget(col))
                continue;

            float distance = AimAssistHitBuffer[i].distance;
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            bestCollider = col;
        }

        if (bestCollider == null)
            return scanDirection;

        Vector3 targetCenter = GetAimAssistPoint(bestCollider);
        if (requireLineOfSight && !HasClearLineOfSight(origin, targetCenter, bestCollider))
            return scanDirection;

        Vector3 assistedDirection = targetCenter - origin;
        return assistedDirection.sqrMagnitude > 0.001f
            ? assistedDirection.normalized
            : scanDirection;
    }

    private static bool IsValidAimAssistTarget(Collider col)
    {
        if (col.GetComponent<Hitbox>() != null)
            return !IsDeadCollider(col);

        return col.GetComponentInParent<EnemyBase>() != null && !IsDeadCollider(col);
    }

    private static bool IsDeadCollider(Collider col)
    {
        Health health = col.GetComponentInParent<Health>();
        return health != null && health.IsDead;
    }

    private static Vector3 GetAimAssistPoint(Collider col)
    {
        EnemyBase enemy = col.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            Collider bodyCollider = enemy.GetComponentInChildren<Collider>();
            if (bodyCollider != null)
                return bodyCollider.bounds.center;
        }

        return col.bounds.center;
    }

    private bool HasClearLineOfSight(Vector3 origin, Vector3 targetCenter, Collider targetCollider)
    {
        Vector3 toTarget = targetCenter - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.01f)
            return true;

        if (!Physics.Raycast(origin, toTarget / distance, out RaycastHit block, distance, hitscanMask, QueryTriggerInteraction.Collide))
            return true;

        Transform targetRoot = targetCollider.transform.root;
        return block.collider.transform.root == targetRoot
            || block.collider.transform.IsChildOf(targetRoot);
    }

    protected void FireRaycast(Vector3 direction)
    {
        Transform originTransform = firePoint != null ? firePoint : transform;
        Vector3 origin = originTransform.position;

        Debug.DrawRay(origin, direction * hitscanRange, Color.red, 0.5f);

        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, hitscanRange, hitscanMask, QueryTriggerInteraction.Collide);
        Vector3 end = didHit ? hit.point : origin + direction * hitscanRange;
        SpawnBulletTracer(origin, end);

        if (!didHit)
            return;

        Collider hitCollider = hit.collider;
        Hitbox hitbox = hitCollider.GetComponent<Hitbox>();
        Health health = hitCollider.GetComponentInParent<Health>();

        if (hitbox != null)
        {
            hitbox.TakeHit(gunData.damage);
            float displayedDamage = gunData.damage * hitbox.damageMultiplier;
            SpawnBloodFX(hit.point, hit.normal);
            DamageDisplay.current?.CreatePopUp(hit.point, displayedDamage.ToString("0"), Color.red, 1.5f, 1.5f);
            return;
        }

        if (health != null && hitCollider.GetComponentInParent<EnemyBase>() != null)
        {
            health.TakeDamage(gunData.damage, origin);
            SpawnBloodFX(hit.point, hit.normal);
            DamageDisplay.current?.CreatePopUp(hit.point, gunData.damage.ToString(), Color.yellow);
        }
    }

    private void SpawnBloodFX(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (gunData == null || gunData.bloodFX == null)
            return;

        Quaternion rotation = hitNormal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(hitNormal)
            : Quaternion.identity;
        GameObject fx = Instantiate(gunData.bloodFX, hitPoint, rotation);
        Destroy(fx, 3f);
    }

    protected void ConfigureSpawnedBullet(GameObject bullet, Vector3 direction)
    {
        if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
            bulletScript.Initialize(gunData);

        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.linearVelocity = direction.normalized * Mathf.Abs(gunData.bulletSpeed);
    }

    protected void SpawnMuzzleFlash()
    {
        if (gunData.muzzleFlashPrefab == null || muzzlePoint == null)
            return;

        GameObject muzzle = Instantiate(gunData.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
        Destroy(muzzle, 2f);
    }

    protected void SpawnBulletTracer(Vector3 start, Vector3 end)
    {
        if (gunData == null || gunData.bulletTracerPrefab == null)
            return;

        BulletTracer.Spawn(gunData.bulletTracerPrefab, start, end);
    }

    public void Reload()
    {
        if (gunData == null)
            return;

        if (isReloading || currentAmmo >= gunData.maxAmmo)
            return;

        if (!HasInfiniteReserve && currentMagsize <= 0)
            return;

        isReloading = true;
        reloadAnimatorResetReceived = false;
        CancelReloadAnimatorFallback();
        PlayReloadAnimation();
        reloadAnimatorFallbackCoroutine = StartCoroutine(ReloadAnimatorFallbackRoutine());

        CancelInvoke(nameof(FinishReload));
        Invoke(nameof(FinishReload), gunData.reloadTime);
    }

    private void PlayReloadAnimation()
    {
        if (weaponAnimator != null)
        {
            if (reloadAnimCoroutine != null)
                StopCoroutine(reloadAnimCoroutine);

            reloadAnimCoroutine = StartCoroutine(PlayReloadAnimationRoutine());
            return;
        }

        if (driveAnimatorInternally && animator != null)
            animator.Play("Reload", 0, 0f);
    }

    private IEnumerator PlayReloadAnimationRoutine()
    {
        bool useEmptyReload = currentAmmo <= 0;

        SetWeaponReloadBools(false, false);
        weaponAnimator.SetBool("Fire", false);
        weaponAnimator.SetBool("AltFire", false);
        weaponAnimator.SetBool("Idle", false);

        yield return null;

        SetWeaponReloadBools(!useEmptyReload, useEmptyReload);

        reloadAnimCoroutine = null;
    }

    private void SetWeaponReloadBools(bool reload, bool emptyReload)
    {
        if (weaponAnimator == null)
            return;

        weaponAnimator.SetBool("Reload", reload);
        weaponAnimator.SetBool("EmptyReload", emptyReload);
        weaponAnimator.SetBool("ReloadLoop", false);
        weaponAnimator.SetBool("EndReload", false);
    }

    private void ResetWeaponAnimatorToIdle()
    {
        if (weaponAnimator == null)
            return;

        weaponAnimator.SetBool("Fire", false);
        weaponAnimator.SetBool("AltFire", false);
        SetWeaponReloadBools(false, false);
        weaponAnimator.SetBool("Idle", true);
    }

    private IEnumerator ReloadAnimatorFallbackRoutine()
    {
        float waitTime = gunData.reloadTime + 0.2f;
        yield return new WaitForSeconds(waitTime);

        if (reloadAnimatorResetReceived)
            yield break;

        if (weaponAnimator == null)
            yield break;

        bool reload = weaponAnimator.GetBool("Reload");
        bool emptyReload = weaponAnimator.GetBool("EmptyReload");
        bool idle = weaponAnimator.GetBool("Idle");

        if (reload || emptyReload || !idle)
            ResetWeaponAnimatorToIdle();

        reloadAnimatorFallbackCoroutine = null;
    }

    private void CancelReloadAnimatorFallback()
    {
        if (reloadAnimatorFallbackCoroutine != null)
        {
            StopCoroutine(reloadAnimatorFallbackCoroutine);
            reloadAnimatorFallbackCoroutine = null;
        }
    }

    public void AddAmmo(float percent) // ví dụ percent = 0.3f cho 30%
    {
        int ammoToAdd = Mathf.CeilToInt(gunData.magSize * percent);
        currentMagsize = Mathf.Min(currentMagsize + ammoToAdd, gunData.magSize);
    }

    /// <summary>Gọi từ Animation Event reload trên model súng FPS.</summary>
    public void CompleteReloadFromAnimation()
    {
        if (gunData == null)
            return;

        CancelInvoke(nameof(FinishReload));
        FinishReload();
    }

    public void PlayMuzzleFlashFromAnimation()
    {
        SpawnMuzzleFlash();
    }

    private void FinishReload()
    {
        int bulletsNeeded = gunData.maxAmmo - currentAmmo;
        int bulletsToReload = HasInfiniteReserve
            ? bulletsNeeded
            : Mathf.Min(bulletsNeeded, currentMagsize);

        currentAmmo += bulletsToReload;

        if (!HasInfiniteReserve)
            currentMagsize -= bulletsToReload;

        isReloading = false;
    }

    private void OnDisable()
    {
        if (reloadAnimCoroutine != null)
        {
            StopCoroutine(reloadAnimCoroutine);
            reloadAnimCoroutine = null;
        }

        CancelReloadAnimatorFallback();
    }

}
