using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly Dictionary<string, int> totals = new();
    private readonly Dictionary<string, ItemData> itemsById = new();
    public IEnumerable<ItemData> GetAllTrackedItems() => itemsById.Values;

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
            if (warehouse == null)
            {
                continue;
            }

            IReadOnlyDictionary<ItemData, int> snapshot = warehouse.GetStoredCountsSnapshot();
            foreach (KeyValuePair<ItemData, int> pair in snapshot)
            {
                AddInternal(pair.Key, pair.Value);
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
        if (warehouse == null)
        {
            return;
        }

        IReadOnlyDictionary<ItemData, int> snapshot = warehouse.GetStoredCountsSnapshot();
        foreach (KeyValuePair<ItemData, int> pair in snapshot)
        {
            int newCount = AddInternal(pair.Key, pair.Value);
            OnInventoryChanged?.Invoke(pair.Key, newCount);
        }
    }

    private void HandleWarehouseUnregistered(Warehouse warehouse)
    {
        if (warehouse == null)
        {
            return;
        }

        IReadOnlyDictionary<ItemData, int> snapshot = warehouse.GetStoredCountsSnapshot();
        foreach (KeyValuePair<ItemData, int> pair in snapshot)
        {
            int newCount = AddInternal(pair.Key, -pair.Value);
            OnInventoryChanged?.Invoke(pair.Key, newCount);
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
        if (itemData == null || string.IsNullOrEmpty(itemData.id))
        {
            Debug.LogError($"{name}: {nameof(AddInternal)}: null {nameof(ItemData)} or {nameof(itemData.id)}");
            return 0;
        }

        string itemId = itemData.id;
        itemsById[itemId] = itemData;

        if (!totals.TryGetValue(itemId, out int old))
        {
            old = 0;
        }

        int next = Mathf.Max(0, old + delta);
        totals[itemId] = next;

        return next;
    }

    public int GetTotal(ItemData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.id))
        {
            return 0;
        }

        return totals.TryGetValue(itemData.id, out int value) ? value : 0;
    }
}