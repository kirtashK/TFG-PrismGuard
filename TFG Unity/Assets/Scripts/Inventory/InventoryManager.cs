using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly Dictionary<ItemData, int> totals = new();

    public event Action<ItemData, int> OnInventoryChanged;

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

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (WarehouseManager.Instance == null)
        {
            yield return null;
        }

        foreach (Warehouse warehouse in WarehouseManager.Instance.AllWarehouses)
        {
            if (warehouse.storedItem != null && warehouse.currentCapacity > 0)
            {
                AddInternal(warehouse.storedItem, warehouse.currentCapacity);
            }
        }

        WarehouseManager.Instance.OnWarehouseRegistered += HandleWarehouseRegistered;
        WarehouseManager.Instance.OnWarehouseUnregistered += HandleWarehouseUnregistered;
        WarehouseManager.Instance.OnWarehouseItemStored += HandleWarehouseItemStored;
        WarehouseManager.Instance.OnWarehouseItemRetrieved += HandleWarehouseItemRetrieved;
    }

    private void OnDisable()
    {
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.OnWarehouseRegistered -= HandleWarehouseRegistered;
            WarehouseManager.Instance.OnWarehouseUnregistered -= HandleWarehouseUnregistered;
            WarehouseManager.Instance.OnWarehouseItemStored -= HandleWarehouseItemStored;
            WarehouseManager.Instance.OnWarehouseItemRetrieved -= HandleWarehouseItemRetrieved;
        }
    }

    private void HandleWarehouseRegistered(Warehouse warehouse)
    {
        if (warehouse.storedItem != null && warehouse.currentCapacity > 0)
        {
            OnInventoryChanged?.Invoke(warehouse.storedItem, AddInternal(warehouse.storedItem, warehouse.currentCapacity));
        }
    }

    private void HandleWarehouseUnregistered(Warehouse warehouse)
    {
        if (warehouse.storedItem != null && warehouse.currentCapacity > 0)
        {
            OnInventoryChanged?.Invoke(warehouse.storedItem, AddInternal(warehouse.storedItem, -warehouse.currentCapacity));
        }
    }

    private void HandleWarehouseItemStored(Warehouse warehouse, ItemData itemData)
    {
        int newCount = AddInternal(itemData, +1);
        OnInventoryChanged?.Invoke(itemData, newCount);
    }

    private void HandleWarehouseItemRetrieved(Warehouse warehouse, ItemData itemData)
    {
        int newCount = AddInternal(itemData, -1);
        OnInventoryChanged?.Invoke(itemData, newCount);
    }

    private int AddInternal(ItemData itemData, int delta)
    {
        if (!totals.TryGetValue(itemData, out int old))
        {
            old = 0;
        }

        int next = Mathf.Max(0, old + delta);
        totals[itemData] = next;

        return next;
    }

    public int GetTotal(ItemData itemData)
        => totals.TryGetValue(itemData, out int v) ? v : 0;
}