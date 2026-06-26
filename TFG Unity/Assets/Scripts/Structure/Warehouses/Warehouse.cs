using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warehouse : MonoBehaviour, IItemConsumer
{
    [HideInInspector] public Structure structure;
    private WarehouseData data;

    public GameObject storage;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey maxCapacityStat;

    [Header("Runtime")]

    [SerializeField]
    private int maxCapacity;
    [SerializeField]
    private int currentCapacity = 0;

    public int MaxCapacity => maxCapacity;
    public int CurrentCapacity => currentCapacity;

    // Each ItemData has a queue of items stored in the warehouse
    private readonly Dictionary<string, Queue<GameObject>> storedItems = new();

    private readonly Dictionary<string, int> reservedForStoreByItem = new();
    private int reservedForStoreTotal = 0;

    private readonly Dictionary<string, int> reservedForRetrieveByItem = new();

    public int FreeSlots => Mathf.Max(0, maxCapacity - (currentCapacity + reservedForStoreTotal));
    public Vector3 GetReceivePosition() => storage.transform.position;

    public event Action<ItemData> OnItemStored;
    public event Action<ItemData> OnItemRetrieved;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = (WarehouseData)structure.structureData;
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
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (WarehouseManager.Instance == null 
            || ItemConsumerManager.Instance == null
            || StatModifierManager.Instance == null)
        {
            yield return null;
        }

        WarehouseManager.Instance.Register(this);
        ItemConsumerManager.Instance.Register(this);

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
        structure.OnDeathStartedEvent += OnDeathStarted;
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
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }
        structure.OnDeathStartedEvent -= OnDeathStarted;
    }

    #endregion

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
        if (maxCapacityStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxCapacityStat)}");
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
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxCapacityStat, out finalValue))
        {
            maxCapacity = (int)finalValue;
        }
    }

    #endregion

    private void OnDeathStarted(ITarget deadTarget)
    {
        DropAllStoredItems();
    }

    private void DropAllStoredItems()
    {
        if (storage == null)
        {
            return;
        }

        Vector3 dropOrigin = storage.transform.position;

        foreach (KeyValuePair<string, Queue<GameObject>> pair in storedItems)
        {
            Queue<GameObject> queue = pair.Value;

            while (queue.Count > 0)
            {
                GameObject item = queue.Dequeue();
                if (item == null)
                {
                    continue;
                }

                item.transform.SetParent(null, true);

                Vector3 randomPosition = UnityEngine.Random.insideUnitSphere * 1.5f;
                randomPosition.y = -0.1f;
                item.transform.position = dropOrigin + randomPosition;

                float randomYRotation = UnityEngine.Random.Range(0f, 360f);
                item.transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f) * item.transform.rotation;

                if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
                {
                    itemInstance.SetVisible(true);
                }
                if (item.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
                {
                    moveItemTask.ClearStored();
                }
            }
        }

        storedItems.Clear();
        reservedForStoreByItem.Clear();
        reservedForRetrieveByItem.Clear();
        reservedForStoreTotal = 0;
        currentCapacity = 0;
    }

    #region Storage Category

    // Checks whether this warehouse allows the category of the item
    private bool CategoryAllows(ItemData itemData)
    {
        if (itemData == null || itemData.category == null)
        {
            Debug.LogError($"{name}: {nameof(CategoryAllows)}: null {nameof(ItemData)} or {nameof(itemData.category)}");
            return false;
        }

        foreach (ItemCategory accepted in data.acceptedCategories)
        {
            if (accepted != null && accepted.Matches(itemData.category))
            {
                return true;
            }
        }
        return false;
    }

    public int CategoryMatchDepth(ItemCategory itemCategory)
    {
        // Returns:
        //  0  => exact match 
        //  1  => parent match
        //  2  => grandparent match
        // ...
        //  n => grand grand... n match
        // -1  => not accepted

        if (itemCategory == null)
        {
            return -1;
        }

        int depth = 0;
        ItemCategory current = itemCategory;
        while (current != null)
        {
            foreach (ItemCategory acceptedCategory in data.acceptedCategories)
            {
                if (acceptedCategory != null && acceptedCategory.id == current.id)
                {
                    return depth;
                }
            }
            current = current.parentCategory;
            depth++;
        }

        return -1;
    }

    #endregion

    #region ItemConsumer

    public bool CanRetrieve(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"{name}: {nameof(CanRetrieve)}: null {nameof(ItemData)}");
            return false;
        }
        if (!storedItems.TryGetValue(itemId, out Queue<GameObject> queue))
        {
            return false;
        }

        int reserved = reservedForRetrieveByItem.TryGetValue(itemId, out int storedReserved) ? storedReserved : 0;
        return (queue.Count - reserved) > 0;
    }

    public bool ReserveForRetrieve(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId) || !CanRetrieve(itemData))
        {
            return false;
        }
        if (reservedForRetrieveByItem.TryGetValue(itemId, out int reserved))
        {
            reservedForRetrieveByItem[itemId] = reserved + 1;
        }
        else
        {
            reservedForRetrieveByItem[itemId] = 1;
        }
        return true;
    }

    public GameObject RetrieveItem(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"{name}: {nameof(RetrieveItem)}: null {nameof(ItemData)}");
            return null;
        }

        if (!storedItems.TryGetValue(itemId, out Queue<GameObject> queue))
        {
            Debug.LogError($"{name}: {nameof(RetrieveItem)}: Failed to get queue from storedItems for [{itemData}]");
            return null;
        }

        if (queue.Count == 0)
        {
            Debug.LogError($"{name}: RetrieveItem: Queue empty [{itemData}]");
            return null;
        }

        return queue.Peek();
    } 

    public void ConfirmRetrieval(GameObject retrievedObject)
    {
        if (retrievedObject == null)
        {
            Debug.LogError($"{name}: null {nameof(retrievedObject)}");
            return;
        }
        if (!retrievedObject.TryGetComponent<ItemInstance>(out ItemInstance instance))
        {
            Debug.LogError($"{name}: retrievedObject has no {nameof(ItemInstance)}");
            return;
        }

        string itemId = instance.itemData.id;

        if (!storedItems.TryGetValue(itemId, out Queue<GameObject> queue))
        {
            Debug.LogError($"{name}: Failed to get queue for {instance.itemData}");
            return;
        }

        Queue<GameObject> rebuiltQueue = new();
        bool removed = false;

        while (queue.Count > 0)
        {
            GameObject current = queue.Dequeue();

            if (!removed && current == retrievedObject)
            {
                if (current != null && current.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
                {
                    moveItemTask.ClearStored();
                }

                currentCapacity = Mathf.Max(0, currentCapacity - 1);

                if (reservedForRetrieveByItem.TryGetValue(itemId, out int reserved) && reserved > 0)
                {
                    reservedForRetrieveByItem[itemId] = reserved - 1;
                }

                removed = true;
                continue;
            }

            rebuiltQueue.Enqueue(current);
        }

        if (!removed)
        {
            Debug.LogError($"{name}: retrieved object not found in queue for {instance.itemData}");
        }

        if (rebuiltQueue.Count == 0)
        {
            storedItems.Remove(itemId);
        }
        else
        {
            storedItems[itemId] = rebuiltQueue;
        }

        OnItemRetrieved?.Invoke(instance.itemData);
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
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId) || !CanReceive(itemData))
        {
            return false;
        }

        if (reservedForStoreByItem.TryGetValue(itemId, out int current))
        {
            reservedForStoreByItem[itemId] = current + 1;
        }
        else
        {
            reservedForStoreByItem[itemId] = 1;
        }

        reservedForStoreTotal++;
        return true;
    }

    public void Release(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"{name}: {nameof(Release)}: null {nameof(ItemData)}");
            return;
        }

        if (reservedForStoreByItem.TryGetValue(itemId, out int current) && current > 0)
        {
            reservedForStoreByItem[itemId] = current - 1;
            reservedForStoreTotal = Mathf.Max(0, reservedForStoreTotal - 1);
        }
    }

    public void OnReceived(GameObject item, ItemData itemData)
    {
        if (item == null || itemData == null)
        {
            Debug.LogError($"{name}: {nameof(OnReceived)}: null item or {nameof(ItemData)}");
            return;
        }

        string itemId = itemData.id;

        if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.SetVisible(true);
        }

        item.transform.SetParent(storage.transform, worldPositionStays: true);
        item.transform.position = GetReceivePosition();

        if (!storedItems.TryGetValue(itemId, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            storedItems[itemId] = queue;
        }
        queue.Enqueue(item);
        currentCapacity = Mathf.Max(0, currentCapacity + 1);

        Release(itemData);

        if (item.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
        {
            moveItemTask.MarkStored();
        }

        OnItemStored?.Invoke(itemData);
    }

    #endregion

    // Returns a readonly snapshot of stored counts by item
    public IReadOnlyDictionary<ItemData, int> GetStoredCountsSnapshot()
    {
        Dictionary<string, ItemData> itemDataById = new();
        Dictionary<string, int> countById = new();

        foreach (KeyValuePair<string, Queue<GameObject>> pair in storedItems)
        {
            if (pair.Value.Count <= 0)
            {
                continue;
            }

            GameObject peeked = pair.Value.Peek();
            if (peeked == null || !peeked.TryGetComponent<ItemInstance>(out ItemInstance instance) || instance.itemData == null)
            {
                continue;
            }

            string itemId = instance.itemData.id;
            if (string.IsNullOrEmpty(itemId))
            {
                continue;
            }

            itemDataById[itemId] = instance.itemData;
            countById[itemId] = pair.Value.Count;
        }

        Dictionary<ItemData, int> snapshot = new();
        foreach (KeyValuePair<string, int> entry in countById)
        {
            snapshot[itemDataById[entry.Key]] = entry.Value;
        }

        return snapshot;
    }
}