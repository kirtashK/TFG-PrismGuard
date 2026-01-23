using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Soldier : MonoBehaviour, IAddressableInstance, ICombatTarget, IOrderable, IGuardable, IAttackMovable
{
    public SoldierData data;

    private float currentHealth;

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
        currentState?.UpdateState(this);
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
            $"\nHealth of {name}: {currentHealth}/{data.maxHealth}");

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
        if (!isAlive || currentHealth >= data.maxHealth)
        {
            return;
        }

        float amountToRegenerate = data.maxHealth * percent;
        currentHealth = Mathf.Min(currentHealth + amountToRegenerate, data.maxHealth);

        Debug.Log($"{name} regenerated {amountToRegenerate} health (now {currentHealth}/{data.maxHealth})");
    }

    public void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle)
    {
        addressableInstanceHandle = handle;
        hasAddressableHandle = handle.IsValid();
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
}
