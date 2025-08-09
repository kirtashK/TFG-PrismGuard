using System;
using System.Collections.Generic;
using UnityEngine;

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    public List<Warehouse> allWarehouses = new();

    public IReadOnlyList<Warehouse> AllWarehouses => allWarehouses;

    public event Action<Warehouse> OnWarehouseRegistered;
    public event Action<Warehouse> OnWarehouseUnregistered;

    public event Action<Warehouse, ItemData> OnWarehouseItemStored;
    public event Action<Warehouse, ItemData> OnWarehouseItemRetrieved;

    private readonly Dictionary<Warehouse, Action<ItemData>> storedHandlers = new();
    private readonly Dictionary<Warehouse, Action<ItemData>> retrievedHandlers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    public void Register(Warehouse warehouse)
    {
        if (!allWarehouses.Contains(warehouse))
        {
            allWarehouses.Add(warehouse);

            void onStoredHandler(ItemData item)
            {
                OnWarehouseItemStored?.Invoke(warehouse, item);
            }
            void onRetrievedHandler(ItemData item)
            {
                OnWarehouseItemRetrieved?.Invoke(warehouse, item);
            }

            storedHandlers[warehouse] = onStoredHandler;
            retrievedHandlers[warehouse] = onRetrievedHandler;

            warehouse.OnItemStored += onStoredHandler;
            warehouse.OnItemRetrieved += onRetrievedHandler;

            OnWarehouseRegistered?.Invoke(warehouse);
        }
    }

    public void Unregister(Warehouse warehouse)
    {
        if (warehouse == null)
        {
            return;
        }

        if(allWarehouses.Remove(warehouse))
        {
            if (storedHandlers.TryGetValue(warehouse, out Action<ItemData> storedHandler))
            {
                warehouse.OnItemStored -= storedHandler;
                storedHandlers.Remove(warehouse);
            }

            if (retrievedHandlers.TryGetValue(warehouse, out Action<ItemData> retrievedHandler))
            {
                warehouse.OnItemRetrieved -= retrievedHandler;
                retrievedHandlers.Remove(warehouse);
            }

            OnWarehouseUnregistered?.Invoke(warehouse);
        }
    }

    public List<Warehouse> GetWarehousesThatCanStore(ItemData item)
    {
        List<Warehouse> result = new();
        foreach (Warehouse warehouse in allWarehouses)
        {
            if (warehouse.CanReceive(item))
            {
                result.Add(warehouse);
            }
        }
        return result;
    }

    public Warehouse FindNearestForStore(Vector3 fromPosition, ItemData item)
    {
        Warehouse best = null;
        float bestDist = float.MaxValue;

        foreach (Warehouse warehouse in allWarehouses)
        {
            if (warehouse.CanReceive(item))
            {
                float distance = Vector3.Distance(fromPosition, warehouse.transform.position);
                if (distance < bestDist)
                {
                    bestDist = distance;
                    best = warehouse;
                }
            }
        }

        return best;
    }

    public Warehouse FindNearestForRetrieve(ItemData item, Vector3 fromPosition)
    {
        Warehouse best = null;
        float bestDist = float.MaxValue;

        foreach (Warehouse warehouse in allWarehouses)
        {
            if (warehouse.CanRetrieve(item))
            {
                float distance = Vector3.Distance(fromPosition, warehouse.transform.position);
                if (distance < bestDist)
                {
                    bestDist = distance;
                    best = warehouse;
                }
            }
        }

        return best;
    }
}