using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IAddressableInstance, ICombatTarget
{
    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;

    private float currentHealth;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackCooldown;

    [HideInInspector] public float aggroRadius;

    [HideInInspector] public float guardRadius;
    [HideInInspector] public float guardChaseBuffer;
    [HideInInspector] public float patrolDelayMin;
    [HideInInspector] public float patrolDelayMax;

    public EnemyData data;
    public Transform crystalTransform;

    [HideInInspector] 
    public NavMeshAgent agent;

    private IEnemyState currentState;

    public ICombatTarget MainTarget { get; private set; }

    private readonly Collider[] aggroBuffer = new Collider[16];

    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    public enum Behaviour
    {
        Aggressive,
        Guard
    }

    public Behaviour behaviour = Behaviour.Aggressive;

    public Vector3 homePosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

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

        CheckNullStats();

        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }
    }

    private void Start()
    {
        RefreshStats();

        currentHealth = maxHealth;

        aggroRadius = data.AggroRadius;

        guardRadius = data.guardRadius;
        guardChaseBuffer = data.guardChaseBuffer;
        patrolDelayMin = data.patrolDelayMin;
        patrolDelayMax = data.patrolDelayMax;

        Initialize(crystalTransform, Behaviour.Guard);
    }

    private void Update()
    {
        currentState?.UpdateState(this);
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

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
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
            moveSpeed = agent.speed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackRangeStat, out finalValue))
        {
            attackRange = agent.stoppingDistance = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackDamageStat, out finalValue))
        {
            attackDamage = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackCooldownStat, out finalValue))
        {
            attackCooldown = finalValue;
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

    public Vector3 Position => transform.position;

    public bool isAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"{name} took {amount} damage" +
            $"\nHealth of {name}: {currentHealth}/{maxHealth}");

        if (behaviour == Behaviour.Guard)
        {
            behaviour = Behaviour.Aggressive;
            UIManager.Instance.ChangeEnemyCount(1);
            ChangeState(new EnemyChaseState(MainTarget));
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        //Debug.Log($"{name} has died");

        if (behaviour == Behaviour.Aggressive)
        {
            ScoreManager.Instance.AddScore(data.spawnCost);
            UIManager.Instance.ChangeEnemyCount(-1);
        }

        // TODO Sonido, animaciones, efectos

        // Release addressable handle
        if (hasAddressableHandle  && addressableInstanceHandle.IsValid())
        {
            Addressables.ReleaseInstance(addressableInstanceHandle);
            hasAddressableHandle = false;
            return;
        }

        Destroy(gameObject);
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
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
                && playerUnit.isAlive 
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