using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Sensor;

public class AgentSoldier : Agent, IOrderable, IGuardable
{
    [HideInInspector] public Unit unit;
    private SoldierData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;

    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;
    [SerializeField] private StatKey detectionRadiusStat;

    [HideInInspector] public float detectionRadius;

    [Tooltip("Number of enemies to include in observations")]
    public int kNearest = 4;

    private float lastAttackTime = -Mathf.Infinity;

    [SerializeField] private Sensor enemySensor;
    [SerializeField] private Sensor allySensor;

    [Header("Rewards")]

    [Tooltip("Reward scale per 1 HP damage dealt")]
    public float rewardPerDamage = 0.01f;
    [Tooltip("Penalty scale per 1 HP received")]
    public float penaltyPerDamageTaken = 0.02f;

    [Tooltip("Bonus when an attack is successful")]
    public float rewardPerSuccessfulAttack = 0.01f;

    [Tooltip("Reward when slaying an enemy")]
    public float rewardOnKill = 1.0f;
    [Tooltip("Penalty when diying")]
    public float penaltyOnDeath = 1.0f;

    [Tooltip("Reward per HP healed")]
    public float rewardPerHealHP = 0.01f;
    [Tooltip("Penalty for wasting healing")]
    public float penaltyWastedHeal = 0.05f;

    [Header("Abilities")]
    public List<HeroAbility> abilities = new();
    public const int MaxAbilitySlots = 3;

    private float[] abilityCooldownTimers;

    private bool isPerformingAbility = false;

    private int pendingAbilitySlot = -1;
    private ITarget pendingAbilityTarget = null;

    private static readonly int AnimatorCastAbility = Animator.StringToHash("CastAbility");
    private static readonly int AnimatorAbilityIndex = Animator.StringToHash("AbilityIndex");

    private bool isMovingToGuardSpot;
    private bool hasGuardPoint;
    private Vector3 playerGuardPoint;

    [Header("Debug")]
    public bool drawGizmos = true;

    #region Unity methods

    protected override void Awake()
    {
        base.Awake();

        if (TryGetComponent<Unit>(out Unit unit))
        {
            this.unit = unit;
            data = (SoldierData)unit.unitData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(unit)}");
        }

        if (enemySensor == null)
        {
            Debug.LogError($"{name}: missing {nameof(enemySensor)}");
        }
        if (allySensor == null)
        {
            Debug.LogError($"{name}: missing {nameof(allySensor)}");
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

        abilityCooldownTimers = new float[MaxAbilitySlots];

        enemySensor.Initialize(unit.transform, unit.Faction, detectionRadius, SensorMode.Enemies);
        allySensor.Initialize(unit.transform, unit.Faction, detectionRadius, SensorMode.Allies);

        unit.agent.stoppingDistance = unit.attackRange;

        SetGuardPoint(unit.Position, true);
    }

    public override void Initialize()
    {
        
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (StatModifierManager.Instance == null)
        {
            yield return null;
        }

        unit.OnDamageTakenEvent += OnDamageTaken;
        unit.OnHealedEvent += OnHealed;

        unit.OnAbilityHitEvent += OnAbilityHit;
        unit.OnAbilityEndEvent += OnAbilityEnd;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        unit.OnDamageTakenEvent -= OnDamageTaken;
        unit.OnHealedEvent -= OnHealed;

        unit.OnAbilityHitEvent -= OnAbilityHit;
        unit.OnAbilityEndEvent -= OnAbilityEnd;
    }

    private void Update()
    {
        if (abilityCooldownTimers == null)
        {
            return;
        }

        for (int i = 0; i < abilityCooldownTimers.Length; i++)
        {
            if (abilityCooldownTimers[i] > 0f)
            {
                abilityCooldownTimers[i] -= Time.deltaTime;
            }
        }

        HandleGuardBehavior();
    }

    #endregion

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
        if (detectionRadiusStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(detectionRadiusStat)}");
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
            unit.moveSpeed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            unit.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            unit.healOnWaveCompleted = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackRangeStat, out finalValue))
        {
            unit.attackRange = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackDamageStat, out finalValue))
        {
            unit.attackDamage = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackCooldownStat, out finalValue))
        {
            unit.attackCooldown = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, detectionRadiusStat, out finalValue))
        {
            detectionRadius = finalValue;
            enemySensor.SetRadius(detectionRadius);
            allySensor.SetRadius(detectionRadius);
        }
    }

    #endregion

    #region ML Agent

    public override void OnEpisodeBegin()
    {
        if (TrainingManager.Instance == null)
        {
            return;
        }

        unit.currentHealth = unit.maxHealth;
        lastAttackTime = -Mathf.Infinity;

        if (abilityCooldownTimers != null)
        {
            for (int i = 0; i < abilityCooldownTimers.Length; i++)
            {
                abilityCooldownTimers[i] = 0f;
            }
        }

        ClearPendingAbility();
        ClearGuardPoint();
        SetGuardPoint(unit.Position, true);

        if (unit.agent != null)
        {
            unit.agent.ResetPath();
            unit.agent.isStopped = false;
        }
    }

    public override void CollectObservations(VectorSensor vectorSensor)
    {
        vectorSensor.AddObservation(Mathf.Clamp01(unit.currentHealth / unit.maxHealth));

        vectorSensor.AddObservation(GetAttackCooldownNormalized());

        List<ITarget> nearbyEnemies = GetNearbyEnemies();
        vectorSensor.AddObservation(Mathf.Clamp01((float)nearbyEnemies.Count / (float)kNearest));

        // kNearest * 3: per-enemy local position + distance
        for (int i = 0; i < kNearest; i++)
        {
            if (i < nearbyEnemies.Count)
            {
                Vector3 local = transform.InverseTransformPoint(nearbyEnemies[i].Position);

                vectorSensor.AddObservation(Mathf.Clamp(local.x / detectionRadius, -1f, 1f));
                vectorSensor.AddObservation(Mathf.Clamp(local.z / detectionRadius, -1f, 1f));
                vectorSensor.AddObservation(Mathf.Clamp01(local.magnitude / detectionRadius));
            }
            else
            {
                vectorSensor.AddObservation(0f);
                vectorSensor.AddObservation(0f);
                vectorSensor.AddObservation(0f);
            }
        }

        // MaxAbilitySlots * (1 + ObservationsPerSlot): cooldown + context per ability
        for (int i = 0; i < MaxAbilitySlots; i++)
        {
            if (i < abilities.Count && abilities[i] != null)
            {
                float abilityCooldownNorm = Mathf.Clamp01(abilityCooldownTimers[i] / abilities[i].cooldown);
                vectorSensor.AddObservation(abilityCooldownNorm);
                abilities[i].CollectObservations(this, vectorSensor);
            }
            else
            {
                for (int j = 0; j < 1 + HeroAbility.ObservationsPerSlot; j++)
                {
                    vectorSensor.AddObservation(0f);
                }
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete branches:
        // Branch 0 = targetIndex [0..kNearest], kNearest means no target
        // Branch 1 = action type [0 = nothing, 1 = attack, 2 = ability0, 3 = ability1, 4 = ability2]

        if (!unit.IsAlive || isPerformingAbility)
        {
            return;
        }

        ActionSegment<int> discreteActions = actionBuffers.DiscreteActions;
        int targetIndex = Mathf.Clamp(discreteActions[0], 0, kNearest);
        int actionType = Mathf.Clamp(discreteActions[1], 0, 1 + MaxAbilitySlots);

        List<ITarget> nearbyEnemies = GetNearbyEnemies();
        ITarget selectedTarget = (targetIndex < nearbyEnemies.Count) 
            ? nearbyEnemies[targetIndex] : null;

        switch (actionType)
        {
            case 1:
                if (selectedTarget != null)
                {
                    TryAttack(selectedTarget);
                    SetDestination(unit.GetTargetAttackPosition(selectedTarget));
                }
                break;

            default:
                TryUseAbility(actionType - 2, selectedTarget);
                break;
        }

        // Encourage movement efficiency
        AddReward(-1f / 2000f);
    }

    // Heuristic for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (!unit.IsAlive)
        {
            return;
        }

        ActionSegment<int> discreteOut = actionsOut.DiscreteActions;
        List<ITarget> enemies = GetNearbyEnemies();

        if (enemies.Count == 0)
        {
            discreteOut[0] = kNearest;
            discreteOut[1] = 0;
            return;
        }

        discreteOut[0] = 0;

        ITarget closestEnemy = enemies[0];

        int usableAbilityIndex = GetUsableAbility(closestEnemy);
        if (usableAbilityIndex >= 0)
        {
            discreteOut[1] = usableAbilityIndex + 2;
            return;
        }

        discreteOut[1] = 1;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        List<ITarget> enemies = GetNearbyEnemies();
        ITarget firstTarget = enemies.Count > 0 ? enemies[0] : null;

        if (enemies.Count == 0)
        {
            actionMask.SetActionEnabled(1, 1, false);
        }

        for (int i = 0; i < MaxAbilitySlots; i++)
        {
            bool hasAbility = i < abilities.Count && abilities[i] != null;
            bool offCooldown = hasAbility && abilityCooldownTimers[i] <= 0f;
            bool usable = offCooldown && abilities[i].IsUsable(this, firstTarget);

            if (!usable)
            {
                actionMask.SetActionEnabled(1, i + 2, false);
            }
        }
    }

    #endregion

    #region Combat

    /// <summary>
    /// Returns ITargets inside aggroRadius, ordered by distance ascending
    /// </summary>
    public List<ITarget> GetNearbyEnemies()
    {
        if (enemySensor == null)
        {
            return new List<ITarget>();
        }

        return enemySensor.GetSortedTargets(transform.position, prioritizeUnits: true);
    }

    public List<ITarget> GetNearbyAllies()
    {
        if (allySensor == null)
        {
            return new List<ITarget>();
        }

        return allySensor.GetSortedTargets(transform.position);
    }


    /// <summary>
    /// Try to perform an attack on the target
    /// Returns true if an attack was performed
    /// </summary>
    public bool TryAttack(ITarget target)
    {
        if (!target.IsAlive || target == null || !unit.IsAlive)
        {
            return false;
        }

        // Cooldown
        if (Time.time < lastAttackTime + unit.attackCooldown)
        {
            return false;
        }

        // Range
        float distSqr = (unit.GetTargetAttackPosition(target) - unit.Position).sqrMagnitude;
        if (distSqr > unit.attackRange * unit.attackRange)
        {
            return false;
        }

        unit.FaceTarget(target.Position, 720f);
        unit.Attack(target);

        float reward = rewardPerSuccessfulAttack + unit.attackDamage * rewardPerDamage;

        if (!target.IsAlive)
        {
            reward += rewardOnKill;
        }

        AddReward(reward);

        lastAttackTime = Time.time;
        return true;
    }

    private void OnDamageTaken(float amount, Vector3 attackOrigin)
    {
        AddReward(-amount * penaltyPerDamageTaken);

        if (!unit.IsAlive)
        {
            ClearPendingAbility();
            AddReward(-penaltyOnDeath);

            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.NotifyAgentDied(this);
            }

            EndEpisode();
        }
    }

    private void OnHealed(float amount)
    {

    }

    /// <summary>
    /// Returns normalized cooldown remaining in [0,1]
    /// </summary>
    public float GetAttackCooldownNormalized()
    {
        if (unit.attackCooldown <= 0f)
        {
            return 0f;
        }

        float elapsed = Time.time - lastAttackTime;
        return Mathf.Clamp01(1f - (elapsed / unit.attackCooldown));
    }

    private void TryUseAbility(int slotIndex, ITarget target)
    {
        if (slotIndex < 0 || slotIndex >= abilities.Count)
        {
            return;
        }

        HeroAbility ability = abilities[slotIndex];
        if (ability == null
            || abilityCooldownTimers[slotIndex] > 0f
            || !ability.IsUsable(this, target))
        {
            return;
        }

        pendingAbilitySlot = slotIndex;
        pendingAbilityTarget = target;
        isPerformingAbility = true;

        if (target != null)
        {
            unit.FaceTarget(target.Position, 720f);
        }

        if (unit.animator != null)
        {
            unit.animator.SetInteger(AnimatorAbilityIndex, slotIndex);
            unit.animator.SetTrigger(AnimatorCastAbility);
        }
        else
        {
            ExecutePendingAbility();
            ClearPendingAbility();
        }
    }

    private int GetUsableAbility(ITarget target)
    {
        for (int i = 0; i < abilities.Count && i < MaxAbilitySlots; i++)
        {
            if (abilities[i] == null
                || abilityCooldownTimers[i] > 0f
                || !abilities[i].IsUsable(this, target))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    #endregion

    #region Guard & Orders

    public void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options)
    {
        SetGuardPoint(destination, options.returnToGuard);
    }

    public void SetGuardPoint(Vector3 guardPosition, bool returnToGuard)
    {
        playerGuardPoint = guardPosition;
        hasGuardPoint = true;
        isMovingToGuardSpot = false;
    }

    public void ClearGuardPoint()
    {
        isMovingToGuardSpot = false;
        hasGuardPoint = false;

        Stop();
    }

    private void HandleGuardBehavior()
    {
        if (!unit.IsAlive)
        {
            return;
        }

        List<ITarget> nearbyEnemies = GetNearbyEnemies();
        if (nearbyEnemies.Count > 0)
        {
            return;
        }

        if (hasGuardPoint)
        {
            if (HasReachedGuardPoint())
            {
                if (isMovingToGuardSpot)
                {
                    isMovingToGuardSpot = false;
                    Stop();              
                }
            }
            else
            {
                if (!isMovingToGuardSpot)
                {
                    isMovingToGuardSpot = true;
                    SetDestination(playerGuardPoint);
                }
            }
        }
    }

    private bool HasReachedGuardPoint()
    {
        if (unit == null)
        {
            return false;
        }

        float arrivalThreshold = unit.agent != null
            ? Mathf.Max(unit.agent.stoppingDistance + 0.25f, 0.5f)
            : 0.5f;

        float distanceSqr = (unit.Position - playerGuardPoint).sqrMagnitude;
        return distanceSqr <= arrivalThreshold * arrivalThreshold;
    }

    #endregion

    #region Movement

    public void SetDestination(Vector3 worldPosition)
    {
        if (unit.agent == null)
        {
            return;
        }

        unit.agent.isStopped = false;
        unit.agent.SetDestination(worldPosition);
    }

    public void Stop()
    {
        if (unit.agent == null)
        {
            return;
        }

        unit.agent.isStopped = true;
        unit.agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (unit.agent == null)
        {
            return true;
        }

        if (!unit.agent.hasPath)
        {
            return true;
        }

        if (unit.agent.pathPending)
        {
            return false;
        }

        if (unit.agent.remainingDistance <= Mathf.Max(unit.agent.stoppingDistance))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Animations

    private void ExecutePendingAbility()
    {
        if (pendingAbilitySlot < 0 
            || pendingAbilitySlot >= abilities.Count)
        {
            ClearPendingAbility();
            return;
        }

        HeroAbility ability = abilities[pendingAbilitySlot];
        if (ability == null)
        {
            ClearPendingAbility();
            return;
        }

        if (!ability.IsUsable(this, pendingAbilityTarget))
        {
            return;
        }

        AbilityResult result = ability.Execute(this, pendingAbilityTarget);

        if (result.wasExecuted)
        {
            abilityCooldownTimers[pendingAbilitySlot] = ability.cooldown;
            AddReward(ability.CalculateReward(this, result));
        }
    }

    private void ClearPendingAbility()
    {
        pendingAbilitySlot = -1;
        pendingAbilityTarget = null;
        isPerformingAbility = false;
    }

    private void OnAbilityHit()
    {
        ExecutePendingAbility();
    }

    private void OnAbilityEnd()
    {
        ClearPendingAbility();
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (abilities == null)
        {
            return;
        }

        foreach (HeroAbility ability in abilities)
        {
            if (ability is AoEAttackAbility aoeAttack)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.65f);
                Gizmos.DrawWireSphere(transform.position, aoeAttack.aoeRadius);
            }
            if (ability is AoEHealAbility aoeHeal)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.65f);
                Gizmos.DrawWireSphere(transform.position, aoeHeal.aoeRadius);
            }
        }        
    }
#endif
}