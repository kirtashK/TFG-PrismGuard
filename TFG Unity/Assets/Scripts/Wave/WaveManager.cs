using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyPoolEntry
{
    [Tooltip("Prefab que contiene el componente Enemy")]
    public GameObject prefab;
    [Tooltip("Datos de este tipo de enemigo")]
    public EnemyData data;
}

public class WaveManager : MonoBehaviour
{
    [Header("Pool de Enemigos")]

    [Tooltip("Todos los tipos de enemigos disponibles")]
    public List<EnemyPoolEntry> enemyPool = new();

    [Header("Categorización por Coste")]

    [Tooltip("Coste máximo para considerarlo 'Barato'")]
    public int cheapMaxCost = 5;
    [Tooltip("Coste máximo para considerarlo 'Medio' (>= cheapMaxCost)")]
    public int mediumMaxCost = 15;

    [Header("Distribución de la Ola")]

    [Range(0, 1)] 
    public float pctCheap = 0.6f;
    [Range(0, 1)] 
    public float pctMedium = 0.3f;
    [Range(0, 1)] 
    public float pctExpensive = 0.1f;

    [Header("Presupuesto de Oleadas")]

    [Tooltip("Presupuesto base para la ola 0")]
    public float initialBudget = 10f;
    [Tooltip("Incremento lineal por ola")]
    public float linearDelta = 5f;
    [Tooltip("Factor exponencial r (>1)")]
    public float exponentialRate = 1.02f;

    [Header("Spawn")]

    public Transform spawnPoint;
    [Tooltip("Segundos entre cada ola")]
    public float waveInterval = 100f;

    private bool waitingForNextWave;

    private int waveIndex = 0;

    private List<EnemyPoolEntry> cheapList = new();
    private List<EnemyPoolEntry> mediumList = new();
    private List<EnemyPoolEntry> expensiveList = new();
    private int minCost;

    private int scoreAtWaveStart;

    [SerializeField]
    private WaveRewardCurve rewardCurve;

    private void Start()
    {
        CategorizePool();
        StartCoroutine(RunWaves());
    }

    private void OnEnable()
    {
        // Suscribirse al evento de oleada completada de UIManager
        StartCoroutine(RegisterWhenUIManagerReady());
    }

    private void OnDisable()
    {
        // Limpiar suscripción si se destruye WaveManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
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
        // Primera ola inmediata para testing
        yield return SpawnWave();

        while (true)
        {
            // Si hay una oleada en marcha, no se activa el
            // temporizador para la siguiente oleada
            waitingForNextWave = true;
            yield return new WaitUntil(() => waitingForNextWave == false);

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
        // Cuando se lanze el evento (0 enemigos con vida)
        // se activa el temporizador para la siguiente oleada
        waitingForNextWave = false;

        // Puntuación obtenida en esta oleada:
        int gainedThisWave = ScoreManager.Instance.CurrentScore - scoreAtWaveStart;

        // Recompensar puntuación proporcional a la oleada completada:
        int waveScoreReward = CalculateRewardForCompletingWave(waveNumber);
        // Máximo 1000:
        waveScoreReward = Mathf.Min(waveScoreReward, 1000);

        ScoreManager.Instance.AddScore(waveScoreReward);

        // Mostramos banner con ola + puntuación ganada
        UIManager.Instance.ShowWaveCompletedBanner(waveNumber, gainedThisWave, waveScoreReward);
    }

    private int CalculateRewardForCompletingWave(int waveNumber)
    {
        float raw = rewardCurve.rewardByWave.Evaluate(Mathf.Max(1, waveNumber));
        return Mathf.RoundToInt(raw);
    }

    /// <summary>
    /// Configura las categorías de enemigos y el coste mínimo (enemigo mas barato)
    /// </summary>
    private void CategorizePool()
    {
        cheapList.Clear();
        mediumList.Clear();
        expensiveList.Clear();
        minCost = int.MaxValue;

        foreach (EnemyPoolEntry enemy in enemyPool)
        {
            int cost = enemy.data.spawnCost;
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
    /// Genera una oleada, primero se aumenta el presupuesto siguiendo una formula,
    /// se generan todos los enemigos que quepan con el presupuesto,
    /// hay tres categorias de enemigos, baratos, medios y carios, 
    /// cada categoría ocupa un % del spawn, de forma que se garantiza que cada categoría se generé si hay presupuesto
    /// </summary>
    private IEnumerator SpawnWave()
    {
        //TODO Animaciones, efectos, sonidos, mostrar en UI nueva oleada

        waveIndex++;

        scoreAtWaveStart = ScoreManager.Instance.CurrentScore;

        // Mostrar texto de nueva oleada
        UIManager.Instance.ShowWaveStartedBanner(waveIndex);

        // Calcula presupuesto: (initial + delta*n) * r^n
        float budget = (initialBudget + linearDelta * waveIndex)
                       * Mathf.Pow(exponentialRate, waveIndex);

        Debug.Log($"[WaveManager] Wave {waveIndex}: Budget = {budget:F1}");

        // Mientras quede presupuesto suficiente para el enemigo más barato
        while (budget >= minCost)
        {
            // Selecciona categoría según porcentajes
            float randomValue = Random.value;
            List<EnemyPoolEntry> poolCat;
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

            // Filtrar los enemigos que caben en el presupuesto
            List<EnemyPoolEntry> candidates = poolCat.FindAll(enemy => enemy.data.spawnCost <= budget);

            if (candidates.Count == 0)
            {
                // Si en esa categoría no cabe ninguno, intentar las otras
                candidates = new List<EnemyPoolEntry>();
                foreach (List<EnemyPoolEntry> alt in new[] { cheapList, mediumList, expensiveList })
                    candidates.AddRange(alt.FindAll(enemy => enemy.data.spawnCost <= budget));
                if (candidates.Count == 0)
                    break; // no cabe mas
            }

            // Elige uno al azar entre todos los candidatos
            EnemyPoolEntry chosen = candidates[Random.Range(0, candidates.Count)];

            // Spawnear el enemigo
            GameObject gameObject = Instantiate(chosen.prefab, spawnPoint.position, spawnPoint.rotation);
            Enemy enemy = gameObject.GetComponent<Enemy>();
            enemy.data = chosen.data;
            if (enemy.crystalTransform == null)
            {
                enemy.crystalTransform = GameObject.FindWithTag("Crystal")?.transform;
            }
            enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));

            budget -= chosen.data.spawnCost;

            // spawnear cada enemigo con una pausa pequeña
            yield return new WaitForSeconds(0.25f); 
        }

        yield return null;
    }
}
