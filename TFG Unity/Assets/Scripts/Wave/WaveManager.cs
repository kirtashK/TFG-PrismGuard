using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WaveManager : MonoBehaviour
{
    [Header("Testing")]

    [SerializeField]
    [Tooltip("If false, waves wont be generated (for testing)")]
    private bool isEnabled_TESTING = true;

    [SerializeField]
    [Tooltip("If true, first wave will happen instantly")]
    private bool InstantFirstWave_TESTING = false;

    [SerializeField]
    [Tooltip("True to enable a limit on the budget")]
    private bool enableLimitBudget_TESTING = false;

    [SerializeField]
    [Tooltip("If enableLimitBudget is enabled, the budget wont pass limitBudget amount")]
    private int limitBudget_TESTING = 0;

    [Header("Data source")]

    [Tooltip("Label used in Addressables for EnemyData")]
    public string enemyLabel = "Unit";

    public List<EnemyData> enemyPool = new();

    private AsyncOperationHandle<IList<EnemyData>> loadHandle;
    private bool isLoaded = false;

    public event Action<List<EnemyData>> OnEnemyLoaded;

    [Header("Cost category")]

    [Tooltip("Maximun cost to consider it cheap")]
    public int cheapMaxCost = 5;
    [Tooltip("Maximun cost to consider it medium (>= cheapMaxCost)")]
    public int mediumMaxCost = 15;

    [Header("Wave distribution")]

    [Range(0, 1)] 
    public float pctCheap = 0.6f;
    [Range(0, 1)] 
    public float pctMedium = 0.3f;
    [Range(0, 1)] 
    public float pctExpensive = 0.1f;

    [Header("Wave budget")]

    [Tooltip("Budget for first wave")]
    public float initialBudget = 10f;
    [Tooltip("Linear increment per wave")]
    public float linearDelta = 5f;
    [Tooltip("Exponential factor r (>1)")]
    public float exponentialRate = 1.02f;

    [Header("Spawn")]

    public Transform spawnPoint;
    [Tooltip("Seconds between waves")]
    public float waveInterval = 100f;

    private bool waitingForNextWaveToComplete;

    private int waveIndex = 0;

    private readonly List<EnemyData> cheapList = new();
    private readonly List<EnemyData> mediumList = new();
    private readonly List<EnemyData> expensiveList = new();
    private int minCost;

    private int scoreAtWaveStart;

    [SerializeField]
    private WaveRewardCurve rewardCurve;

    private void Start()
    {
        if (isEnabled_TESTING)
        {
            StartCoroutine(LoadEnemies());
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenUIManagerReady());
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    private void OnDestroy()
    {
        if (isLoaded && loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
            enemyPool.Clear();
            isLoaded = false;
        }
    }

    private IEnumerator LoadEnemies()
    {
        if (isLoaded)
        {
            yield break;
        }

        loadHandle = Addressables.LoadAssetsAsync<EnemyData>(
            enemyLabel,
            enemyData => { }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            enemyPool = new List<EnemyData>(loadHandle.Result);
            isLoaded = true;

            OnEnemyLoaded?.Invoke(enemyPool);

            CategorizePool();
            StartCoroutine(RunWaves());
        }
        else
        {
            Debug.LogWarning($"{name}: failed to load {nameof(EnemyData)}");
        }
    }

    private IEnumerator RegisterWhenUIManagerReady()
    {
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        UIManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    private IEnumerator RunWaves()
    {
        if (InstantFirstWave_TESTING)
        {
            yield return SpawnWave();
            waitingForNextWaveToComplete = true;
        }
        else
        {
            waitingForNextWaveToComplete = false;
        }

        while (true)
        {
            // If there is an ongoing wave the timer for next wave wont start
            yield return new WaitUntil(() => waitingForNextWaveToComplete == false);

            float waveIntervalFirstWarning = waveInterval * 0.35f;
            float waveIntervalSecondWarning = waveInterval * 0.15f;

            yield return new WaitForSeconds(waveInterval - waveIntervalFirstWarning);

            UIManager.Instance.ShowTimeUntilWaveBanner(waveIndex + 1, waveIntervalFirstWarning);

            yield return new WaitForSeconds(waveIntervalFirstWarning - waveIntervalSecondWarning);

            UIManager.Instance.ShowTimeUntilWaveBanner(waveIndex + 1, waveIntervalSecondWarning);

            yield return new WaitForSeconds(waveIntervalSecondWarning);

            yield return SpawnWave();
        }
    }

    private void OnWaveCompleted(int waveNumber)
    {
        // Once the wave is completed (notified by event), timer will start
        waitingForNextWaveToComplete = false;

        int gainedThisWave = ScoreManager.Instance.CurrentScore - scoreAtWaveStart;

        // Give extra score proportional to the completed wave number
        int waveScoreReward = CalculateRewardForCompletingWave(waveNumber);
        waveScoreReward = Mathf.Min(waveScoreReward, 1000);

        ScoreManager.Instance.AddScore(waveScoreReward);

        UIManager.Instance.ShowWaveCompletedBanner(waveNumber, gainedThisWave, waveScoreReward);
    }

    private int CalculateRewardForCompletingWave(int waveNumber)
    {
        float raw = rewardCurve.rewardByWave.Evaluate(Mathf.Max(1, waveNumber));
        return Mathf.RoundToInt(raw);
    }

    /// <summary>
    /// Configure enemy categories and minimun cost
    /// </summary>
    private void CategorizePool()
    {
        cheapList.Clear();
        mediumList.Clear();
        expensiveList.Clear();
        minCost = int.MaxValue;

        foreach (EnemyData enemy in enemyPool)
        {
            int cost = enemy.spawnCost;
            minCost = Mathf.Min(minCost, cost);

            if (cost <= cheapMaxCost)
            {
                cheapList.Add(enemy);
            }
            else if (cost <= mediumMaxCost)
            {
                mediumList.Add(enemy);
            }
            else
            {
                expensiveList.Add(enemy);
            }
        }
    }

    /// <summary>
    /// Spawns a wave, first the budget is increased, then as many 
    /// enemies as possible are spawned, limited by budget.
    /// 3 categories of enemies: cheap, medium, expensive
    /// Each category takes a % of the wave
    /// </summary>
    private IEnumerator SpawnWave()
    {
        //TODO Animaciones, efectos, sonidos

        waitingForNextWaveToComplete = true;

        waveIndex++;

        scoreAtWaveStart = ScoreManager.Instance.CurrentScore;

        UIManager.Instance.ShowWaveStartedBanner(waveIndex);

        // Obtain budget: (initial + delta*n) * r^n
        float budget = (initialBudget + linearDelta * waveIndex)
                       * Mathf.Pow(exponentialRate, waveIndex);

        if (enableLimitBudget_TESTING)
        {
            budget = Mathf.Min(budget, limitBudget_TESTING);
        }

        Debug.Log($"[WaveManager] Wave {waveIndex}: Budget = {budget:F1}");

        while (budget >= minCost)
        {
            // Select a category
            float randomValue = UnityEngine.Random.value;
            List<EnemyData> poolCat;
            if (randomValue < pctCheap)
            {
                poolCat = cheapList;
            }
            else if (randomValue < pctCheap + pctMedium)
            {
                poolCat = mediumList;
            }
            else
            {
                poolCat = expensiveList;
            }

            // Filter enemies that fit within budget
            List<EnemyData> candidates = poolCat.FindAll(enemy => enemy.spawnCost <= budget);

            if (candidates.Count == 0)
            {
                // If it doesnt fit, try another
                candidates = new List<EnemyData>();
                foreach (List<EnemyData> alt in new[] { cheapList, mediumList, expensiveList })
                {
                    candidates.AddRange(alt.FindAll(enemy => enemy.spawnCost <= budget));
                }
                if (candidates.Count == 0)
                {
                    break;
                }
            }

            // Chose a random candidate
            EnemyData chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            // Spawn the candidate
            GameObject gameObject = null;
            AsyncOperationHandle<GameObject> handle = chosen.GetRandomPrefabReference().InstantiateAsync(spawnPoint.position, spawnPoint.rotation);
            
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                gameObject = handle.Result;
            }

            if (gameObject == null)
            {
                continue;
            }

            // Give handle to the unit so it frees it upon death
            if (gameObject.TryGetComponent<IAddressableInstance>(out IAddressableInstance addressable))
            {
                addressable.SetAddressableInstanceHandle(handle);
            }

            if (!gameObject.TryGetComponent<Enemy>(out Enemy enemy))
            {
                Debug.LogWarning($"{name}: failed to get {nameof(Enemy)} component from {enemy}");
                continue;
            }

            Transform crystal = GameObject.FindWithTag("Crystal").transform;
            enemy.Initialize(crystal, Enemy.Behaviour.Aggressive, spawnPoint.position);

            budget -= chosen.spawnCost;

            // Add a small delay so not all enemies spawn at the same instant
            yield return new WaitForSeconds(0.25f); 
        }

        yield return null;
    }
}