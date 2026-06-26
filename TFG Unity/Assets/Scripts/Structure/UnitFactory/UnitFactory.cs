using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DisallowMultipleComponent]
public class UnitFactory : MonoBehaviour
{
    [HideInInspector] public Structure structure;
    private UnitFactoryData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey maxConcurrentBatchesStat;
    [SerializeField] private StatKey processingSpeedStat;

    [HideInInspector] public int maxConcurrentBatches = 1;
    [HideInInspector] public int CurrentConcurrentBatches => buildingOrders.Count;

    private float processingSpeed = 1;

    [HideInInspector] public int maxQueueLength = 5;
    [HideInInspector] public int QueueCount => allOrders.Count;
    private int enqueueCounter = 0;

    public Transform[] spawnPoints;
    private int spawnRoundRobin = 0;

    [Tooltip("Empty GameObject where delivered items are stored")]
    public Transform storage;

    private readonly List<UnitProductionOrder> allOrders = new();
    private readonly Queue<UnitProductionOrder> readyQueue = new();
    private readonly List<UnitProductionOrder> buildingOrders = new();

    public event Action<UnitFactory, UnitProductionOrder> OnOrderEnqueued;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderStateChanged;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderCompleted;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderCancelled;

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = (UnitFactoryData)structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        CheckNullStats();

        if (storage == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(storage)}");
        }
        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }
    }

    private void Start()
    {
        RefreshStats();

        structure.currentHealth = structure.maxHealth;

        maxQueueLength = data.maxQueueLength;
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
        if (maxConcurrentBatchesStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxConcurrentBatchesStat)}");
        }
        if (processingSpeedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(processingSpeedStat)}");
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
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxConcurrentBatchesStat, out finalValue))
        {
            maxConcurrentBatches = (int)finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, processingSpeedStat, out finalValue))
        {
            processingSpeed = finalValue;
        }
    }

    #endregion

    public Vector3 GetReceivePosition()
    {
        return storage.position;
    }

    public Transform GetOrderStorageParent()
    {
        return storage;
    }

    public Vector3 GetOrderStoragePosition()
    {
        return GetReceivePosition();
    }

    // Called by UI to enqueue a single unit
    public Guid EnqueueProduction(UnitData unit)
    {
        if (unit == null)
        {
            Debug.LogWarning($"{nameof(UnitFactory)}: missing {nameof(unit)}");
            return Guid.Empty;
        }

        if (maxQueueLength >= 0 && CountPendingOrders() >= maxQueueLength)
        {
            Debug.LogWarning($"{nameof(UnitFactory)}: queue is full");
            return Guid.Empty;
        }

        // Reserve score first
        Guid scoreToken = ScoreManager.Instance.ReserveScore(unit.scoreCost);
        if (scoreToken == Guid.Empty)
        {
            Debug.Log($"{nameof(UnitFactory)}: Not enough available score to enqueue {unit.Name}");
            return Guid.Empty;
        }

        UnitProductionOrder order = new(unit, this, ++enqueueCounter, scoreToken);
        allOrders.Add(order);

        ItemConsumerManager.Instance.Register(order);

        order.OnStateChanged += HandleOrderStateChanged;
        order.OnCompleted += HandleOrderCompleted;
        order.OnCancelled += HandleOrderCancelled;

        OnOrderEnqueued?.Invoke(this, order);
        TryStartBuilds();

        return order.orderId;
    }

    private int CountPendingOrders()
    {
        int pending = 0;
        foreach (UnitProductionOrder order in allOrders)
        {
            if (order.State == UnitProductionOrder.OrderState.WaitingForItems ||
                order.State == UnitProductionOrder.OrderState.Ready ||
                order.State == UnitProductionOrder.OrderState.Building)
            {
                pending++;
            }
        }
        return pending;
    }

    internal void NotifyOrderReady(UnitProductionOrder order)
    {
        if (order == null)
        {
            return;
        }
        if (order.State != UnitProductionOrder.OrderState.Ready)
        {
            return;
        }
        if (readyQueue.Contains(order))
        {
            Debug.LogWarning($"{name}: tried to enqueue a duplicate order: {order.enqueueIndex}");
            return;
        }

        readyQueue.Enqueue(order);
        OnOrderStateChanged?.Invoke(this, order);
        TryStartBuilds();
    }

    private void HandleOrderStateChanged(UnitProductionOrder order)
    {
        OnOrderStateChanged?.Invoke(this, order);
    }

    private void HandleOrderCompleted(UnitProductionOrder order)
    {
        readyQueue.RemoveIfPresent(order);
        buildingOrders.Remove(order);
        allOrders.Remove(order);

        ItemConsumerManager.Instance.Unregister(order);
        OnOrderCompleted?.Invoke(this, order);

        TryStartBuilds();
    }

    private void HandleOrderCancelled(UnitProductionOrder order)
    {
        readyQueue.RemoveIfPresent(order);
        buildingOrders.Remove(order);
        allOrders.Remove(order);

        ItemConsumerManager.Instance.Unregister(order);
        OnOrderCancelled?.Invoke(this, order);

        TryStartBuilds();
    }

    private void TryStartBuilds()
    {
        //Debug.Log($"buildingOrders count = {buildingOrders.Count} & readyQueue count = {readyQueue.Count}");
        while (buildingOrders.Count < maxConcurrentBatches && readyQueue.Count > 0)
        {
            UnitProductionOrder next = readyQueue.Dequeue();
            if (next == null || next.State != UnitProductionOrder.OrderState.Ready)
            {
                Debug.LogWarning($"{name}: skipped invalid ready queue entry: {next.enqueueIndex}");
                continue;
            }

            buildingOrders.Add(next);
            next.StartBuilding();
            OnOrderStateChanged?.Invoke(this, next);

            StartCoroutine(BuildCoroutine(next));
        }
    }

    private IEnumerator BuildCoroutine(UnitProductionOrder order)
    {
        if (order == null)
        {
            yield break;
        }

        float time = Mathf.Max(0f, order.unitData.buildTime * processingSpeed);
        float timePassed = 0f;

        while (timePassed < time)
        {
            timePassed += Time.deltaTime;
            yield return null;
        }

        // If order was cancelled:
        if (order.State != UnitProductionOrder.OrderState.Building)
        {
            yield break;
        }

        // Build finished
        if (order.scoreReservationToken != Guid.Empty)
        {
            ScoreManager.Instance.CommitReservation(order.scoreReservationToken);
            order.scoreReservationToken = Guid.Empty;
        }

        order.ConsumeStoredItems();

        Transform spawn = GetNextSpawnPoint();
        AssetReferenceGameObject prefabReference = order.unitData.GetRandomPrefabReference();
        if (prefabReference != null)
        {
            AsyncOperationHandle<GameObject> handle = prefabReference.InstantiateAsync(spawn.position, spawn.rotation);
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject spawned = handle.Result;
                // Give handle to the unit so it frees it upon death
                if (spawned.TryGetComponent<IAddressableInstance>(out IAddressableInstance addressable))
                {
                    addressable.SetAddressableInstanceHandle(handle);
                }
            }
            else
            {
                Debug.LogError($"{nameof(UnitFactory)}: failed to instantiate unit prefab for {order.unitData.Name}");
            }
        }
        else
        {
            Debug.LogError($"UnitFactory: missing PrefabReference for {order.unitData.Name}");
        }

        order.Complete();
        OnOrderStateChanged?.Invoke(this, order);
        OnOrderCompleted?.Invoke(this, order);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform;
        }
        if (spawnRoundRobin >= spawnPoints.Length)
        {
            spawnRoundRobin = 0;
        }
        Transform spawnPointSelected = spawnPoints[spawnRoundRobin];
        spawnRoundRobin = (spawnRoundRobin + 1) % Mathf.Max(1, spawnPoints.Length);
        return spawnPointSelected;
    }

    // Cancel an order by ID Returns true if cancelled
    public bool CancelOrder(Guid orderId)
    {
        UnitProductionOrder target = allOrders.Find(order => order.orderId == orderId);
        if (target == null)
        {
            return false;
        }

        bool returnItems = (target.State != UnitProductionOrder.OrderState.Building);

        target.Cancel(returnItems);
        return true;
    }

    public IReadOnlyList<UnitProductionOrder> GetAllOrders() => allOrders.AsReadOnly();

    public IEnumerable<UnitData> GetProducibleUnits(UnitRegistryAddressables registry)
    {
        if (data.producibleUnits != null && data.producibleUnits.Count > 0)
        {
            return data.producibleUnits;
        }
        else if (registry != null)
        {
            return registry.loadedUnits;
        }
        else
        {
            return new List<UnitData>();
        }
    }
}

internal static class QueueExtensions
{
    public static void RemoveIfPresent<T>(this Queue<T> queue, T item)
    {
        if (queue.Count == 0)
        {
            return;
        }
        List<T> list = new(queue);
        if (list.Remove(item))
        {
            queue.Clear();
            foreach (T element in list)
            {
                queue.Enqueue(element);
            }
        }
    }
}