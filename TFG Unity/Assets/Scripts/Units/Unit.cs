using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Unit : MonoBehaviour, IAddressableInstance, ITarget
{
    public UnitData unitData;

    [HideInInspector] public float maxHealth;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public float healOnWaveCompleted;

    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackCooldown;
    [HideInInspector] public float nextAttackTime = 0f;

    [HideInInspector] public Animator animator;
    [HideInInspector] public NavMeshAgent agent;

    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    private bool isDying = false;
    private readonly float deathAnimationTimeout = 30f;
    private readonly float deathAnimationDelay = 10f;

    private static readonly int AnimatorSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimatorDie = Animator.StringToHash("Die");
    private static readonly int AnimatorIsDead = Animator.StringToHash("IsDead");

    ITarget target;

    [SerializeField] private float dissolveDuration = 10.0f;
    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private readonly List<Renderer> cachedRenderers = new();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (TryGetComponent<Selectable>(out Selectable selectable))
        {
            selectable.data = unitData;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{name}: Missing {nameof(NavMeshAgent)}");
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"{name}: missing {nameof(Animator)}");
            }
        }

        CacheRenderers();
    }

    private void Update()
    {
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat(AnimatorSpeed, speed, 0.1f, Time.deltaTime);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        UIManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
    }

    #region ICombatTarget

    public event Action OnDeathStartedEvent;
    public event Action OnDeathCleanupEvent;
    public event Action<float, Vector3> OnDamageTakenEvent;
    public event Action<float> OnHealedEvent;

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public Faction Faction => unitData.faction;

    public Category Category => Category.Unit;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    public bool TryGetAttackPosition(Vector3 attackerPosition, out Vector3 attackPosition)
    {
        attackPosition = Position;
        return true;
    }

    /// <summary>
    /// Damages unit, if health falls to 0 the unit dies, otherwise calls OnDamageTaken
    /// </summary>
    /// <param name="amount">Damage received</param>
    /// <param name="attackOrigin"></param>
    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        if (isDying || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"{name} took {amount} damage" +
            $"\nHealth of {name}: {currentHealth}/{maxHealth}");

        OnDamageTaken(amount, attackOrigin);

        if (!IsAlive)
        {
            Die();
        }
    }

    public void OnDamageTaken(float amount, Vector3 attackOrigin)
    {
        OnDamageTakenEvent?.Invoke(amount, attackOrigin);
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

        Debug.Log($"{name} healed by {actualHealed}. Health: {currentHealth}/{maxHealth}");
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

    public Vector3 GetTargetAttackPosition(ITarget target)
    {
        Vector3 destination;
        if (target is ITarget iTarget
            && iTarget.TryGetAttackPosition(transform.position, out Vector3 attackPosition))
        {
            destination = attackPosition;
        }
        else
        {
            destination = target.Position;
        }
        return destination;
    }

    public void Attack(ITarget combatTarget)
    {
        if (!IsAlive || combatTarget == null || !combatTarget.IsAlive)
        {
            return;
        }

        target = combatTarget;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            target.TakeDamage(attackDamage, Position);
            target = null;
        }
    }

    public void OnAttackHit()
    {
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

        Debug.Log($"{name} has died");

        // Stop movement
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        OnDeathStarted();

        if (animator != null)
        {
            animator.SetBool(AnimatorIsDead, true);
            animator.SetTrigger(AnimatorDie);
            StartCoroutine(DeathAnimationTimeout());
        }
        else
        {
            CompleteDeathCleanup();
        }
    }

    public void OnDeathStarted()
    {
        // TODO notify player of unit death ?

        OnDeathStartedEvent?.Invoke();
    }

    private IEnumerator DeathAnimationTimeout()
    {
        float timer = 0f;
        while (timer < deathAnimationTimeout)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        Debug.LogWarning($"{name}: death animation took too long or missing event on death end");

        CompleteDeathCleanup();
    }

    /// <summary>
    /// Called by Animation Event at the end of death clip
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        StartCoroutine(DeathCleanupDelay());
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

        if (hasAddressableHandle && addressableInstanceHandle.IsValid())
        {
            Addressables.ReleaseInstance(addressableInstanceHandle);
            hasAddressableHandle = false;
        }

        Destroy(gameObject);
    }

    public void OnDeathCleanup()
    {
        OnDeathCleanupEvent?.Invoke();
    }

    #endregion


    #endregion

    public void FaceTarget(Vector3 targetPosition, float rotationSpeed)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

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