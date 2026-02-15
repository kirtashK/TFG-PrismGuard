using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class Warehouse : MonoBehaviour, IItemConsumer
{
    [Header("Configuration")]
    [Tooltip("Categories this warehouse accepts. If a category is an ancestor of an item's category it will match")]
    public List<ItemCategory> acceptedCategories = new();

    [Tooltip("Maximum total number of items that can be stored")]
    public int maxCapacity = 12;

    public int currentCapacity = 0;

    // Each ItemData has a queue of items stored in the warehouse
    private readonly Dictionary<ItemData, Queue<GameObject>> storedItems = new();

    private readonly Dictionary<ItemData, int> reservedForStoreByItem = new();
    private int reservedForStoreTotal = 0;

    private readonly Dictionary<ItemData, int> reservedForRetrieveByItem = new();

    public int FreeSlots => Mathf.Max(0, maxCapacity - (currentCapacity + reservedForStoreTotal));
    public Vector3 GetReceivePosition() => transform.position;

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

    // Checks whether this warehouse allows the category of the item
    private bool CategoryAllows(ItemData itemData)
    {
        if (itemData == null || itemData.category == null)
        {
            Debug.LogError($"{name}: CategoryAllows: null ItemData or category");
            return false;
        }
        if (acceptedCategories == null || acceptedCategories.Count == 0)
        {
            Debug.LogError($"{name}: CategoryAllows: Category not configured");
            return false;
        }

        foreach (ItemCategory accepted in acceptedCategories)
        {
            if (accepted != null && accepted.Matches(itemData.category))
            {
                return true;
            }
        }
        return false;
    }

    public bool CanRetrieve(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError($"{name}: CanRetrieve: null ItemData: [{itemData}]");
            return false;
        }
        if (!storedItems.TryGetValue(itemData, out Queue<GameObject> queue))
        {
            return false;
        }

        int reserved = reservedForRetrieveByItem.TryGetValue(itemData, out int storedReserved) ? storedReserved : 0;
        return (queue.Count - reserved) > 0;
    }

    public bool ReserveForRetrieve(ItemData itemData)
    {
        if (!CanRetrieve(itemData))
        {
            return false;
        }
        if (reservedForRetrieveByItem.TryGetValue(itemData, out int reserved))
        {
            reservedForRetrieveByItem[itemData] = reserved + 1;
        }
        else
        {
            reservedForRetrieveByItem[itemData] = 1;
        }
        return true;
    }

    public GameObject RetrieveItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError($"{name}: RetrieveItem: null ItemData [{itemData}]");
            return null;
        }

        if (!storedItems.TryGetValue(itemData, out Queue<GameObject> queue))
        {
            Debug.LogError($"{name}: RetrieveItem: Failed to get queue from storedItems for [{itemData}]");
            return null;
        }

        if (queue.Count == 0)
        {
            Debug.LogError($"{name}: RetrieveItem: Queue empty [{itemData}]");
            return null;
        }

        return queue.Peek();
}

    public void ConfirmRetrieval(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError($"{name}: ConfirmRetrieval: null ItemData: [{itemData}]");
            return;
        }
        if (!storedItems.TryGetValue(itemData, out Queue<GameObject> queue))
        {
            Debug.LogError($"{name}: ConfirmRetrieval: Failed to get queue from storedItems for [{itemData}]");
            return;
        }
        if (queue.Count == 0)
        {
            Debug.LogError($"{name}: ConfirmRetrieval: Queue empty [{itemData}]");
            return;
        }

        GameObject gameObject = queue.Dequeue();
        currentCapacity = Mathf.Max(0, currentCapacity - 1);

        if (reservedForRetrieveByItem.TryGetValue(itemData, out int reserved) && reserved > 0)
        {
            reservedForRetrieveByItem[itemData] = reserved - 1;
        }

        if (queue.Count == 0)
        {
            storedItems.Remove(itemData);
        }

        OnItemRetrieved?.Invoke(itemData);
    }

    public bool CanReceive(ItemData itemData)
    {
        if (!CategoryAllows(itemData))
        {
            return false;
        }
        return FreeSlots > 0;
    }

    public bool Reserve(ItemData itemData)
    {
        if (!CanReceive(itemData))
        {
            return false;
        }

        if (reservedForStoreByItem.TryGetValue(itemData, out int current))
        {
            reservedForStoreByItem[itemData] = current + 1;
        }
        else
        {
            reservedForStoreByItem[itemData] = 1;
        }

        reservedForStoreTotal++;
        return true;
    }

    public void Release(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError($"{name}: Release: null ItemData: [{itemData}]");
            return;
        }
        if (reservedForStoreByItem.TryGetValue(itemData, out int current) && current > 0)
        {
            reservedForStoreByItem[itemData] = current - 1;
            reservedForStoreTotal = Mathf.Max(0, reservedForStoreTotal - 1);
        }
    }


    public void OnReceived(GameObject item, ItemData itemData)
    {
        if (item == null || itemData == null)
        {
            Debug.LogError($"{name}: OnReceived: null item or ItemData");
            return;
        }

        if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.SetVisible(true);
        }

        item.transform.SetParent(transform, worldPositionStays: true);
        item.transform.position = GetReceivePosition();

        if (!storedItems.TryGetValue(itemData, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            storedItems[itemData] = queue;
        }
        queue.Enqueue(item);
        currentCapacity = Mathf.Max(0, currentCapacity + 1);

        Release(itemData);

        if (item.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
        {
            moveItemTask.Reset();
        }

        OnItemStored?.Invoke(itemData);
    }

    // Returns a readonly snapshot of stored counts by item
    public IReadOnlyDictionary<ItemData, int> GetStoredCountsSnapshot()
    {
        Dictionary<ItemData, int> snapshot = new();
        foreach (KeyValuePair<ItemData, Queue<GameObject>> pair in storedItems)
        {
            snapshot[pair.Key] = pair.Value.Count;
        }
        return snapshot;
    }
}