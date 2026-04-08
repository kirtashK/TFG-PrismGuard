using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class AgentSoldier : Agent
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

    private Sensor sensor;

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

        sensor = GetComponentInChildren<Sensor>(true);
        if (sensor == null)
        {
            Debug.LogError($"{name}: missing {nameof(Sensor)}");
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

        sensor.Initialize(unit.transform, unit.Faction, detectionRadius);

        unit.agent.stoppingDistance = unit.attackRange;
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

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    protected override void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        unit.OnDamageTakenEvent -= OnDamageTaken;
        unit.OnHealedEvent -= OnHealed;
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
        }
    }

    #endregion

    #region ML Agent

    public override void CollectObservations(VectorSensor vectorSensor)
    {

        float healthNorm = Mathf.Clamp01(unit.currentHealth / Mathf.Max(0.0001f, unit.maxHealth));
        vectorSensor.AddObservation(healthNorm);

        float cooldownNorm = GetAttackCooldownNormalized();
        vectorSensor.AddObservation(cooldownNorm);

        List<ITarget> nearbyEnemies = GetNearbyEnemies();
        float numNorm = Mathf.Clamp01((float)nearbyEnemies.Count / (float)kNearest);
        vectorSensor.AddObservation(numNorm);

        // Local x, local z normalized
        for (int i = 0; i < kNearest; i++)
        {
            if (i < nearbyEnemies.Count)
            {
                Vector3 worldPos = nearbyEnemies[i].Position;
                Vector3 local = transform.InverseTransformPoint(worldPos);
                float nx = Mathf.Clamp(local.x / detectionRadius, -1f, 1f);
                float nz = Mathf.Clamp(local.z / detectionRadius, -1f, 1f);

                vectorSensor.AddObservation(nx);
                vectorSensor.AddObservation(nz);

                float dist = Mathf.Clamp01(local.magnitude / detectionRadius);
                vectorSensor.AddObservation(dist);
            }
            else
            {
                vectorSensor.AddObservation(0f);
                vectorSensor.AddObservation(0f);
                vectorSensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete branches:
        // Branch 0 = targetIndex [0..kNearest] kNearest means no target
        // Branch 1 = attackFlag [0..1]

        if (!unit.IsAlive)
        {
            return;
        }

        ActionSegment<int> discreteActions = actionBuffers.DiscreteActions;
        int targetIndex = Mathf.Clamp(discreteActions[0], 0, kNearest);
        int attackFlag = Mathf.Clamp(discreteActions[1], 0, 1);

        List<ITarget> nearbyEnemies = GetNearbyEnemies();

        if (targetIndex >= 0 && targetIndex < nearbyEnemies.Count)
        {
            Vector3 dest = unit.GetTargetAttackPosition(nearbyEnemies[targetIndex]);
            SetDestination(dest);
        }
        else
        {
            Stop();
        }

        if (attackFlag == 1 && targetIndex >= 0 && targetIndex < nearbyEnemies.Count)
        {
            TryAttack(nearbyEnemies[targetIndex]);
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

        if (enemies.Count > 0)
        {
            discreteOut[0] = 0;
            float dist = Vector3.Distance(transform.position, enemies[0].Position);
            discreteOut[1] = (dist <= unit.attackRange) ? 1 : 0;
        }
        else
        {
            discreteOut[0] = kNearest;
            discreteOut[1] = 0;
        }
    }

    #endregion

    #region Combat

    /// <summary>
    /// Returns ITargets inside aggroRadius, ordered by distance ascending
    /// </summary>
    private List<ITarget> GetNearbyEnemies()
    {
        if (sensor == null)
        {
            return new List<ITarget>();
        }

        return sensor.GetSortedTargets(transform.position, prioritizeUnits: true);
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

        float reward = 0;
        reward += rewardPerSuccessfulAttack;

        reward += unit.attackDamage * rewardPerDamage;

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
            AddReward(-penaltyOnDeath);
            EndEpisode();
        }
    }

    private void OnHealed(float amount)
    {
        
    }

    public void Heal(float amount)
    {
        float before = unit.currentHealth;

        unit.Heal(amount);
        AddReward(amount * rewardPerHealHP);

        if (before / unit.maxHealth > 0.9f)
        {
            AddReward(-penaltyWastedHeal);
        }
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
        float remaining = Mathf.Clamp01(1f - (elapsed / unit.attackCooldown));

        return remaining;
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

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}