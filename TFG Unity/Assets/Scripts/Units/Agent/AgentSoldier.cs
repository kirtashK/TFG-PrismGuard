using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(AgentCombat))]
[RequireComponent(typeof(AgentMovementController))]
public class AgentSoldier : Agent
{
    [Header("Data")]
    public SoldierData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;

    public float currentHealth;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackCooldown;

    [Header("Movement")]
    public AgentMovementController movement;

    [Header("Combat")]
    public AgentCombat combat;

    [Tooltip("Number of enemies to include in observations")]
    public int kNearest = 4;

    public string enemyLayer = "EnemyUnit";

    private static readonly Collider[] agroBuffer = new Collider[16];

    [Header("Debug")]
    public bool drawGizmos = true;

    protected override void Awake()
    {
        base.Awake();

        if (movement == null)
        {
            movement = GetComponent<AgentMovementController>();
        }
        if (combat == null)
        {
            combat = GetComponent<AgentCombat>();
        }

        CheckNullStats();
    }

    private void Start()
    {
        RefreshStats();

        currentHealth = maxHealth;
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

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    protected override void OnDisable()
    {
        StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
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
            moveSpeed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackRangeStat, out finalValue))
        {
            attackRange = finalValue;
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

    public override void CollectObservations(VectorSensor sensor)
    {
        float healthNorm = Mathf.Clamp01(currentHealth / Mathf.Max(0.0001f, maxHealth));
        sensor.AddObservation(healthNorm);

        float cooldownNorm = combat.GetAttackCooldownNormalized();
        sensor.AddObservation(cooldownNorm);

        List<ICombatTarget> nearbyEnemies = GetNearestEnemies(transform.position, data.AggroRadius, kNearest, enemyLayer);
        float numNorm = Mathf.Clamp01((float)nearbyEnemies.Count / (float)kNearest);
        sensor.AddObservation(numNorm);

        // Local x, local z normalized
        for (int i = 0; i < kNearest; i++)
        {
            if (i < nearbyEnemies.Count)
            {
                Vector3 worldPos = nearbyEnemies[i].Position;
                Vector3 local = transform.InverseTransformPoint(worldPos);
                float nx = Mathf.Clamp(local.x / data.AggroRadius, -1f, 1f);
                float nz = Mathf.Clamp(local.z / data.AggroRadius, -1f, 1f);

                sensor.AddObservation(nx);
                sensor.AddObservation(nz);

                float dist = Mathf.Clamp01(local.magnitude / data.AggroRadius);
                sensor.AddObservation(dist);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete branches:
        // Branch 0 = targetIndex [0..kNearest] kNearest means no target
        // Branch 1 = attackFlag [0..1]

        ActionSegment<int> discreteActions = actionBuffers.DiscreteActions;
        int targetIndex = Mathf.Clamp(discreteActions[0], 0, kNearest);
        int attackFlag = Mathf.Clamp(discreteActions[1], 0, 1);

        List<ICombatTarget> nearbyEnemies = GetNearestEnemies(transform.position, data.AggroRadius, kNearest, enemyLayer);

        if (targetIndex >= 0 && targetIndex < nearbyEnemies.Count)
        {
            Vector3 dest = nearbyEnemies[targetIndex].Position;
            movement.SetDestination(dest);
        }
        else
        {
            movement.Stop();
        }

        if (attackFlag == 1 && targetIndex >= 0 && targetIndex < nearbyEnemies.Count)
        {
            combat.TryAttack(nearbyEnemies[targetIndex]);
        }

        // Encourage movement efficiency
        AddReward(-1f / 2000f);
    }

    // Heuristic for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteOut = actionsOut.DiscreteActions;
        List<ICombatTarget> enemies = GetNearestEnemies(transform.position, data.AggroRadius, kNearest, enemyLayer);

        if (enemies.Count > 0)
        {
            discreteOut[0] = 0;
            float dist = Vector3.Distance(transform.position, enemies[0].Position);
            discreteOut[1] = (combat != null && dist <= attackRange) ? 1 : 0;
        }
        else
        {
            discreteOut[0] = kNearest;
            discreteOut[1] = 0;
        }
    }

    /// <summary>
    /// Returns up to maxCount nearest ICombatTarget on the given layer, ordered by distance ascending
    /// </summary>
    public static List<ICombatTarget> GetNearestEnemies(Vector3 origin, float maxRadius, int maxCount, string layerName)
    {
        int layerMask = LayerMask.GetMask(layerName);

        List<ICombatTarget> results = new();

        if (layerMask == 0)
        {
            return results;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(origin, maxRadius, agroBuffer, layerMask);
        if (hitCount <= 0)
        {
            return results;
        }

        HashSet<ICombatTarget> seen = new();
        ICombatTarget combatTarget = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = agroBuffer[i];
            if (collider == null)
            {
                continue;
            }

            combatTarget = collider.GetComponentInParent<ICombatTarget>();
            if (combatTarget == null)
            {
                continue;
            }

            if (!seen.Contains(combatTarget))
            {
                seen.Add(combatTarget);
                results.Add(combatTarget);
            }
        }

        results = results
            .OrderBy(gameObject => Vector3.SqrMagnitude(combatTarget.Position - origin))
            .Take(maxCount)
            .ToList();

        return results;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.AggroRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);        
    }
}