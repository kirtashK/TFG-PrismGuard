using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Soldier : MonoBehaviour, IAddressableInstance, ICombatTarget, 
                    IOrderable, IGuardable, IAttackMovable, IStatRefresher
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

    public SoldierData data;

    [HideInInspector]
    public NavMeshAgent agent;

    private ISoldierState currentState;

    // IGuardable
    private Vector3 guardPoint;
    private bool hasGuardPoint = false;
    private bool returnToGuardOnFinish = false;
    private bool attackMove = false;

    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        CheckNullStats();
    }

    private void Start()
    {
        RefreshStats();

        currentHealth = maxHealth;

        aggroRadius = data.AggroRadius;

        ChangeState(new SoldierIdleState());
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
        while (UIManager.Instance == null 
            || StatModifierManager.Instance == null)
        {
            yield return null;
        }

        UIManager.Instance.OnWaveCompleted += OnWaveCompleted;
        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
        StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
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

    public void ChangeState(ISoldierState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public Vector3 Position => transform.position;

    public bool isAlive => currentHealth > 0f;

    /// <summary>
    /// Soldier takes damage, if hp is 0 or lower, soldier dies
    /// </summary>
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

    private void Die()
    {
        Debug.Log($"{name} has died");

        // Release addressable handle
        if (hasAddressableHandle && addressableInstanceHandle.IsValid())
        {
            Addressables.ReleaseInstance(addressableInstanceHandle);
            hasAddressableHandle = false;
            return;
        }

        Destroy(gameObject);
    }

    private void OnWaveCompleted(int waveNumber)
    {
        RegenerateHealth(0.25f);
    }

    /// <summary>
    /// Regenerates a percentage of the maximun health
    /// </summary>
    public void RegenerateHealth(float percent)
    {
        if (!isAlive || currentHealth >= maxHealth)
        {
            return;
        }

        float amountToRegenerate = maxHealth * percent;
        currentHealth = Mathf.Min(currentHealth + amountToRegenerate, maxHealth);

        Debug.Log($"{name} regenerated {amountToRegenerate} health (now {currentHealth}/{maxHealth})");
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
    }

    public void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options)
    {
        attackMove = options.attackMove;

        // Set guard point if required, otherwise clear it
        if (options.returnToGuard)
        {
            SetGuardPoint(destination, true);
        }
        else
        {
            ClearGuardPoint();
        }


        ChangeState(new SoldierMoveState(destination, options.attackMove, options.returnToGuard));
    }

    public void SetGuardPoint(Vector3 point, bool returnToGuard)
    {
        hasGuardPoint = true;
        guardPoint = point;
        returnToGuardOnFinish = returnToGuard;
    }

    public void ClearGuardPoint()
    {
        hasGuardPoint = false;
        returnToGuardOnFinish = false;
    }

    public void SetAttackMove(bool toggle)
    {
        attackMove = toggle;
    }

    // Called by states when combat finishes or target lost
    public void HandleCombatEnd()
    {
        if (returnToGuardOnFinish && hasGuardPoint)
        {
            // Return to guard point
            ChangeState(new SoldierMoveState(guardPoint, true, false));
        }
        else
        {
            // No guard, just go idle
            ChangeState(new SoldierIdleState());
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        if (hasGuardPoint)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(guardPoint, 0.15f);
        }
    }
}
