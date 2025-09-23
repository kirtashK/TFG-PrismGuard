using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR;

public class Soldier : MonoBehaviour, ICombatTarget
{
    public SoldierData data;

    private float currentHealth;

    [HideInInspector]
    public NavMeshAgent agent;

    private ISoldierState currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        currentHealth = data.maxHealth;

        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;

        ChangeState(new SoldierIdleState());
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void OnEnable()
    {
        // Subscribe to wave completed event to regen hp
        StartCoroutine(RegisterWhenUIManagerReady());
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    private IEnumerator RegisterWhenUIManagerReady()
    {
        while (UIManager.Instance == null)
        {
            yield return null;
        }

        UIManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    public void ChangeState(ISoldierState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter(this);
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    /// <summary>
    /// Soldier takes damage, if hp is 0 or lower, soldier dies
    /// </summary>
    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Health of {name}: {currentHealth}/{data.maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
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
        if (!IsAlive || currentHealth >= data.maxHealth)
        {
            return;
        }

        float amountToRegenerate = data.maxHealth * percent;
        currentHealth = Mathf.Min(currentHealth + amountToRegenerate, data.maxHealth);

        Debug.Log($"{name} regenerated {amountToRegenerate} health (now {currentHealth}/{data.maxHealth})");
    }


    private void Die()
    {
        Debug.Log($"{name} has died");
        // TODO Animacion muerte, sonido
        Destroy(gameObject);
    }
}
