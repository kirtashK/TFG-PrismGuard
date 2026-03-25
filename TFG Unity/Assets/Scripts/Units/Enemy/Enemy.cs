using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IStatRefresher
{
    [HideInInspector] public Unit unit;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;

    [HideInInspector] public float aggroRadius;

    [HideInInspector] public float guardRadius;
    [HideInInspector] public float guardChaseBuffer;
    [HideInInspector] public float patrolDelayMin;
    [HideInInspector] public float patrolDelayMax;

    private EnemyData data;
    public Transform crystalTransform;

    private IEnemyState currentState;

    public ICombatTarget MainTarget { get; private set; }

    private readonly Collider[] aggroBuffer = new Collider[16];

    public enum Behaviour
    {
        Aggressive,
        Guard
    }

    public Behaviour behaviour = Behaviour.Aggressive;

    public Vector3 homePosition;

    private void Awake()
    {
        if (TryGetComponent<Unit>(out Unit unit))
        {
            this.unit = unit;
            data = (EnemyData)unit.unitData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(unit)}");
        }

        SetMainTarget();

        CheckNullStats();

        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }
    }

    private void SetMainTarget()
    {
        if (crystalTransform != null)
        {
            MainTarget = crystalTransform.GetComponent<ICombatTarget>();
        }
        else
        {
            GameObject crystalObject = GameObject.FindWithTag("Crystal");
            if (crystalObject != null)
            {
                MainTarget = crystalObject.GetComponent<ICombatTarget>();
            }
            else
            {
                MainTarget = null;
            }
        }
    }

    private void Start()
    {
        RefreshStats();

        unit.currentHealth = unit.maxHealth;

        aggroRadius = data.AggroRadius;

        guardRadius = data.guardRadius;
        guardChaseBuffer = data.guardChaseBuffer;
        patrolDelayMin = data.patrolDelayMin;
        patrolDelayMax = data.patrolDelayMax;

        Initialize(crystalTransform, Behaviour.Guard);
    }

    private void Update()
    {
        if (unit.IsAlive)
        {
            currentState?.UpdateState(this);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (StatModifierManager.Instance == null)
        {
            yield return null;
        }

        unit.OnDeathStartedEvent += OnDeathStarted;
        unit.OnDeathCleanupEvent += OnDeathCleanup;
        unit.OnDamageTakenEvent += OnDamageTaken;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        unit.OnDeathStartedEvent -= OnDeathStarted;
        unit.OnDeathCleanupEvent -= OnDeathCleanup;
        unit.OnDamageTakenEvent -= OnDamageTaken;
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (moveSpeedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(moveSpeedStat)}");
        }
        if (attackRangeStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(attackRangeStat)}");
        }
        if (attackDamageStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(attackDamageStat)}");
        }
        if (attackCooldownStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(attackCooldownStat)}");
        }
    }

    void HandleModifiersChanged(string targetId, string statKeyId)
    {
        if (targetId == data.id)
        {
            RefreshStats();
        }
        // Global modifier:
        else if (string.IsNullOrEmpty(targetId))
        {
            RefreshStats();
        }
    }

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, moveSpeedStat, out float finalValue))
        {
            unit.moveSpeed = unit.agent.speed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            unit.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackRangeStat, out finalValue))
        {
            unit.attackRange = unit.agent.stoppingDistance = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackDamageStat, out finalValue))
        {
            unit.attackDamage = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackCooldownStat, out finalValue))
        {
            unit.attackCooldown = finalValue;
        }
    }

    public void Initialize(Transform crystal, Behaviour behaviour, Vector3? homeOverride = null)
    {
        if (crystal != null)
        {
            crystalTransform = crystal;
            MainTarget = crystalTransform.GetComponent<ICombatTarget>();
        }

        this.behaviour = behaviour;

        if (homeOverride.HasValue)
        {
            homePosition = homeOverride.Value;
        }
        else
        {
            homePosition = transform.position;
        }

        if (this.behaviour == Behaviour.Guard)
        {
            ChangeState(new EnemyGuardState(homePosition, guardRadius, guardChaseBuffer));
        }
        else
        {
            if (MainTarget != null)
            {
                UIManager.Instance.ChangeEnemyCount(1);
                ChangeState(new EnemyChaseState(MainTarget));
            }
            else
            {
                Debug.LogWarning($"{name}: Failed to initialize as {nameof(Behaviour.Aggressive)}");
                ChangeState(new EnemyIdleState());
            }
        }
    }


    private void OnDamageTaken(float amount, Vector3 attackOrigin)
    {
        if (unit.IsAlive && behaviour == Behaviour.Guard)
        {
            behaviour = Behaviour.Aggressive;
            UIManager.Instance.ChangeEnemyCount(1);
            ChangeState(new EnemyChaseState(MainTarget));
        }
    }

    private void OnDeathStarted()
    {
        ScoreManager.Instance.AddScore(data.spawnCost);
        UIManager.Instance.ChangeEnemyCount(-1);
    }

    private void OnDeathCleanup()
    {

    }

    /// <summary>
    /// Finds the closests player unit inside aggroRadius
    /// </summary>
    public ICombatTarget FindNearestPlayerUnit()
    {
        int hitCount = Physics.OverlapSphereNonAlloc
            (transform.position,
            aggroRadius,
            aggroBuffer,
            LayerMask.GetMask("PlayerUnit"));

        ICombatTarget best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            ICombatTarget playerUnit = aggroBuffer[i].GetComponentInParent<ICombatTarget>();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        if (behaviour == Behaviour.Guard)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(homePosition, guardRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(homePosition, guardRadius + guardChaseBuffer);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(homePosition, 0.15f);
        }
    }
}