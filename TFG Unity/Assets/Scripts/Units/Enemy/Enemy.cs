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

    public EnemyData data;
    public Transform crystalTransform;

    [HideInInspector] 
    public NavMeshAgent agent;

    private IEnemyState currentState;

    public ICombatTarget MainTarget { get; private set; }

    private readonly Collider[] aggroBuffer = new Collider[16];

    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        MainTarget = crystalTransform != null
            ? crystalTransform.GetComponent<ICombatTarget>()
            : GameObject.FindWithTag("Crystal")
                .GetComponent<ICombatTarget>();

        CheckNullStats();
    }

    private void Start()
    {
        UIManager.Instance.ChangeEnemyCount(1);

        RefreshStats();

        currentHealth = maxHealth;

        ChangeState(new EnemyChaseState(MainTarget));
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

    public Vector3 Position => transform.position;

    public bool isAlive => currentHealth > 0f;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"{name} took {amount} damage" +
            $"\nHealth of {name}: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
    }

    private void Die()
    {
        //Debug.Log($"{name} has died");

        ScoreManager.Instance.AddScore(data.spawnCost);

        UIManager.Instance.ChangeEnemyCount(-1);

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

    /// <summary>
    /// Finds the closests player unit inside data.AggroRadius
    /// </summary>
    public ICombatTarget FindNearestPlayerUnit()
    {
        int hitCount = Physics.OverlapSphereNonAlloc
            (transform.position,
            data.AggroRadius,
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
}