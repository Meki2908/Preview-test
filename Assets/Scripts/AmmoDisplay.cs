using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplay : MonoBehaviour
{
    public static AmmoDisplay instance { get; private set; }

    private Shooting shootingScript;
    private SoldierShooting soldierShooting;

    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image gunIcon;

    private void Awake()
    {
        instance = this;
        soldierShooting = FindAnyObjectByType<SoldierShooting>();
    }

    private void Update()
    {
        GunBase gun = soldierShooting != null ? soldierShooting.CurrentGun : shootingScript?.currentGun;

        if (gun != null && ammoText != null)
        {
            ammoText.text = gun.AmmoDisplayText;
            if (gunIcon != null && gun.gunData != null && gun.gunData.icon != null)
            {
                gunIcon.sprite = gun.gunData.icon;
                gunIcon.enabled = true;
            }
            else if (gunIcon != null)
            {
                gunIcon.enabled = false;
            }
        }
        else if (ammoText != null)
        {
            ammoText.text = "No Gun";
            if (gunIcon != null)
                gunIcon.enabled = false;
        }
    }

    public void SetShooting(Shooting shooting)
    {
        shootingScript = shooting;
    }
}
