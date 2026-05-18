using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager Instance { get; private set; }

    [Header("Agents")]
    [SerializeField] private List<AgentSoldier> agents = new();
    [SerializeField] private Transform[] agentSpawnPoints;

    [Header("Enemies")]
    [SerializeField] private int maxEnemiesAlive = 5;
    [SerializeField] private List<EnemyData> enemyPool = new();
    [SerializeField] private Transform[] enemySpawnPoints;

    [Header("Crystal")]
    [SerializeField] private Structure crystal;
    [SerializeField] private float crystalMaxHealth = 500f;

    [Header("Episode")]
    [SerializeField] private float episodeTimeLimit = 120f;
    [SerializeField] private float episodeCompletionBonus = 1f;
    [SerializeField] private float crystalDamagePenalty = 0.5f;

    private readonly List<GameObject> activeEnemies = new();
    private float episodeTimer;
    private bool episodeRunning = false;
    private int agentsAlive = 0;

    #region Unity methods

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ValidateSetup();
        ResetEpisode();
    }

    private void Update()
    {
        if (!episodeRunning)
        {
            return;
        }

        episodeTimer += Time.deltaTime;

        if (episodeTimer >= episodeTimeLimit)
        {
            // Crystal survived episode
            GiveSharedReward(episodeCompletionBonus);
            NotifyEpisodeEnd();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromCrystal();
    }

    #endregion

    #region Episode lifecycle

    private void ResetEpisode()
    {
        episodeRunning = false;
        episodeTimer = 0f;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObject = activeEnemies[i];
            if (enemyObject == null)
            {
                continue;
            }

            if (enemyObject.TryGetComponent<Unit>(out Unit unit))
            {
                // Skip death animation during training
                unit.ForceKill();
            }
            else
            {
                Destroy(enemyObject);
            }
        }
        activeEnemies.Clear();

        ResetCrystal();

        // Reset and reposition agents
        agentsAlive = 0;
        for (int i = 0; i < agents.Count; i++)
        {
            AgentSoldier agent = agents[i];
            if (agent == null)
            {
                continue;
            }

            if (i < agentSpawnPoints.Length && agentSpawnPoints[i] != null)
            {
                agent.transform.SetPositionAndRotation(
                    agentSpawnPoints[i].position,
                    agentSpawnPoints[i].rotation
                );
            }

            agentsAlive++;
            agent.EndEpisode();
        }

        SpawnEnemies();

        episodeRunning = true;
    }

    public void NotifyEpisodeEnd()
    {
        if (!episodeRunning)
        {
            return;
        }

        episodeRunning = false;
        ResetEpisode();
    }

    #endregion

    #region Agent tracking

    public void NotifyAgentDied(AgentSoldier agent)
    {
        agentsAlive = Mathf.Max(0, agentsAlive - 1);

        if (agentsAlive == 0)
        {
            NotifyEpisodeEnd();
        }
    }

    public void NotifyCrystalDamaged(float amount)
    {
        GiveSharedReward(-crystalDamagePenalty * amount);
    }

    #endregion

    #region Enemy spawning

    private void SpawnEnemies()
    {
        if (enemyPool == null || enemyPool.Count == 0
            || enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: enemy pool or spawn points not configured");
            return;
        }

        int toSpawn = maxEnemiesAlive - activeEnemies.Count;
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPool == null || enemyPool.Count == 0)
        {
            return;
        }

        EnemyData data = enemyPool[Random.Range(0, enemyPool.Count)];
        Transform spawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];

        StartCoroutine(SpawnEnemyCoroutine(data, spawnPoint));
    }

    private IEnumerator SpawnEnemyCoroutine(EnemyData data, Transform spawnPoint)
    {
        AssetReferenceGameObject prefabReference = data.GetRandomPrefabReference();
        if (prefabReference == null)
        {
            Debug.LogWarning($"{name}: no valid prefab reference for {data.Name}");
            yield break;
        }

        AsyncOperationHandle<GameObject> handle = prefabReference.InstantiateAsync(
            spawnPoint.position,
            spawnPoint.rotation
        );

        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"{name}: failed to instantiate {data.Name}");
            yield break;
        }

        GameObject spawnedObject = handle.Result;

        if (!episodeRunning)
        {
            Addressables.ReleaseInstance(spawnedObject);
            yield break;
        }

        if (!spawnedObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            Debug.LogWarning($"{name}: spawned object missing {nameof(Enemy)} component");
            Addressables.ReleaseInstance(spawnedObject);
            yield break;
        }

        if (spawnedObject.TryGetComponent<IAddressableInstance>(out IAddressableInstance addressable))
        {
            addressable.SetAddressableInstanceHandle(handle);
        }

        enemy.Initialize(crystal.transform, Enemy.Behaviour.Aggressive);
        activeEnemies.Add(spawnedObject);

        if (spawnedObject.TryGetComponent<Unit>(out Unit unit))
        {
            unit.OnDeathCleanupEvent += () => OnEnemyDied(spawnedObject);
        }
    }

    private void OnEnemyDied(GameObject enemyObject)
    {
        activeEnemies.Remove(enemyObject);

        if (episodeRunning)
        {
            SpawnEnemy();
        }
    }

    #endregion

    #region Crystal

    private void ResetCrystal()
    {
        if (crystal == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(crystal)}");
            return;
        }

        UnsubscribeFromCrystal();

        crystal.maxHealth = crystalMaxHealth;
        crystal.currentHealth = crystalMaxHealth;

        crystal.OnDamageTakenEvent += OnCrystalDamageTaken;
        crystal.OnDeathStartedEvent += OnCrystalDied;
    }

    private void UnsubscribeFromCrystal()
    {
        if (crystal == null)
        {
            return;
        }

        crystal.OnDamageTakenEvent -= OnCrystalDamageTaken;
        crystal.OnDeathStartedEvent -= OnCrystalDied;
    }

    private void OnCrystalDamageTaken(float amount, Vector3 attackOrigin)
    {
        NotifyCrystalDamaged(amount);
    }

    private void OnCrystalDied(ITarget deadTarget)
    {
        NotifyEpisodeEnd();
    }

    #endregion

    private void GiveSharedReward(float reward)
    {
        foreach (AgentSoldier agent in agents)
        {
            if (agent != null)
            {
                agent.AddReward(reward);
            }
        }
    }

    private void ValidateSetup()
    {
        if (agents == null || agents.Count == 0)
        {
            Debug.LogError($"{name}: no agents assigned");
        }
        if (agentSpawnPoints == null || agentSpawnPoints.Length < agents.Count)
        {
            Debug.LogWarning($"{name}: fewer agent spawn points than agents");
        }
        if (enemyPool == null || enemyPool.Count == 0)
        {
            Debug.LogError($"{name}: enemy pool is empty");
        }
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogError($"{name}: no enemy spawn points assigned");
        }
        if (crystal == null)
        {
            Debug.LogError($"{name}: crystal not assigned");
        }
    }
}