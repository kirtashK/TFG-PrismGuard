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

    /// <summary>
    /// Finds items matching ItemData in all warehouses and returns a list with the items found.
    /// These items are retrieved from warehouses, meaning they have been reserved and released.
    /// Their MoveItemTasks are not reset nor they are unparented from the warehouse
    /// </summary>
    /// <param name="itemData">ItemData to find</param>
    /// <param name="quantity">Amount of items to find</param>
    /// <returns> List of items matching itemData found in all warehouses</returns>
    public List<GameObject> FindItemsInWarehouses(ItemData itemData, int quantity = 1)
    {
        List<GameObject> itemsFound = new();

        foreach (Warehouse warehouse in AllWarehouses)
        {
            if (quantity <= 0)
            {
                break;
            }
            if (warehouse == null)
            {
                continue;
            }

            while (quantity > 0 && warehouse.CanRetrieve(itemData))
            {
                if (!warehouse.ReserveForRetrieve(itemData))
                {
                    break;
                }

                GameObject grabbed = warehouse.RetrieveItem(itemData);
                if (grabbed == null)
                {
                    Debug.LogWarning($"{name} retrieved null item: {itemData.name}");
                    break;
                }

                itemsFound.Add(grabbed);
                warehouse.ConfirmRetrieval(grabbed);
                quantity--;
            }
        }

        return itemsFound;
    }
}