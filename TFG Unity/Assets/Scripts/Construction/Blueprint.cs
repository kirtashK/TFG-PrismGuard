using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Blueprint : MonoBehaviour
{
    public StructureData data;

    [Tooltip("Spot donde dejar los recursos")]
    public Transform dropSpot;

    private readonly Dictionary<ItemData, int> delivered = new();
    private readonly Dictionary<ItemData, int> pending = new();

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());

        delivered.Clear();
        pending.Clear();
        foreach (StructureData.ResourceRequirement requirement in data.requirements)
        {
            delivered[requirement.itemData] = 0;
            pending[requirement.itemData] = 0;
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ConstructionManager.Instance == null)
        {
            yield return null;
        }
        ConstructionManager.Instance.RegisterBlueprint(this);
    }

    private void OnDisable()
    {
        ConstructionManager.Instance.UnregisterBlueprint(this);
    }

    private void Awake()
    {
        TryConstruct();
    }

    // Llamado cuando llega un recurso mediante callbacks genéricos
    public void DeliverResource(ItemData item, GameObject itemObject)
    {
        pending[item]--;
        delivered[item]++;

        itemObject.transform.SetParent(transform, worldPositionStays: true);
        itemObject.transform.position = dropSpot.position;

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
        foreach (StructureData.ResourceRequirement requirement in data.requirements)
        {
            if (DeliveredCount(requirement.itemData) < requirement.quantity)
                return;
        }

        Instantiate(data.builtPrefab, transform.position, transform.rotation);

        // Destruye el blueprint y todos los recursos entregados
        Destroy(gameObject);
    }
}
