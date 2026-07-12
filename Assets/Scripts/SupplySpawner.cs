using UnityEngine;

public class SupplySpawner : MonoBehaviour
{
    public GameObject supplyPrefab;
    public Transform[] spawnPoints;
    public float respawnTime = 30f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnAmmo), 3f, respawnTime);
    }

    void SpawnAmmo()
    {
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(supplyPrefab, point.position, point.rotation);
    }
}
