using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Worker : MonoBehaviour, IAddressableInstance, ICombatTarget, 
                    IOrderable, IStatRefresher
{
    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey maxCarryWeightStat;

    [HideInInspector] public float maxHealth;
    private float currentHealth;
    [HideInInspector] public float maxCarryWeight;
    public float currentLoad = 0f;

    public WorkerData data;
    private IWorkerState currentState;

    public NavMeshAgent agent { get; private set; }
    public ITask currentTask { get; set; }

    [SerializeField]
    private Transform inventorySpot;

    private readonly List<GameObject> inventory = new();


    private AsyncOperationHandle<GameObject> addressableInstanceHandle;
    private bool hasAddressableHandle = false;

    private void Start()
    {
        RefreshStats();

        currentHealth = maxHealth;

        ChangeState(new IdleState());
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{name}: Missing {nameof(NavMeshAgent)}");
        }

        CheckNullStats();
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
        if (maxCarryWeightStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxCarryWeightStat)}");
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

    private void Update()
    {
        currentState?.UpdateState(this);
    }

    public void ChangeState(IWorkerState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState?.EnterState(this);
    }

    public Vector3 Position => transform.position;

    public bool isAlive => currentHealth > 0f;

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, moveSpeedStat, out float finalValue))
        {
            agent.speed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxCarryWeightStat, out finalValue))
        {
            maxCarryWeight = finalValue;
        }
    }

    /// <summary>
    /// Damages worker, if its still alive, it runs away
    /// </summary>
    /// <param name="amount">Damage to receive</param>
    /// <param name="attackerOrigin">Attacker's position</param>
    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        //Debug.Log($"Health of {name}: {currentHealth}/{workerData.maxHealth}");

        DropAll(inventorySpot.position);

        if (currentHealth > 0f)
        {
            Retreat(attackOrigin);
        }
        else
        {
            Die();
        }
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

    /// <summary>
    /// Worker runs away from threatPosition
    /// </summary>
    /// <param name="threatPosition">Threat's position</param>
    /// <param name="retreatDistance">Distance to retreat</param>
    public void Retreat(Vector3 threatPosition, float retreatDistance = 5f)
    {
        Vector3 fleeDir = (transform.position - threatPosition).normalized;

        Vector3 rawTarget = transform.position + fleeDir * retreatDistance;

        if (NavMesh.SamplePosition(rawTarget,
                                   out NavMeshHit hit,
                                   retreatDistance,
                                   NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            //agent.SetDestination(rawTarget);
        }
    }

    // #############
    // # Inventory #
    // #############

    public bool CanCarry(ItemData itemData)
        => currentLoad + itemData.weight <= maxCarryWeight;

    public void PickUp(GameObject item)
    {
        inventory.Add(item);

        if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.carrier = this;
            currentLoad += itemInstance.itemData.weight;
            itemInstance.SetVisible(false);
        }

        item.transform.SetParent(inventorySpot, worldPositionStays: true);
    }

    public void ClearFromInventory(GameObject item)
    {
        inventory.Remove(item);
    }

    public void DropAll(Vector3 dropPosition)
    {
        foreach (GameObject carriedItem in inventory)
        {
            if (carriedItem == null)
            {
                continue;
            }

            if (carriedItem.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
            {
                itemInstance.carrier = null;
                itemInstance.SetVisible(true);
            }

            carriedItem.transform.SetParent(null, worldPositionStays: true);
            carriedItem.transform.position = dropPosition;
        }
        inventory.Clear();
        currentLoad = 0f;
    }

    public void DropItem(GameObject carriedItem, Vector3 dropPosition)
    {
        if (carriedItem == null)
        {
            return;
        }

        if (carriedItem.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.carrier = null;
            currentLoad = Mathf.Max(0f, currentLoad - itemInstance.itemData.weight);
            itemInstance.SetVisible(true);
        }

        inventory.Remove(carriedItem);

        carriedItem.transform.SetParent(null, worldPositionStays: true);
        carriedItem.transform.position = dropPosition;
    }

    public void CancelCurrentTask()
    {
        if (currentTask == null)
        {
            return;
        }

        currentTask.Cancel(this);
        currentTask = null;
    }

    // IOrderable
    public void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options)
    {
        CancelCurrentTask();

        // Move to destination
        ChangeState(new MovingState(destination, arrivalThreshold: 0.5f));
    }
}