using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Blueprint : MonoBehaviour, IItemConsumer
{
    public StructureData structureData;

    [Tooltip("Spot where delivered items will be put")]
    public Transform dropSpot;

    private readonly Dictionary<ItemData, int> delivered = new();
    private readonly Dictionary<ItemData, int> pending = new();

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());

        delivered.Clear();
        pending.Clear();
        foreach (StructureData.ResourceRequirement requirement in structureData.requirements)
        {
            delivered[requirement.itemData] = 0;
            pending[requirement.itemData] = 0;
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ConstructionManager.Instance == null
            || ItemConsumerManager.Instance == null)
        {
            yield return null;
        }
        ConstructionManager.Instance.RegisterBlueprint(this);
        ItemConsumerManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        ConstructionManager.Instance.UnregisterBlueprint(this);
        ItemConsumerManager.Instance.Unregister(this);
    }

    private void Start()
    {
        TryConstruct();
    }

    public int DeliveredCount(ItemData item)
    {
        if (delivered.TryGetValue(item, out int count))
        {
            return count;
        }
        return 0;
    }

    public int PendingCount(ItemData item)
    {
        if (pending.TryGetValue(item, out int count))
        {
            return count;
        }
        return 0;
    }

    public void RegisterPending(ItemData item)
    {
        pending[item]++;
    }

    private void TryConstruct()
    {
        foreach (StructureData.ResourceRequirement requirement in structureData.requirements)
        {
            if (DeliveredCount(requirement.itemData) < requirement.quantity)
                return;
        }

        Instantiate(structureData.builtPrefab, transform.position, transform.rotation);

        // Destroy the blueprint and all delivered items
        Destroy(gameObject);
    }

    public bool CanReceive(ItemData item)
    {
        if (!delivered.ContainsKey(item))
        {
            return false;
        }

        int have = delivered[item];
        int inFlight = pending[item];
        int needed = structureData.requirements
                          .Find(required => required.itemData == item).quantity;
        return (have + inFlight) < needed;
    }

    public bool Reserve(ItemData item)
    {
        if (!CanReceive(item))
        {
            return false;
        }

        pending[item]++;
        return true;
    }

    public void Release(ItemData item)
    {
        if (pending.ContainsKey(item))
        {
            pending[item] = Mathf.Max(0, pending[item] - 1);
        }
    }

    public Vector3 GetReceivePosition()
    {
        return dropSpot != null
            ? dropSpot.position
            : transform.position;
    }

    public void OnReceived(GameObject itemObj, ItemData item)
    {
        Release(item);

        delivered[item]++;

        itemObj.transform.SetParent(transform, worldPositionStays: true);
        itemObj.transform.position = GetReceivePosition();

        TryConstruct();
    }

    public void ConfirmRetrieval()
    {
        // Does nothing as Blueprint doesnt
        // store items to be picked up
    }
}