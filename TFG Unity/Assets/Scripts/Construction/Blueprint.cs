using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Blueprint : MonoBehaviour
{
    public StructureData data;

    [Tooltip("Spot donde dejar los recursos")]
    public Transform dropSpot;

    private readonly Dictionary<ItemData, int> delivered = new Dictionary<ItemData, int>();
    private readonly Dictionary<ItemData, int> pending = new Dictionary<ItemData, int>();

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());

        delivered.Clear();
        pending.Clear();
        foreach (var requirement in data.requirements)
        {
            delivered[requirement.item] = 0;
            pending[requirement.item] = 0;
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

    // Llamado cuando llega un recurso
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
            return count;
        return 0;
    }

    public int PendingCount(ItemData item)
    {
        if (pending.TryGetValue(item, out var count))
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
        foreach (var requirement in data.requirements)
        {
            if (DeliveredCount(requirement.item) < requirement.quantity)
                return;
        }


        // Construir una vez tiene todos los recursos
        Instantiate(data.builtPrefab, transform.position, transform.rotation);
        // Destruir el blueprint y los recursos entregados
        Destroy(gameObject);
    }
}
