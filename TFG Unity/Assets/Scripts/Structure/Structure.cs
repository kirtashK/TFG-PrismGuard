using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Structure : MonoBehaviour, ITarget
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


    private void Awake()
    {
        if (TryGetComponent<Selectable>(out Selectable selectable))
        {
            selectable.data = structureData;
        }

        CacheRenderers();
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

    #region ICombatTarget

    public event Action OnDeathStartedEvent;
    public event Action OnDeathCleanupEvent;
    public event Action<float, Vector3> OnDamageTakenEvent;

    ITarget target;

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    public Faction Faction => structureData.faction;

    public Category Category => Category.Structure;

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        if (isDying || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"{name} took {amount} damage. " +
            $"Health of {name}: {currentHealth}/{maxHealth}");

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

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

        Debug.Log($"{name} healed by {healAmount}. Health: {currentHealth}/{maxHealth}");
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

        // TODO Structure attacks via arrow, call OnAttackHit when arrow's collider hits different faction ?
        target.TakeDamage(attackDamage, Position);
        target = null;
    }

    // TODO Called by arrows shot by a structure, implement shooting arrows
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

        Debug.Log($"{name} has been destroyed");

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        OnDeathStarted();

        StartCoroutine(DeathCleanupDelay());
    }

    public void OnDeathStarted()
    {
        // TODO notify player of structure death ?

        OnDeathStartedEvent?.Invoke();
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