using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    public RectTransform content;
    public ScrollRect scrollRect;
    public float debounceTime = 0.05f;

    private readonly SortedList<string, InventorySlotUI> slots = new();

    private readonly HashSet<ItemData> dirtyItems = new();
    private bool refreshScheduled = false;

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (InventoryManager.Instance == null)
        {
            yield return null;
        }

        InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;

        foreach (KeyValuePair<ItemData, int> kv in InventoryManager.Instance.GetType()
            .GetField("totals", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(InventoryManager.Instance) as Dictionary<ItemData, int>)
        {
            EnqueueUpdate(kv.Key);
        }
    }

    private void OnDisable()
    {
        InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged(ItemData item, int newCount)
    {
        EnqueueUpdate(item);
    }

    private void EnqueueUpdate(ItemData item)
    {
        dirtyItems.Add(item);
        if (!refreshScheduled)
        {
            refreshScheduled = true;
            StartCoroutine(DelayedRefresh());
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(debounceTime);

        List<ItemData> batch = new(dirtyItems);
        batch.Sort((a, b) => a.itemName.CompareTo(b.itemName));

        foreach (ItemData item in batch)
        {
            int count = InventoryManager.Instance.GetTotal(item);
            string key = item.itemName;

            if (count <= 0)
            {
                if (slots.ContainsKey(key))
                {
                    InventorySlotUI slot = slots[key];
                    slots.Remove(key);
                    InventorySlotPool.Instance.Release(slot);
                }
            }
            else
            {
                if (!slots.ContainsKey(key))
                {
                    InventorySlotUI slot = InventorySlotPool.Instance.Get();
                    slot.transform.SetParent(content, false);
                    slot.Initialize(item);
                    slots.Add(key, slot);
                }
                slots[key].SetCount(count);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        dirtyItems.Clear();
        refreshScheduled = false;
    }
}