using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DisallowMultipleComponent]
public class UnitFactory : MonoBehaviour
{
    private UnitFactoryData unitFactoryData;

    private int concurrentSlots = 1;
    private int maxQueueLength = 5;

    public Transform[] spawnPoints;
    private int spawnRoundRobin = 0;

    [Tooltip("Parent transform where delivered items are stored")]
    public Transform storageParent;

    private readonly List<UnitProductionOrder> allOrders = new();
    private readonly Queue<UnitProductionOrder> readyQueue = new();
    private readonly List<UnitProductionOrder> buildingOrders = new();

    public event Action<UnitFactory, UnitProductionOrder> OnOrderEnqueued;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderStateChanged;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderCompleted;
    public event Action<UnitFactory, UnitProductionOrder> OnOrderCancelled;

    private int enqueueCounter = 0;

    private void Awake()
    {
        if (storageParent == null)
        {
            Debug.LogWarning($"{name} missing storage");
        }

        if (!TryGetComponent<Structure>(out Structure structure))
        {
            Debug.LogError($"{name} missing Structure component");
        }
        if (structure.structureData is UnitFactoryData unitFactoryData)
        {
            this.unitFactoryData = unitFactoryData;

            concurrentSlots = unitFactoryData.concurrentSlots;
            maxQueueLength = unitFactoryData.maxQueueLength;
        }
        else
        {
            Debug.LogWarning($"{name} couldnt get UnitFactoryData from Structure");
        }
    }

    public Vector3 GetReceivePosition()
    {
        return storageParent.position;
    }

    public Transform GetOrderStorageParent()
    {
        return storageParent;
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
            Debug.LogWarning("UnitFactory.EnqueueProduction: unit null");
            return Guid.Empty;
        }

        if (maxQueueLength >= 0 && CountPendingOrders() >= maxQueueLength)
        {
            Debug.LogWarning("UnitFactory.EnqueueProduction: queue is full");
            return Guid.Empty;
        }

        // Reserve score first
        Guid scoreToken = ScoreManager.Instance.ReserveScore(unit.scoreCost);
        if (scoreToken == Guid.Empty)
        {
            Debug.Log($"UnitFactory: Not enough available score to enqueue {unit.Name}");
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
        // Start as many builds as allowed by concurrentSlots
        while (buildingOrders.Count < concurrentSlots && readyQueue.Count > 0)
        {
            UnitProductionOrder next = readyQueue.Dequeue();
            if (next == null || next.State != UnitProductionOrder.OrderState.Ready)
            {
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

        float time = Mathf.Max(0f, order.unitData.buildTime);
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

        // Instantiate the unit
        Transform spawn = GetNextSpawnPoint();
        if (order.unitData.PrefabReference != null && order.unitData.PrefabReference.RuntimeKeyIsValid())
        {
            AsyncOperationHandle<GameObject> handle = order.unitData.PrefabReference.InstantiateAsync(spawn.position, spawn.rotation);
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
                Debug.LogError($"UnitFactory: failed to instantiate unit prefab for {order.unitData.Name}");
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

    // UI helpers
    public IReadOnlyList<UnitProductionOrder> GetAllOrders() => allOrders.AsReadOnly();

    public IEnumerable<UnitData> GetProducibleUnits(UnitRegistryAddressables registry)
    {
        if (unitFactoryData.producibleUnits != null && unitFactoryData.producibleUnits.Count > 0)
        {
            return unitFactoryData.producibleUnits;
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

// Small helper extension to remove from queue if present
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