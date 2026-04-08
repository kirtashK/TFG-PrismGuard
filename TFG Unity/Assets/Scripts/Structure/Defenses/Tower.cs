using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Tower : MonoBehaviour
{
    [HideInInspector] public Structure structure;
    private StructureData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;

    [SerializeField] private StatKey attackRangeStat;
    [SerializeField] private StatKey attackDamageStat;
    [SerializeField] private StatKey attackCooldownStat;
    [SerializeField] private StatKey detectionRadiusStat;

    [HideInInspector] public float detectionRadius;

    [Header("Shooting")]

    [Tooltip("Projectile prefab spawned when the tower attacks")]
    [SerializeField] private TowerProjectile projectilePrefab;

    [Tooltip("Transform where the projectile spawns")]
    [SerializeField] private Transform muzzle;

    [Tooltip("Forward offset applied when spawning the projectile")]
    [SerializeField] private float muzzleOffset = 0.15f;

    [Tooltip("Vertical offset added to the target aim point")]
    [SerializeField] private float aimHeightOffset = 1f;

    private Sensor sensor;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        sensor = GetComponentInChildren<Sensor>(true);
        if (sensor == null)
        {
            Debug.LogError($"{name}: missing {nameof(Sensor)}");
        }

        if (muzzle == null)
        {
            Debug.LogError($"{name}: missing {nameof(muzzle)}");
        }
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(projectilePrefab)}");
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

        structure.currentHealth = structure.maxHealth;

        sensor.Initialize(structure.transform, structure.Faction, detectionRadius);
    }

    private void Update()
    {
        TryAttack();
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
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out float finalValue))
        {
            structure.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            structure.healOnWaveCompleted = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackRangeStat, out finalValue))
        {
            structure.attackRange = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackDamageStat, out finalValue))
        {
            structure.attackDamage = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, attackCooldownStat, out finalValue))
        {
            structure.attackCooldown = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, detectionRadiusStat, out finalValue))
        {
            detectionRadius = finalValue;
        }
    }

    #endregion

    /// <summary>
    /// Finds the closest enemy unit/structure inside aggroRadius
    /// </summary>
    /// <returns></returns>
    public ITarget FindNearestEnemyTarget()
    {
        return sensor.GetBestTarget(transform.position);
    }

    private bool IsTargetInAttackRange(ITarget target)
    {
        if (target == null || !target.IsAlive)
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, target.Position);
        return distance <= structure.attackRange;
    }

    private void TryAttack()
    {
        if (structure == null || !structure.IsAlive)
        {
            return;
        }

        if (Time.time < structure.nextAttackTime)
        {
            return;
        }

        structure.target = FindNearestEnemyTarget();

        if (structure.target == null || !structure.target.IsAlive)
        {
            return;
        }

        if (!IsTargetInAttackRange(structure.target))
        {
            return;
        }

        FireProjectile(structure.target);
        structure.nextAttackTime = Time.time + structure.attackCooldown;
    }

    private void FireProjectile(ITarget target)
    {
        if (projectilePrefab == null || target == null)
        {
            return;
        }

        Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position;
        Vector3 targetPosition = target.Position + Vector3.up * aimHeightOffset;
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        spawnPosition += direction * muzzleOffset;

        TowerProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction, Vector3.up));

        projectile.Initialize(structure.attackDamage, structure.Faction, structure.Position,
            direction, projectilePrefab.ProjectileSpeed, structure.transform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
