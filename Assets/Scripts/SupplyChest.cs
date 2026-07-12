using UnityEngine;

public class SupplyPickup : MonoBehaviour
{
    [SerializeField] private Animation anim;
    [SerializeField] private AudioSource openSound;
    public float multipliedAmmoAmount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Da cham player");
            anim.Play();
            openSound?.Play();
            if (multipliedAmmoAmount > 0) other.GetComponentInChildren<GunBase>().AddAmmo(multipliedAmmoAmount);
            Destroy(gameObject, 2f);
        }
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        Debug.Log("Da cham player");
    //        anim.Play();
    //        if (multipliedAmmoAmount > 0) collision.gameObject.GetComponentInChildren<GunBase>().AddAmmo(multipliedAmmoAmount);
    //        Destroy(gameObject);
    //    }
    //}
}
