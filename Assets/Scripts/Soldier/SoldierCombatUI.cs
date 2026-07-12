using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI combat cho Soldier: bắn (giữ nút), đổi súng.
/// Grenade dùng GrenadeJoystick trên UI joystick riêng.
/// </summary>
public class SoldierCombatUI : MonoBehaviour
{
    [SerializeField] private SoldierShooting soldierShooting;
    [SerializeField] private HoldButton fireButton;
    [SerializeField] private Button switchWeaponButton;
    [SerializeField] private Button reloadButton;
    [SerializeField] private bool hideReloadButton = true;

    private void Awake()
    {
        if (soldierShooting == null)
            soldierShooting = FindAnyObjectByType<SoldierShooting>();

        if (hideReloadButton && reloadButton != null)
            reloadButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (fireButton != null && fireButton.IsHeld)
            soldierShooting?.TryShoot();
    }

    private void OnEnable()
    {
        if (switchWeaponButton != null)
            switchWeaponButton.onClick.AddListener(OnSwitchWeapon);
        if (reloadButton != null && !hideReloadButton)
            reloadButton.onClick.AddListener(OnReload);
    }

    private void OnDisable()
    {
        if (switchWeaponButton != null)
            switchWeaponButton.onClick.RemoveListener(OnSwitchWeapon);
        if (reloadButton != null && !hideReloadButton)
            reloadButton.onClick.RemoveListener(OnReload);
    }

    private void OnSwitchWeapon() => soldierShooting?.SwitchNextWeapon();
    private void OnReload() => soldierShooting?.Reload();
}
