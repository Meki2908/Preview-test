using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Level 2: endless minion spawns + win when boss Mutant dies.
/// </summary>
public class Level2Director : MonoBehaviour
{
    [Header("Target Setup")]
    [SerializeField] private EnemyBase bossMutant;

    [Header("Endless Spawner")]
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private int maxZombiesAlive = 15;

    [Header("Spawn Placement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float spawnHeight = 10f;
    [SerializeField] private float groundRayDistance = 50f;
    [SerializeField] private float navMeshSampleRadius = 5f;

    [Header("Victory Sequence")]
    [SerializeField] private float victoryDelaySeconds = 3f;

    private readonly List<EnemyBase> activeZombies = new List<EnemyBase>();
    private bool isBossDead;

    private void Start()
    {
        if (!ValidateSetup())
            return;

        if (bossMutant != null)
            bossMutant.OnEnemyDeath += HandleBossDeath;

        StartCoroutine(EndlessSpawnRoutine());
    }

    private bool ValidateSetup()
    {
        if (bossMutant == null)
            Debug.LogError("[Level2Director] Boss Mutant chưa được gán!", this);

        if (zombiePrefab == null)
        {
            Debug.LogError("[Level2Director] zombiePrefab chưa được gán!", this);
            enabled = false;
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Level2Director] spawnPoints trống!", this);
            enabled = false;
            return false;
        }

        return true;
    }

    private IEnumerator EndlessSpawnRoutine()
    {
        while (!isBossDead && GameManager.Instance != null && !GameManager.Instance.isGameOver)
        {
            activeZombies.RemoveAll(z =>
            {
                if (z == null)
                    return true;

                Health health = z.GetComponent<Health>();
                return health == null || health.IsDead;
            });

            if (activeZombies.Count < maxZombiesAlive)
            {
                Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                if (spawnPoint != null)
                    SpawnMinion(spawnPoint);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnMinion(Transform spawnPoint)
    {
        Vector3 initialPosition = spawnPoint.position + Vector3.up * spawnHeight;
        GameObject zombieObject = Instantiate(zombiePrefab, initialPosition, spawnPoint.rotation);
        PlaceOnGroundAndNavMesh(zombieObject, spawnPoint.position);

        EnemyBase enemy = zombieObject.GetComponent<EnemyBase>();
        if (enemy == null)
            enemy = zombieObject.GetComponentInChildren<EnemyBase>();

        if (enemy == null)
        {
            Debug.LogWarning($"[Level2Director] '{zombiePrefab.name}' không có EnemyBase.", zombieObject);
            Destroy(zombieObject);
            return;
        }

        activeZombies.Add(enemy);

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

    private void HandleBossDeath(EnemyBase boss)
    {
        if (boss != null)
            boss.OnEnemyDeath -= HandleBossDeath;

        if (isBossDead)
            return;

        isBossDead = true;
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        Debug.Log("[Level2Director] Boss gục ngã — dọn dẹp chiến trường...");

        for (int i = 0; i < activeZombies.Count; i++)
        {
            EnemyBase minion = activeZombies[i];
            if (minion == null)
                continue;

            Health health = minion.GetComponent<Health>();
            if (health != null && !health.IsDead)
                health.TakeDamage(health.MaxHealthPoint, minion.transform.position);
        }

        yield return new WaitForSeconds(victoryDelaySeconds);

        if (GameManager.Instance != null)
            GameManager.Instance.Victory();
    }

    private void OnDisable()
    {
        if (bossMutant != null)
            bossMutant.OnEnemyDeath -= HandleBossDeath;
    }
}
