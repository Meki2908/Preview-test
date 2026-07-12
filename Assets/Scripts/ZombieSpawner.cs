using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ZombieType
    {
        public string name; // Runner, Walker, Mutant
        public GameObject prefab;
        [Range(0f, 1f)] public float spawnChance; // Tỷ lệ xuất hiện (0-1)
    }

    public ZombieType[] zombieTypes; // Mảng loại zombie
    public Transform[] spawnPoints;  // Vị trí spawn

    public float spawnInterval = 3f;
    private float spawnTimer;

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnZombie();
            spawnTimer = 0f;
        }
    }

    void SpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombieTypes.Length == 0) return;

        // Chọn vị trí spawn ngẫu nhiên
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Random loại zombie dựa trên spawnChance
        float roll = Random.value; // 0 -> 1
        float cumulative = 0f;

        foreach (ZombieType type in zombieTypes)
        {
            cumulative += type.spawnChance;
            if (roll <= cumulative)
            {
                GameObject zombie = Instantiate(type.prefab, spawnPoint.position + Vector3.up * 10f, spawnPoint.rotation);

                // Raycast xuống để tìm mặt đất/NavMesh
                RaycastHit hit;
                if (Physics.Raycast(zombie.transform.position, Vector3.down, out hit, 50f, LayerMask.GetMask("Ground")))
                {
                    // Warp NavMeshAgent xuống vị trí đúng
                    UnityEngine.AI.NavMeshAgent agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                        agent.Warp(hit.point);
                    else
                        zombie.transform.position = hit.point; // fallback nếu không có NavMeshAgent
                }

                break;
            }
        }
    }

}
