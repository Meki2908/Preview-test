using UnityEngine;

public class Hitbox : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private GunData gunData;
    [SerializeField] public float damageMultiplier = 1;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();
    }

    public void TakeHit(int baseDamage)
    {
        if (health == null)
            return;

        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        health.TakeDamage(finalDamage);
    }
}
