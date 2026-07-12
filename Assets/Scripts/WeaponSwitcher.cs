using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [SerializeField] private Shooting shootingScript;
    //[SerializeField] private GameObject[] weaponPrefabs; // Các prefab súng
    //[SerializeField] private Transform weaponHolder;     // Chỗ gắn súng lên playe
    //private GameObject currentWeapon;
    public int currentWeaponIndex = 0;

    private void Start()
    {
        if (GetComponentInParent<SoldierShooting>() != null || FindAnyObjectByType<SoldierShooting>() != null)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        int previousWeaponIndex = currentWeaponIndex;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % transform.childCount;
        }
        else if (scroll < 0f)
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0)
                currentWeaponIndex = transform.childCount - 1;
        }

        if (previousWeaponIndex != currentWeaponIndex)
        {
            SelectWeapon();
        }
    }

    void SelectWeapon()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform weapon = transform.GetChild(i);
            bool isActiveWeapon = (i == currentWeaponIndex);

            // Bật/tắt game object
            weapon.gameObject.SetActive(isActiveWeapon);

            // Bật/tắt script
            GunBase gunScript = weapon.GetComponent<GunBase>();
            if (gunScript != null)
            {
                gunScript.enabled = isActiveWeapon;

                // Cập nhật current gun cho Shooting
                if (isActiveWeapon)
                    shootingScript.SetCurrentWeapon(weapon.gameObject);
            }
        }
    }
}
