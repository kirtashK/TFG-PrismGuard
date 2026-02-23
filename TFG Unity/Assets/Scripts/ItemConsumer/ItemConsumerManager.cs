using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemConsumerManager : MonoBehaviour
{
    public static ItemConsumerManager Instance { get; private set; }

    private readonly List<IItemConsumer> consumers = new();
    public IReadOnlyList<IItemConsumer> Consumers => consumers;

    public event System.Action<IItemConsumer> OnConsumerUnregistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Register(IItemConsumer consumer)
    {
        if (!consumers.Contains(consumer))
        {
            consumers.Add(consumer);
        }
    }

    public void Unregister(IItemConsumer consumer)
    {
        if (consumer == null)
        {
            return;
        }

        OnConsumerUnregistered?.Invoke(consumer);

        if (consumers.Contains(consumer))
        {
            consumers.Remove(consumer);
        }
    }

    public IEnumerable<IItemConsumer> ActiveConsumers
    {
        get
        {
            foreach (IItemConsumer consumer in consumers)
            {
                if (consumer is MonoBehaviour monoBehaviour)
                {
                    if (monoBehaviour.isActiveAndEnabled)
                    {
                        yield return consumer;
                    }
                }
                else
                {
                    yield return consumer;
                }
            }
        }
    }

    /// <summary>
    /// Find consumers that can receive the given item, ordered by priority:
    /// 1) non-Warehouse consumers 
    /// 2) Warehouses
    /// </summary>
    public IEnumerable<IItemConsumer> FindCandidatesFor(ItemData itemData, Vector3 origin, IItemConsumer lastConsumer = null)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: FindCandidatesFor: null ItemData {nameof(itemData)}");
            yield break;
        }

        List<IItemConsumer> nonWarehouses = new();
        List<Warehouse> warehouses = new();

        // Split activeConsumers into warehouses & nonWarehouses:
        foreach (IItemConsumer consumer in ActiveConsumers)
        {
            if (consumer == null)
            {
                continue;
            }
            if (consumer is Warehouse warehouse)
            {
                warehouses.Add(warehouse);
            }
            else
            {
                nonWarehouses.Add(consumer);
            }
        }

        // First: non-warehouse consumers that can receive
        List<IItemConsumer> nonWarehouseCandidates = nonWarehouses
            .Where(consumer => consumer != lastConsumer && consumer.CanReceive(itemData))
            .OrderBy(consumer =>
            {
                if (consumer is MonoBehaviour monoBehaviour)
                {
                    return Vector3.SqrMagnitude(monoBehaviour.transform.position - origin);
                }
                return float.MaxValue;
            })
            .ToList();

        foreach (IItemConsumer consumer in nonWarehouseCandidates)
        {
            yield return consumer;
        }

        // Second: warehouses
        List<(Warehouse warehouse, int depth, float distSq, int freeSlots)> warehouseCandidates = new();

        foreach (Warehouse warehouse in warehouses)
        {
            if (warehouse == null)
            {
                continue;
            }
            if (!warehouse.gameObject.activeInHierarchy)
            {
                continue;
            }

            int depth = -1;
            try
            {
                depth = warehouse.CategoryMatchDepth(itemData.category);
            }
            catch
            {
                depth = -1;
            }

            if (depth < 0)
            {
                continue;
            }
            if (!warehouse.CanReceive(itemData))
            {
                continue;
            }

            float distSq = Vector3.SqrMagnitude(((MonoBehaviour)warehouse).transform.position - origin);
            int freeSlots = warehouse.FreeSlots;

            warehouseCandidates.Add((warehouse, depth, distSq, freeSlots));
        }

        List<Warehouse> orderedWarehouses = warehouseCandidates
            .OrderBy(candidate => candidate.depth)
            .ThenBy(candidate => candidate.distSq)
            .ThenByDescending(candidate => candidate.freeSlots)
            .Select(candidate => candidate.warehouse)
            .Where(warehouse => !ReferenceEquals(warehouse, lastConsumer))
            .ToList();

        foreach (Warehouse warehouse in orderedWarehouses)
        {
            yield return warehouse;
        }
    }
}