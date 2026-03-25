using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Worker : MonoBehaviour, IOrderable, IStatRefresher
{
    [HideInInspector] public Unit unit;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey moveSpeedStat;
    [SerializeField] private StatKey maxCarryWeightStat;

    [HideInInspector] public float maxCarryWeight;
    public float currentLoad = 0f;

    private WorkerData data;
    private IWorkerState currentState;
    public ITask CurrentTask { get; set; }

    [SerializeField]
    private Transform inventorySpot;

    private readonly List<GameObject> inventory = new();

    private static readonly int AnimatorIsWorking = Animator.StringToHash("IsWorking");
    private static readonly int AnimatorWorkType = Animator.StringToHash("WorkType");

    [Header("Tools")]
    [SerializeField] private List<ToolEntry> tools;

    [Serializable]
    public class ToolEntry
    {
        public WorkType workType;
        public GameObject tool;
    }

    private void Awake()
    {
        if (TryGetComponent<Unit>(out Unit unit))
        {
            this.unit = unit;
            data = (WorkerData)unit.unitData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(unit)}");
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

        ChangeState(new IdleState());
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

        unit.OnDeathStartedEvent += OnDeathStarted;
        unit.OnDeathCleanupEvent += OnDeathCleanup;
        unit.OnDamageTakenEvent += OnDamageTaken;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        unit.OnDeathStartedEvent -= OnDeathStarted;
        unit.OnDeathCleanupEvent -= OnDeathCleanup;
        unit.OnDamageTakenEvent -= OnDamageTaken;
    }    

    private void Update()
    {
        if (unit.IsAlive)
        {
            currentState?.UpdateState(this);
        }
    }

    public void ChangeState(IWorkerState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState?.EnterState(this);
    }

    #region Stats

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

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, moveSpeedStat, out float finalValue))
        {
            unit.agent.speed = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out finalValue))
        {
            unit.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxCarryWeightStat, out finalValue))
        {
            maxCarryWeight = finalValue;
        }
    }

    #endregion

    private void OnDamageTaken(float amount, Vector3 attackOrigin)
    {
        if (unit.IsAlive)
        {
            DropAll(inventorySpot.position);
            Retreat(attackOrigin);
        }
    }

    private void OnDeathStarted()
    {
        ClearWorkAnimation();
    }

    private void OnDeathCleanup()
    {
        
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

        if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit,  retreatDistance,  NavMesh.AllAreas))
        {
            unit.agent.SetDestination(hit.position);
        }
        else
        {
            //agent.SetDestination(rawTarget);
        }
    }

    #region Inventory

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

    #endregion

    public void CancelCurrentTask()
    {
        if (CurrentTask == null)
        {
            return;
        }

        ClearWorkAnimation();

        CurrentTask.Cancel(this);
        CurrentTask = null;
    }

    #region Work Animation

    public void SetWorkAnimation(WorkType workType)
    {
        if (unit.animator == null)
        {
            return;
        }

        unit.animator.SetBool(AnimatorIsWorking, workType != WorkType.None);
        unit.animator.SetInteger(AnimatorWorkType, (int)workType);

        foreach (ToolEntry tool in tools)
        {
            if (tool.workType == workType)
            {
                tool.tool.SetActive(true);
                return;
            }
        }
    }

    public void ClearWorkAnimation()
    {
        if (unit.animator == null)
        {
            return;
        }

        unit.animator.SetBool(AnimatorIsWorking, false);
        unit.animator.SetInteger(AnimatorWorkType, (int)WorkType.None);

        foreach (ToolEntry tool in tools)
        {
            tool.tool.SetActive(false);
        }
    }

    #endregion

    #region Orderable

    public void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options)
    {
        if (!unit.IsAlive)
        {
            return;
        }

        CancelCurrentTask();

        ChangeState(new MovingState(destination, arrivalThreshold: 0.5f));
    }

    #endregion
}