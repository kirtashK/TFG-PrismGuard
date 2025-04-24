using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Warehouse : MonoBehaviour
{
    [Tooltip("Tipo de almacen")]
    public WarehouseType warehouseType;

    [Tooltip("Capacidad total")]
    public int capacity = 12;

    public int currentCount = 0;

    public ItemData storedItem;

    private readonly Queue<GameObject> storedItemsQueue = new Queue<GameObject>();

    private int reservedCount;

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
        bool accepts = currentCount == 0
            ? item.storedIn.Contains(warehouseType)
            : storedItem == item;
        return accepts && FreeSlots > 0;
    }

    public void StoreItem(GameObject itemObject, ItemData item)
    {
        if (storedItem == null)
            storedItem = item;

        currentCount++;

        itemObject.transform.SetParent(transform, worldPositionStays: true);
        itemObject.transform.position = GetStoragePosition();
        storedItemsQueue.Enqueue(itemObject);

        ReleaseReservation();

        // TODO: actualizar UI
    }

    public bool CanRetrieve(ItemData item)
    {
        //Debug.LogWarning("Can retrieve: " + (storedItem == item && currentCount > 0)
        //    + "\nQueue: " + storedItemsQueue.Count);
        return storedItem == item && currentCount > 0;
    }

    public GameObject RetrieveItem()
    {
        if (currentCount == 0)
            return null;

        currentCount--;
        GameObject obj = storedItemsQueue.Dequeue();
        if (currentCount == 0)
            storedItem = null;
        return obj;

        // TODO: actualizar UI
    }

    public int FreeSlots => capacity - (currentCount + reservedCount);

    public void ReserveSlot()
    {
        reservedCount++;
    }

    public void ReleaseReservation()
    {
        reservedCount--;
    }
}
