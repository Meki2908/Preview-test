using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "GunData", menuName = "Scriptable Objects/GunData")]
public class GunData : ScriptableObject
{
    public string weaponName;
    public int magSize;
    public int maxAmmo;
    public float fireRate;
    public float reloadTime;
    public float bulletSpeed;
    public int damage;
    public GameObject bulletPrefab;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
    public GameObject bloodFX;
    public GameObject bulletTracerPrefab;
    public bool isHitscan = true;
    public Sprite icon; // cho UI
    [Tooltip("Khi bật: băng đạn vẫn giảm khi bắn, tự reload, dự trữ vô hạn. UI: cur / ∞")]
    [FormerlySerializedAs("infiniteAmmo")]
    public bool infiniteReserve = true;
}
