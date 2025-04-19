using UnityEngine;
using System.Collections;

public class Warehouse : MonoBehaviour
{
    [Tooltip("Tipo de almacen")]
    public WarehouseType warehouseType;

    [Tooltip("Capacidad total")]
    public int capacity = 12;

    public int currentCount = 0;

    public ItemData storedItem;

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
        if (currentCount == 0)
        {
            return item.storedIn.Contains(warehouseType);
        }

        return storedItem == item && currentCount < capacity;
    }

    public void StoreItem(ItemData item)
    {
        if (storedItem == null)
        {
            storedItem = item;
        }

        //currentCount++;
        //currentCount se aumenta donde se genere la tarea, para reservar el espacio y 
        // evitar problemas de varias tareas cuando no hay suficiente capacidad
        
        // TODO: actualizar UI
    }

    public bool CanRetrieve(ItemData item)
    {
        return storedItem == item && currentCount > 0;
    }

    public void RetrieveItem()
    {
        currentCount--;
        // TODO: actualizar UI
    }
}
