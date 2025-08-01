using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class Warehouse : MonoBehaviour
{
    [Tooltip("Tipo de almacen")]
    public WarehouseType warehouseType;

    [Tooltip("Capacidad total")]
    public int capacity = 12;

    public int storedCount = 0;

    private int reservedForStore = 0;
    private int reservedForRetrieve = 0;

    public int FreeSlots => capacity - (storedCount + reservedForStore);
    public int AvailableForRetrieve => storedCount - reservedForRetrieve;

    public ItemData storedItem;

    private readonly Queue<GameObject> storedItemsQueue = new();

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
        WarehouseManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.Unregister(this);
        }
    }

    public Vector3 GetStoragePosition()
    {
        return transform.position;
    }

    public bool CanStore(ItemData item)
    {
        bool accepts = storedCount == 0
            ? item.storedIn.Contains(warehouseType)
            : storedItem == item;
        return accepts && FreeSlots > 0;
    }

    public void StoreItem(GameObject itemObject, ItemData itemData)
    {
        //Debug.Log($"[{name}] StoreItem called. Before: storedCount={storedCount}, reservedStore={reservedForStore}");

        if (storedCount == 0)
        {
            storedItem = itemData;
        }

        ReleaseStoreReservation();
        storedCount++;

        itemObject.SetActive(true);
        itemObject.transform.SetParent(transform, worldPositionStays: true);
        itemObject.transform.position = GetStoragePosition();
        storedItemsQueue.Enqueue(itemObject);

        //Debug.Log($"[{name}] After Store: storedCount={storedCount}, reservedStore={reservedForStore}");
    }

    public bool CanRetrieve(ItemData item)
    {
        return storedItem == item && AvailableForRetrieve > 0;
    }

    public GameObject RetrieveItem()
    {
        //Debug.Log($"[{name}] RetrieveItem called. Before: storedCount={storedCount}, reservedRetrieve={reservedForRetrieve}");

        if (storedItemsQueue.Count == 0)
        {
            //Debug.LogWarning($"[{name}] RetrieveItem: empty queue");
            return null;
        }

        //Debug.Log($"[{name}] RetrieveItem succeeded. Queue now has {storedItemsQueue.Count - 1} items");
        return storedItemsQueue.Dequeue();
    }

    public void ConfirmRetrieval()
    {
        //Debug.Log($"[{name}] ConfirmRetrieval called. Before: storedCount={storedCount}, reservedRetrieve={reservedForRetrieve}");
        
        ReleaseRetrieveReservation();
        storedCount = Mathf.Max(storedCount - 1, 0);
        if (storedCount == 0)
        {
            storedItem = null;
        }

        //Debug.Log($"[{name}] After ConfirmRetrieval: storedCount={storedCount}, reservedRetrieve={reservedForRetrieve}");
    }

    public void ReserveStoreSlot()
    {
        reservedForStore = Mathf.Min(reservedForStore + 1, capacity);
    }

    public void ReleaseStoreReservation()
    {
        reservedForStore = Mathf.Max(reservedForStore - 1, 0);
    }

    public void ReserveRetrieveSlot()
    {
        reservedForRetrieve = Mathf.Min(reservedForRetrieve + 1, storedCount);
    }
    public void ReleaseRetrieveReservation()
    {
        reservedForRetrieve = Mathf.Max(reservedForRetrieve - 1, 0);
    }
}
