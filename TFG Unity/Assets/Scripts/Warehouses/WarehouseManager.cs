using System.Collections.Generic;
using UnityEngine;

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    public List<Warehouse> allWarehouses = new();

    public IReadOnlyList<Warehouse> AllWarehouses => allWarehouses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(Warehouse warehouse)
    {
        if (!allWarehouses.Contains(warehouse))
            allWarehouses.Add(warehouse);
    }

    public void Unregister(Warehouse warehouse)
    {
        allWarehouses.Remove(warehouse);
    }

    public List<Warehouse> GetWarehousesThatCanStore(ItemData item)
    {
        List<Warehouse> result = new();
        foreach (Warehouse warehouse in allWarehouses)
        {
            if (warehouse.CanStore(item))
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
            if (warehouse.CanStore(item))
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
