using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject canvasPrefab; // Canvas riêng cho UI

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public GameObject SpawnPlayer()
    {
        // Chọn spawn point random
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject player = Instantiate(playerPrefab, spawn.position, spawn.rotation);

        // Spawn Canvas riêng cho Player
        GameObject canvas = Instantiate(canvasPrefab);
        if (canvas != null && canvas.GetComponent<HealthUIBootstrap>() == null)
            canvas.AddComponent<HealthUIBootstrap>();

        TopDownCinemachineCamera topDownCamera = FindAnyObjectByType<TopDownCinemachineCamera>();
        if (topDownCamera != null)
            topDownCamera.SetFollowTarget(CameraTargetAnchor.GetOrCreate(player.transform));

        PlayerTarget.Register(player.transform);

        return player;
    }
}
