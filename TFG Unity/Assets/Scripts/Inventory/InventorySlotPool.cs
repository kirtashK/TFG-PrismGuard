using System.Collections.Generic;
using UnityEngine;

public class InventorySlotPool : MonoBehaviour
{
    public static InventorySlotPool Instance { get; private set; }

    [Tooltip("Inventory Slot Prefab")]
    public InventorySlotUI slotPrefab;

    private readonly Stack<InventorySlotUI> pool = new();

    private void Awake()
    {
        Instance = this;
    }

    public InventorySlotUI Get()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }

        return Instantiate(slotPrefab, transform);
    }

    public void Release(InventorySlotUI slot)
    {
        slot.gameObject.SetActive(false);
        pool.Push(slot);
    }
}