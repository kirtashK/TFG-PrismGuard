using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Blueprint : MonoBehaviour, IItemConsumer
{
    private StructureData data;

    [Tooltip("Spot where delivered items will be put")]
    public Transform dropSpot;

    private readonly Dictionary<ItemData, int> delivered = new();
    private readonly Dictionary<ItemData, int> pending = new();

    private readonly List<GameObject> storedObjects = new();

    public Vector3 GetReceivePosition() => dropSpot.transform.position;

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            data = structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        if (dropSpot == null)
        {
            Debug.LogWarning($"{name} missing {dropSpot.name}");
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());

        delivered.Clear();
        pending.Clear();
        storedObjects.Clear();

        foreach (StructureData.ResourceRequirement requirement in data.buildRequirements)
        {
            delivered[requirement.itemData] = 0;
            pending[requirement.itemData] = 0;
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null)
        {
            yield return null;
        }
        ItemConsumerManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.Unregister(this);
        }
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

    private void TryConstruct()
    {
        foreach (StructureData.ResourceRequirement requirement in data.buildRequirements)
        {
            if (DeliveredCount(requirement.itemData) < requirement.quantity)
            {
                return;
            }
        }

        for (int i = storedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = storedObjects[i];
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        storedObjects.Clear();

        Instantiate(data.builtPrefab, transform.position, transform.rotation);

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
        int needed = data.buildRequirements
                          .Find(required => required.itemData == item).quantity;

        return (have + inFlight) < needed;
    }

    public bool Reserve(ItemData item)
    {
        if (!delivered.ContainsKey(item))
        {
            return false;
        }

        int have = delivered[item];
        int inFlight = pending[item];
        int needed = data.buildRequirements
                          .Find(required => required.itemData == item).quantity;

        if ((have + inFlight) >= needed)
        {
            return false;
        }

        pending[item] = inFlight + 1;
        return true;
    }

    public void Release(ItemData item)
    {
        if (pending.ContainsKey(item))
        {
            pending[item] = Mathf.Max(0, pending[item] - 1);
        }
    }

    public void OnReceived(GameObject itemObj, ItemData itemData)
    {
        Release(itemData);

        delivered[itemData]++;

        if (itemObj.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.SetVisible(true);
        }

        itemObj.transform.SetParent(dropSpot.transform, worldPositionStays: true);
        itemObj.transform.position = GetReceivePosition();

        storedObjects.Add(itemObj);

        TryConstruct();
    }

    public void ConfirmRetrieval(GameObject item)
    {
        // Does nothing as Blueprint doesnt
        // store items to be picked up
    }
}