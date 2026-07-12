using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển toàn bộ flow wave của level:
/// countdown -> spawn -> chờ diệt sạch -> wave kế tiếp -> chuyển level.
/// Giữ tên ZombieSpawner để scene cũ không mất các prefab/spawn point đã gán.
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ZombieType
    {
        public string name;
        public GameObject prefab;
        [Min(0f)] public float spawnChance = 1f;
    }

    [System.Serializable]
    public class WaveData
    {
        public string waveName = "WAVE";
        [Min(1)] public int totalZombiesToSpawn = 5;
        [Min(0f)] public float spawnInterval = 1.5f;
        [Tooltip("Để trống sẽ dùng Zombie Types chung bên dưới.")]
        public ZombieType[] zombieTypes;
    }

    [Header("Legacy / Shared Zombie Types")]
    [Tooltip("Các loại zombie đã gán ở spawner cũ. Wave để trống Zombie Types sẽ dùng danh sách này.")]
    public ZombieType[] zombieTypes;
    public Transform[] spawnPoints;
    [Min(0f)] public float spawnInterval = 1.5f;

    [Header("Wave Settings")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private int[] defaultWaveCounts = { 5, 8, 12 };
    [SerializeField] private float restBetweenWaves = 1.5f;
    [SerializeField] private int countdownSeconds = 3;

    [Header("UI & Transition")]
    [SerializeField] private TextMeshProUGUI centerMessageText;
    [SerializeField] private int nextLevelBuildIndex = GameSceneIndex.Level2;

    [Header("Spawn Placement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float groundRayDistance = 50f;
    [SerializeField] private float navMeshSampleRadius = 5f;

    private readonly HashSet<EnemyBase> activeZombies = new HashSet<EnemyBase>();
    public int ActiveZombieCount
    {
        get
        {
            RemoveDestroyedZombies();
            return activeZombies.Count;
        }
    }

    private void Start()
    {
        if (!ValidateSetup())
            return;

        EnsureMessageText();
        StartCoroutine(MasterWaveFlowRoutine());
    }

    private void OnDisable()
    {
        UnsubscribeAllZombies();
    }

    private bool ValidateSetup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WAVE] ZombieSpawner chưa có Spawn Points.", this);
            enabled = false;
            return false;
        }

        if (!HasAnyValidZombieType(waves) && !HasAnyValidZombieType(zombieTypes))
        {
            Debug.LogError("[WAVE] ZombieSpawner chưa có zombie prefab hợp lệ.", this);
            enabled = false;
            return false;
        }

        return true;
    }

    private IEnumerator MasterWaveFlowRoutine()
    {
        WaveData[] runtimeWaves = GetRuntimeWaves();

        for (int i = 0; i < runtimeWaves.Length; i++)
        {
            if (IsGameOver())
                yield break;

            WaveData wave = runtimeWaves[i];
            string waveName = string.IsNullOrWhiteSpace(wave.waveName)
                ? $"WAVE {i + 1}"
                : wave.waveName;

            yield return CountdownRoutine(waveName, countdownSeconds);
            SetMessageVisible(false);

            yield return SpawnWaveRoutine(wave);
            yield return WaitForWaveCleared();

            if (IsGameOver())
                yield break;

            yield return new WaitForSeconds(restBetweenWaves);
        }

        yield return CountdownRoutine("LEVEL 1 COMPLETE!", countdownSeconds);
        SetMessageVisible(false);

        if (IsGameOver())
            yield break;

        if (nextLevelBuildIndex < 0 || nextLevelBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"[WAVE] Build index {nextLevelBuildIndex} không tồn tại. Kiểm tra Build Settings.",
                this);
            yield break;
        }

        SceneManager.LoadScene(nextLevelBuildIndex);
    }

    private IEnumerator CountdownRoutine(string message, int seconds)
    {
        SetMessageVisible(true);

        for (int i = Mathf.Max(0, seconds); i > 0; i--)
        {
            if (IsGameOver())
                yield break;

            if (centerMessageText != null)
                centerMessageText.text = $"{message}\n{i}";

            yield return new WaitForSeconds(1f);
        }

        if (centerMessageText != null)
            centerMessageText.text = "FIGHT!";

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator SpawnWaveRoutine(WaveData wave)
    {
        int total = Mathf.Max(1, wave.totalZombiesToSpawn);
        float interval = wave.spawnInterval > 0f ? wave.spawnInterval : spawnInterval;
        ZombieType[] availableTypes = HasAnyValidZombieType(wave.zombieTypes)
            ? wave.zombieTypes
            : zombieTypes;

        for (int i = 0; i < total; i++)
        {
            if (IsGameOver())
                yield break;

            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            if (spawnPoint != null)
                SpawnSingleZombie(availableTypes, spawnPoint);

            if (i < total - 1 && interval > 0f)
                yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator WaitForWaveCleared()
    {
        while (!IsGameOver())
        {
            RemoveDestroyedZombies();
            if (activeZombies.Count == 0)
                yield break;

            yield return null;
        }
    }

    private void SpawnSingleZombie(ZombieType[] types, Transform spawnPoint)
    {
        GameObject prefab = GetWeightedRandomZombie(types);
        if (prefab == null)
            return;

        Vector3 initialPosition = spawnPoint.position + Vector3.up * spawnHeight;
        GameObject zombieObject = Instantiate(prefab, initialPosition, spawnPoint.rotation);
        PlaceOnGroundAndNavMesh(zombieObject, spawnPoint.position);

        EnemyBase enemy = zombieObject.GetComponent<EnemyBase>();
        if (enemy == null)
            enemy = zombieObject.GetComponentInChildren<EnemyBase>();

        if (enemy == null)
        {
            Debug.LogWarning($"[WAVE] '{prefab.name}' không có EnemyBase, không thể theo dõi death.", zombieObject);
            Destroy(zombieObject);
            return;
        }

        if (activeZombies.Add(enemy))
            enemy.OnEnemyDeath += HandleZombieDied;

        ZombieVisual visual = zombieObject.GetComponentInChildren<ZombieVisual>();
        if (visual != null)
            visual.RefreshTarget();
    }

    private void PlaceOnGroundAndNavMesh(GameObject zombieObject, Vector3 spawnPointPosition)
    {
        int mask = groundMask.value != 0 ? groundMask.value : Physics.DefaultRaycastLayers;
        Vector3 rayOrigin = spawnPointPosition + Vector3.up * spawnHeight;
        Vector3 groundPosition = spawnPointPosition;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, mask))
            groundPosition = hit.point;

        if (NavMesh.SamplePosition(groundPosition, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
            groundPosition = navHit.position;

        NavMeshAgent agent = zombieObject.GetComponentInChildren<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            zombieObject.transform.position = groundPosition;
            if (agent.isOnNavMesh)
                agent.Warp(groundPosition);
        }
        else
        {
            zombieObject.transform.position = groundPosition;
        }
    }

    private void HandleZombieDied(EnemyBase deadZombie)
    {
        if (deadZombie == null)
            return;

        deadZombie.OnEnemyDeath -= HandleZombieDied;
        activeZombies.Remove(deadZombie);
    }

    private void RemoveDestroyedZombies()
    {
        activeZombies.RemoveWhere(zombie => zombie == null);
    }

    private void UnsubscribeAllZombies()
    {
        foreach (EnemyBase zombie in activeZombies)
        {
            if (zombie != null)
                zombie.OnEnemyDeath -= HandleZombieDied;
        }

        activeZombies.Clear();
    }

    private WaveData[] GetRuntimeWaves()
    {
        if (waves != null && waves.Length > 0)
            return waves;

        int[] counts = defaultWaveCounts != null && defaultWaveCounts.Length > 0
            ? defaultWaveCounts
            : new[] { 5, 8, 12 };

        WaveData[] generated = new WaveData[counts.Length];
        for (int i = 0; i < counts.Length; i++)
        {
            generated[i] = new WaveData
            {
                waveName = $"WAVE {i + 1}",
                totalZombiesToSpawn = Mathf.Max(1, counts[i]),
                spawnInterval = spawnInterval,
                zombieTypes = zombieTypes
            };
        }

        return generated;
    }

    private GameObject GetWeightedRandomZombie(ZombieType[] types)
    {
        if (types == null || types.Length == 0)
            return null;

        float totalWeight = 0f;
        GameObject firstValidPrefab = null;

        for (int i = 0; i < types.Length; i++)
        {
            ZombieType type = types[i];
            if (type == null || type.prefab == null)
                continue;

            firstValidPrefab ??= type.prefab;
            totalWeight += Mathf.Max(0f, type.spawnChance);
        }

        if (firstValidPrefab == null || totalWeight <= 0f)
            return firstValidPrefab;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            ZombieType type = types[i];
            if (type == null || type.prefab == null)
                continue;

            cumulative += Mathf.Max(0f, type.spawnChance);
            if (roll <= cumulative)
                return type.prefab;
        }

        return firstValidPrefab;
    }

    private static bool HasAnyValidZombieType(WaveData[] waveList)
    {
        if (waveList == null)
            return false;

        for (int i = 0; i < waveList.Length; i++)
        {
            if (waveList[i] != null && HasAnyValidZombieType(waveList[i].zombieTypes))
                return true;
        }

        return false;
    }

    private static bool HasAnyValidZombieType(ZombieType[] types)
    {
        if (types == null)
            return false;

        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] != null && types[i].prefab != null)
                return true;
        }

        return false;
    }

    private bool IsGameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.isGameOver;
    }

    private void EnsureMessageText()
    {
        if (centerMessageText != null)
            return;

        GameObject existing = FindObjectIncludingInactive("WaveMessageText");
        if (existing != null)
            centerMessageText = existing.GetComponent<TextMeshProUGUI>();

        if (centerMessageText != null)
            return;

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject(
                "WaveCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject textObject = new GameObject("WaveMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 220f);

        centerMessageText = textObject.GetComponent<TextMeshProUGUI>();
        centerMessageText.alignment = TextAlignmentOptions.Center;
        centerMessageText.fontSize = 50f;
        centerMessageText.fontStyle = FontStyles.Bold;
        centerMessageText.color = new Color(1f, 0.75f, 0.1f);
        centerMessageText.outlineColor = Color.black;
        centerMessageText.outlineWidth = 0.25f;
        centerMessageText.raycastTarget = false;
        centerMessageText.gameObject.SetActive(false);
    }

    private void SetMessageVisible(bool visible)
    {
        if (centerMessageText != null)
            centerMessageText.gameObject.SetActive(visible);
    }

    private static Canvas FindHudCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode != RenderMode.WorldSpace)
                return canvases[i];
        }

        return null;
    }

    private static GameObject FindObjectIncludingInactive(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }
}
