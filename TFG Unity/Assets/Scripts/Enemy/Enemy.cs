using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, ICombatTarget
{
    public EnemyData data;
    public Transform crystalTransform;

    private float currentHealth;

    [HideInInspector] 
    public NavMeshAgent agent;

    private IEnemyState currentState;

    public ICombatTarget MainTarget { get; private set; }

    private readonly int maxColliders = 10;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        MainTarget = crystalTransform != null
            ? crystalTransform.GetComponent<ICombatTarget>()
            : GameObject.FindWithTag("Crystal")
                .GetComponent<ICombatTarget>();
    }

    private void Start()
    {
        UIManager.Instance.ChangeEnemyCount(1);

        currentHealth = data.maxHealth;

        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;

        ChangeState(new EnemyChaseState(MainTarget));
    }

    private void Update()
    {
        currentState.UpdateState(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Salud de {name} = {currentHealth}/{data.maxHealth}");

        if (currentHealth == 0f)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{name} ha muerto");

        // Quitar 1 al contador de enemigos
        UIManager.Instance.ChangeEnemyCount(-1);

        // Añadir puntuación al derrotar el enemigo:
        ScoreManager.Instance.AddScore(data.spawnCost);

        // TODO Sonido, animaciones, efectos, quizas recompensas?
        Destroy(gameObject);
    }

    /// <summary>
    /// Busca la unidad del jugador viva mas cercana dentro de data.AggroRadius
    /// </summary>
    public ICombatTarget FindNearestPlayerUnit()
    {
        Collider[] aggroBuffer = new Collider[maxColliders];

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            data.AggroRadius,
            aggroBuffer,
            LayerMask.GetMask("PlayerUnit")
        );

        ICombatTarget best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ICombatTarget playerUnit = aggroBuffer[i].GetComponent<ICombatTarget>();
            if (playerUnit != null 
                && playerUnit.IsAlive 
                && playerUnit is not Crystal)
            {
                float dist = Vector3.Distance(transform.position, playerUnit.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = playerUnit;
                }
            }
        }
        return best;
    }
}
