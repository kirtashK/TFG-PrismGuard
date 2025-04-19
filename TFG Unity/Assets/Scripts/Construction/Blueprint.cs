using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Blueprint : MonoBehaviour
{
    public StructureData data;

    [Tooltip("Spot donde dejar los recursos")]
    public Transform dropSpot;

    private Dictionary<ItemData, int> delivered = new Dictionary<ItemData, int>();

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());

        delivered.Clear();
        foreach (var req in data.requirements)
        {
            delivered[req.item] = 0;
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
    public void DeliverResource(ItemData item)
    {
        delivered[item]++;
        TryConstruct();
    }

    public int DeliveredCount(ItemData item)
    {
        if (delivered.TryGetValue(item, out int count))
            return count;
        return 0;
    }

    private void TryConstruct()
    {
        foreach (var req in data.requirements)
        {
            if (delivered[req.item] < req.quantity)
                return;
        }

        // Construir una vez tiene todos los recursos
        Instantiate(data.builtPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}
