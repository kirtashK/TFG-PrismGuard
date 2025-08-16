using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class Warehouse : MonoBehaviour, IItemConsumer
{
    public WarehouseType warehouseType;

    public int maxCapacity = 12;
    public int currentCapacity = 0;

    private int reservedForStore = 0;
    private int reservedForRetrieve = 0;

    public int FreeSlots => maxCapacity - (currentCapacity + reservedForStore);
    public int AvailableForRetrieve => currentCapacity - reservedForRetrieve;

    public ItemData storedItem;

    private readonly Queue<GameObject> storedItemsQueue = new();

    public event Action<ItemData> OnItemStored;
    public event Action<ItemData> OnItemRetrieved;

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (WarehouseManager.Instance == null 
            || ItemConsumerManager.Instance == null)
        {
            yield return null;
        }

        WarehouseManager.Instance.Register(this);
        ItemConsumerManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.Unregister(this);
        }
        if (ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.Unregister(this);
        }
    }

    public bool CanRetrieve(ItemData item)
    {
        return storedItem == item && AvailableForRetrieve > 0;
    }

    public GameObject RetrieveItem()
    {
        if (storedItemsQueue.Count == 0)
        {
            return null;
        }

        OnItemRetrieved?.Invoke(storedItem);

        return storedItemsQueue.Dequeue();
    }

    public bool CanReceive(ItemData data)
    {
        bool accepts = (currentCapacity == 0
                        ? data.storedIn.Contains(warehouseType)
                        : storedItem == data);
        return accepts && FreeSlots > 0;
    }

    public bool Reserve(ItemData data)
    {
        if (!CanReceive(data))
        {
            return false;
        }
        reservedForStore = Mathf.Min(reservedForStore + 1, maxCapacity);

        return true;
    }

    public void Release(ItemData data)
    {
        reservedForStore = Mathf.Max(reservedForStore - 1, 0);
    }

    public Vector3 GetReceivePosition() => transform.position;

    public void OnReceived(GameObject item, ItemData data)
    {
        item.SetActive(true);

        Release(data);

        if (currentCapacity == 0)
        {
            storedItem = data;
        }

        currentCapacity++;
        storedItemsQueue.Enqueue(item);
        item.transform.SetParent(transform, worldPositionStays: true);
        item.transform.position = GetReceivePosition();

        if (item.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
        {
            moveItemTask.Reset();
        }

        OnItemStored?.Invoke(data);
    }

    public void ConfirmRetrieval()
    {
        currentCapacity = Mathf.Max(0, currentCapacity - 1);
    }
}