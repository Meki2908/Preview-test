using UnityEngine;

/// <summary>
/// Combat cho Soldier: bắn qua right joystick, đổi súng, ném bom.
/// Drive animator parameters: Shoot, WeaponType, ThrowBomb.
/// </summary>
[RequireComponent(typeof(SoldierController), typeof(Animator))]
public class SoldierShooting : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private int pistolWeaponType = 0;
    [SerializeField] private int rifleWeaponType = 1;

    [Header("Fire")]
    [SerializeField] private float aimFireThreshold = 0.5f;
    [SerializeField] private bool fireWhileAiming = true;

    [Header("Grenade")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private Transform grenadeSpawnPoint;
    [SerializeField] private float minThrowForce = 6f;
    [SerializeField] private float maxThrowForce = 22f;
    [SerializeField] private float grenadeCooldown = 1.5f;

    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int ThrowBombHash = Animator.StringToHash("ThrowBomb");

    private SoldierController soldier;
    private Animator bodyAnimator;
    private GunBase[] weapons;
    private int currentWeaponIndex;
    private float nextGrenadeTime;
    private bool isDead;
    private bool isGrenadeAiming;
    private bool hasPendingThrow;
    private Vector3 pendingThrowDirection;
    private float pendingThrowForce;

    public GunBase CurrentGun { get; private set; }
    public int CurrentWeaponIndex => currentWeaponIndex;
    public bool IsGrenadeAiming => isGrenadeAiming;

    private void Awake()
    {
        soldier = GetComponent<SoldierController>();
        bodyAnimator = GetComponent<Animator>();

        if (weaponHolder == null)
        {
            Transform holder = transform.Find("WeaponHolder");
            if (holder != null)
                weaponHolder = holder;
        }

        if (weaponHolder != null)
        {
            weapons = weaponHolder.GetComponentsInChildren<GunBase>(true);
            foreach (GunBase weapon in weapons)
            {
                weapon.ConfigureForSoldier(bodyAnimator);
            }

            if (weapons.Length > 0)
                SelectWeapon(0);
        }
    }

    private void Update()
    {
        if (isDead || !soldier.InputEnabled)
            return;

        if (fireWhileAiming)
            HandleAutoFire();
    }

    private void HandleAutoFire()
    {
        if (CurrentGun == null || soldier.RightJoystick == null || isGrenadeAiming)
            return;

        Vector2 aim = soldier.RightJoystick.GetAxis();
        if (aim.magnitude < aimFireThreshold)
            return;

        Vector3 aimDirection = new Vector3(aim.x, 0f, aim.y).normalized;
        if (CurrentGun.TryShoot(aimDirection))
            bodyAnimator.SetTrigger(ShootHash);
    }

    public void TryShoot()
    {
        if (isDead || CurrentGun == null || isGrenadeAiming)
            return;

        if (CurrentGun.TryShoot(transform.forward))
            bodyAnimator.SetTrigger(ShootHash);
    }

    public void Reload()
    {
        CurrentGun?.Reload();
    }

    public void SwitchNextWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        SelectWeapon((currentWeaponIndex + 1) % weapons.Length);
    }

    public void SwitchPreviousWeapon()
    {
        if (weapons == null || weapons.Length == 0)
            return;

        int next = currentWeaponIndex - 1;
        if (next < 0)
            next = weapons.Length - 1;

        SelectWeapon(next);
    }

    public void SelectWeapon(int index)
    {
        if (weapons == null || weapons.Length == 0)
            return;

        currentWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);

        for (int i = 0; i < weapons.Length; i++)
        {
            bool active = i == currentWeaponIndex;
            weapons[i].gameObject.SetActive(active);
            weapons[i].enabled = active;
        }

        CurrentGun = weapons[currentWeaponIndex];
        CurrentGun.EnsureWeaponAnimatorIdle();
        bodyAnimator.SetFloat(WeaponTypeHash, GetWeaponTypeForGun(CurrentGun));
    }

    public void SetGrenadeAiming(bool aiming)
    {
        isGrenadeAiming = aiming;
    }

    /// <summary>Thả grenade joystick: lưu hướng/lực rồi chạy animation ném.</summary>
    public void CommitGrenadeThrow(Vector3 worldDirection, float pullMagnitude)
    {
        if (isDead || Time.time < nextGrenadeTime)
            return;

        if (worldDirection.sqrMagnitude < 0.01f)
            worldDirection = transform.forward;
        else
            worldDirection.Normalize();

        pendingThrowDirection = worldDirection;
        pendingThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, Mathf.Clamp01(pullMagnitude));
        hasPendingThrow = true;
        nextGrenadeTime = Time.time + grenadeCooldown;
        bodyAnimator.SetTrigger(ThrowBombHash);
    }

    /// <summary>Gọi từ Animation Event trên clip Throw Grenade.</summary>
    public void OnGrenadeRelease()
    {
        if (!hasPendingThrow || grenadePrefab == null)
        {
            hasPendingThrow = false;
            return;
        }

        Transform spawn = grenadeSpawnPoint != null ? grenadeSpawnPoint : transform;
        GameObject grenade = Instantiate(grenadePrefab, spawn.position, Quaternion.LookRotation(pendingThrowDirection, Vector3.up));

        if (grenade.TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.AddForce(pendingThrowDirection * pendingThrowForce, ForceMode.Impulse);

        hasPendingThrow = false;
    }

    public void SetDead(bool dead)
    {
        isDead = dead;
    }

    private int GetWeaponTypeForGun(GunBase gun)
    {
        if (gun is Pistol)
            return pistolWeaponType;

        return rifleWeaponType;
    }
}
