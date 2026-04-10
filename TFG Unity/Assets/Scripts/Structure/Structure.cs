using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class Structure : MonoBehaviour, ITarget, ICombatFeedbackSource
{
    public StructureData structureData;

    [HideInInspector] public float maxHealth;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public float healOnWaveCompleted;

    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackCooldown;
    [HideInInspector] public float nextAttackTime = 0f;

    private bool isDying = false;
    private readonly float deathAnimationDelay = 1f;

    [SerializeField] private float dissolveDuration = 10.0f;
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private readonly List<Renderer> cachedRenderers = new();
    private MaterialPropertyBlock propertyBlock;

    public event Action<float, Vector3> OnDamageTakenCombatFeedbackEvent;
    public event Action<float, Vector3> OnHealedCombatFeedbackEvent;

    [SerializeField] private Transform combatFeedbackPosition;
    public Vector3 CombatFeedbackPosition => combatFeedbackPosition.position;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Selectable>(out Selectable selectable))
        {
            selectable.data = structureData;
        }

        if (combatFeedbackPosition == null && !name.ToLower().Contains("blueprint"))
        {
            Debug.LogWarning($"{name}: missing {nameof(combatFeedbackPosition)}");
        }


        CacheAttackColliders();
        CacheRenderers();
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (UIManager.Instance == null
            || CombatFeedbackManager.Instance == null)
        {
            yield return null;
        }

        UIManager.Instance.OnWaveCompleted += OnWaveCompleted;
        CombatFeedbackManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
        if (CombatFeedbackManager.Instance != null)
        {
            CombatFeedbackManager.Instance.Unregister(this);
        }
    }

    #endregion

    #region ICombatTarget

    public event Action<ITarget> OnDeathStartedEvent;
    public event Action OnDeathCleanupEvent;
    public event Action<float, Vector3> OnDamageTakenEvent;
    public event Action<float> OnHealedEvent;

    public ITarget target;

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public Faction Faction => structureData.faction;

    public Category Category => Category.Structure;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    [Header("Attack position")]
    [SerializeField] private float attackPositionOffset = 0f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    private readonly List<Collider> cachedAttackColliders = new();

    private void CacheAttackColliders()
    {
        cachedAttackColliders.Clear();

        Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: false);
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            cachedAttackColliders.Add(collider);
        }
    }

    public bool TryGetAttackPosition(Vector3 attackerPosition, out Vector3 attackPosition)
    {
        attackPosition = transform.position;

        if (cachedAttackColliders.Count == 0)
        {
            return false;
        }

        bool foundValidPosition = false;
        float bestDistanceSqr = float.MaxValue;
        Vector3 bestPosition = transform.position;

        for (int i = 0; i < cachedAttackColliders.Count; i++)
        {
            Collider collider = cachedAttackColliders[i];
            if (collider == null || !collider.enabled)
            {
                continue;
            }

            Vector3 candidate = GetCandidateAttackPosition(collider, attackerPosition);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                float distanceSqr = (hit.position - attackerPosition).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestPosition = hit.position;
                    foundValidPosition = true;
                }
            }
        }

        if (foundValidPosition)
        {
            attackPosition = bestPosition;
        }

        return foundValidPosition;
    }

    private Vector3 GetCandidateAttackPosition(Collider collider, Vector3 attackerPosition)
    {
        Vector3 closestPoint = collider.ClosestPoint(attackerPosition);
        Vector3 center = collider.bounds.center;

        Vector3 outwardDirection = closestPoint - center;
        if (outwardDirection.sqrMagnitude < 0.0001f)
        {
            outwardDirection = attackerPosition - center;
        }

        if (outwardDirection.sqrMagnitude < 0.0001f)
        {
            outwardDirection = transform.forward;
        }

        return closestPoint + outwardDirection.normalized * attackPositionOffset;
    }

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        if (isDying || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        OnDamageTaken(amount, attackOrigin);

        if (!IsAlive)
        {
            Die();
        }
    }

    public void OnDamageTaken(float amount, Vector3 attackOrigin)
    {
        OnDamageTakenEvent?.Invoke(amount, attackOrigin);
        OnDamageTakenCombatFeedbackEvent?.Invoke(amount, transform.position);
    }

    public void Heal(float healAmount)
    {
        if (!IsAlive || healAmount <= 0f || currentHealth == maxHealth)
        {
            return;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        float actualHealed = currentHealth - previousHealth;

        OnHealedEvent?.Invoke(actualHealed);
        OnHealedCombatFeedbackEvent?.Invoke(actualHealed, transform.position);
    }

    public void HealPercentage(float percent)
    {
        if (!IsAlive || percent <= 0f)
        {
            return;
        }

        Heal(maxHealth * percent);
    }

    private void OnWaveCompleted(int waveNumber)
    {
        if (Faction != Faction.Player)
        {
            return;
        }

        HealPercentage(healOnWaveCompleted);
    }

    public void Attack(ITarget combatTarget)
    {
        if (!IsAlive || combatTarget == null || !combatTarget.IsAlive)
        {
            return;
        }

        target = combatTarget;

        // TODO Structure attacks via arrows...
        target.TakeDamage(attackDamage, Position);
        target = null;
    }

    public void OnAttackHit()
    {
        // TODO Structures attack via arrows, this is not used as arrows have code to check when they hit
        if (IsAlive && target != null && target.IsAlive)
        {
            target.TakeDamage(attackDamage, Position);
        }
        target = null;
    }

    #region Death

    public void Die()
    {
        if (isDying)
        {
            return;
        }
        isDying = true;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        foreach (NavMeshObstacle obstacle in GetComponentsInChildren<NavMeshObstacle>())
        {
            obstacle.carving = false;
            obstacle.enabled = false;
        }

        OnDeathStarted();

        StartCoroutine(DeathCleanupDelay());
    }

    public void OnDeathStarted()
    {
        // TODO notify player of structure death ?

        OnDeathStartedEvent?.Invoke(this);
    }

    public IEnumerator DeathCleanupDelay()
    {
        yield return new WaitForSeconds(deathAnimationDelay);

        yield return StartCoroutine(DissolveRoutine());

        CompleteDeathCleanup();
    }

    private IEnumerator DissolveRoutine()
    {
        if (cachedRenderers.Count == 0)
        {
            yield break;
        }

        float elapsedTime = 0f;
        float initialValue = 0f;
        {
            propertyBlock.Clear();
            cachedRenderers[0].GetPropertyBlock(propertyBlock);
            if (propertyBlock != null && propertyBlock.isEmpty == false)
            {
                initialValue = propertyBlock.GetFloat(DissolveAmountId);
            }
        }

        while (elapsedTime < dissolveDuration)
        {
            float time = Mathf.Clamp01(elapsedTime / dissolveDuration);

            float dissolveValue = Mathf.Lerp(initialValue, 1f, time);

            for (int i = 0; i < cachedRenderers.Count; i++)
            {
                Renderer rend = cachedRenderers[i];

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(DissolveAmountId, dissolveValue);
                rend.SetPropertyBlock(propertyBlock);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure fully dissolved
        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            Renderer rend = cachedRenderers[i];
            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(DissolveAmountId, 1f);
            rend.SetPropertyBlock(propertyBlock);
        }
    }

    public void CompleteDeathCleanup()
    {
        OnDeathCleanup();

        Destroy(gameObject);
    }

    public void OnDeathCleanup()
    {
        OnDeathCleanupEvent?.Invoke();

        if (structureData == null || structureData.faction != Faction.Player
            || structureData.blueprintPrefab == null)
        {
            return;
        }

        GameObject blueprintObject = Instantiate(structureData.blueprintPrefab, transform.position, transform.rotation);
        blueprintObject.transform.localScale = transform.localScale;
        PlacementController.Instance.ApplyMaterialToObject(blueprintObject, PlacementController.Instance.blueprintMaterial);
    }

    #endregion

    #endregion

    private void CacheRenderers()
    {
        cachedRenderers.Clear();
        propertyBlock = new MaterialPropertyBlock();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        foreach (Renderer renderer in renderers)
        {
            bool hasDissolve = false;
            Material[] materials = renderer.sharedMaterials;
            if (materials != null)
            {
                foreach (Material material in materials)
                {
                    if (material != null && material.HasProperty(DissolveAmountId))
                    {
                        hasDissolve = true;
                        break;
                    }
                }
            }

            if (hasDissolve)
            {
                cachedRenderers.Add(renderer);
            }
        }
    }
}