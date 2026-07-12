using UnityEngine;

public class Medkit : MonoBehaviour
{
    [SerializeField] private AudioSource healSound;
    public int healamount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player nhan medkit");
            healSound.Play();
            if (healamount > 0) other.GetComponent<PlayerHealth>().Heal(healamount);
            Destroy(gameObject, 0.5f);
        }
    }
}
