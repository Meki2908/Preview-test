using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Shooting : MonoBehaviour
{
    public static Shooting Instance { get; private set; }
    [SerializeField] private InputActionReference _shootAction;

    public GunBase currentGun;
    private void Start()
    {
        if (GetComponentInParent<SoldierShooting>() != null || FindAnyObjectByType<SoldierShooting>() != null)
            return;

        if (AmmoDisplay.instance != null)
            AmmoDisplay.instance.SetShooting(this);
    }

    private void Awake()
    {
        if (GetComponentInParent<SoldierShooting>() != null || FindAnyObjectByType<SoldierShooting>() != null)
        {
            enabled = false;
            return;
        }

        currentGun = GetComponentInChildren<GunBase>();
    }

    private void Update()
    {
        if (currentGun == null) return;

        // Giữ chuột => bắn tự động
        if (_shootAction.action.IsPressed())
        {
            currentGun.Shoot();
        }
    }

    public void SetCurrentWeapon(GameObject weaponObject)
    {
        currentGun = weaponObject.GetComponent<GunBase>();
    }
}
