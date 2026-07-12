using UnityEngine;

public class DmgDealTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(1000);
                Debug.Log("Gây damage cho enemy!");
            }
        }
    }
}
