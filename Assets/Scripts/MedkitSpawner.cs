using UnityEngine;

public class MedkitSpawner : MonoBehaviour
{
    public GameObject medkitPrefab;
    public Transform[] spawnPoints;
    public float respawnTime = 30f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnMedkit), 3f, respawnTime);
    }

    void SpawnMedkit()
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(medkitPrefab, point.position, point.rotation);
    }
}
