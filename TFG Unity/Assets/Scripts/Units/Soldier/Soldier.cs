using System.Collections;
using UnityEngine;

public class Soldier : MonoBehaviour, IOrderable, IGuardable, IAttackMovable, IStatRefresher
{
    [HideInInspector] public Unit unit;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;

    [HideInInspector] public float aggroRadius;

    private SoldierData data;

    private ISoldierState currentState;

    // IGuardable
    private Vector3 guardPoint;
    private bool hasGuardPoint = false;
    private bool returnToGuardOnFinish = false;
    private bool attackMove = false;

    private void Awake()
    {
        if (TryGetComponent<Unit>(out Unit unit))
        {
            this.unit = unit;
            data = (SoldierData)unit.unitData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(unit)}");
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

        unit.currentHealth = unit.maxHealth;

        aggroRadius = data.AggroRadius;

        ChangeState(new SoldierIdleState());
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
        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }
    }

    #region Stats

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (healOnWaveCompletedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(healOnWaveCompletedStat)}");
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
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            unit.healOnWaveCompleted = finalValue;
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

    #endregion

    public void ChangeState(ISoldierState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options)
    {
        if (!unit.IsAlive)
        {
            return;
        }

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

    /// <summary>
    /// Finds the closest enemy unit/structure inside aggroRadius
    /// </summary>
    /// <returns></returns>
    public ITarget FindNearestEnemyTarget()
    {
        return TargetSearchUtility.SearchTarget(transform.position, aggroRadius,
            Faction.Player);
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
