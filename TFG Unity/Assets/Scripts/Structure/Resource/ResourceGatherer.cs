using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ResourceGatherer : MonoBehaviour
{
    [HideInInspector] public Structure structure;
    private ResourceGathererData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey gatheringRadiusStat;

    private float gatheringRadius;
    public float GatheringRadius => gatheringRadius;

    [Header("Thresholds")]

    [SerializeField] private List<OutputThresholdEntry> outputThresholds = new();

    [System.Serializable]
    private class OutputThresholdEntry
    {
        public ItemData itemData;

        [Range(1, 100)]
        public int threshold = 10;
    }

    private readonly HashSet<ResourceInstance> trackedNodes = new();
    private readonly Collider[] scanResults = new Collider[5];
    private int nodeLayerMask;

    private SphereCollider detectionArea;
    private Rigidbody rigidBody;

    #region Unity methods

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = (ResourceGathererData)structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        CheckNullStats();

        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }

        nodeLayerMask = 1 << LayerMask.NameToLayer("ResourceNode");

        detectionArea = GetComponent<SphereCollider>();
        if (detectionArea == null)
        {
            detectionArea = gameObject.AddComponent<SphereCollider>();
        }
        detectionArea.isTrigger = true;
        detectionArea.radius = gatheringRadius;

        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null)
        {
            rigidBody = gameObject.AddComponent<Rigidbody>();
        }
        rigidBody.isKinematic = true;
        rigidBody.useGravity = false;
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (StatModifierManager.Instance == null
        || InventoryManager.Instance == null)
        {
            yield return null;
        }

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        ResetResourceGatherer();
    }

    private void OnDestroy()
    {
        ResetResourceGatherer();
    }

    private void Start()
    {
        RefreshStats();        

        structure.currentHealth = structure.maxHealth;

        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 
            gatheringRadius, scanResults, nodeLayerMask);

        for (int i = 0; i < numColliders; i++)
        {
            ResourceInstance instance = scanResults[i].GetComponentInParent<ResourceInstance>();
            if (instance != null)
            {
                TryRegisterInstance(instance);
            }
        }

        RefreshTrackedNodes();
    }

    private void OnTriggerEnter(Collider other)
    {
        ResourceInstance instance = other.GetComponentInParent<ResourceInstance>();
        if (instance != null)
        {
            TryRegisterInstance(instance);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ResourceInstance instance = other.GetComponentInParent<ResourceInstance>();
        if (instance != null)
        {
            TryUnregisterInstance(instance);
        }
    }

    #endregion

    #region Stats

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (healOnWaveCompletedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(healOnWaveCompletedStat)}");
        }
        if (gatheringRadiusStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(gatheringRadiusStat)}");
        }
    }

    void HandleModifiersChanged(string targetId, string statKeyId)
    {
        if (targetId == data.id)
        {
            RefreshStats();
        }
        // Global modifier:
        else if (string.IsNullOrEmpty(targetId))
        {
            RefreshStats();
        }
    }

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out float finalValue))
        {
            structure.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            structure.healOnWaveCompleted = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, gatheringRadiusStat, out finalValue))
        {
            gatheringRadius = finalValue;
        }
    }

    #endregion

    private void HandleInventoryChanged(ItemData itemData, int currentTotal)
    {
        if (itemData == null)
        {
            return;
        }

        if (!HasTrackedNodeWithOutput(itemData))
        {
            return;
        }

        RefreshTrackedNodes();
    }

    private void RefreshTrackedNodes()
    {
        foreach (ResourceInstance instance in trackedNodes)
        {
            if (instance == null)
            {
                continue;
            }

            if (CanMarkInstance(instance))
            {
                instance.AddGatherer(this);
            }
            else
            {
                instance.RemoveGatherer(this);
            }
        }
    }

    private bool CanMarkInstance(ResourceInstance instance)
    {
        if (instance == null || InventoryManager.Instance == null)
        {
            return false;
        }

        if (!instance.TryGetGatherOutputItemData(out ItemData outputItemData))
        {
            return false;
        }

        int threshold = GetThreshold(outputItemData);
        int currentTotal = InventoryManager.Instance.GetTotal(outputItemData);

        return currentTotal < threshold;
    }

    private bool HasTrackedNodeWithOutput(ItemData itemData)
    {
        string itemId = itemData.id;

        foreach (ResourceInstance instance in trackedNodes)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.TryGetGatherOutputItemData(out ItemData outputItemData)
                && outputItemData.id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    #region Thresholds

    public int GetThreshold(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            return int.MaxValue;
        }

        foreach (OutputThresholdEntry entry in outputThresholds)
        {
            if (entry != null && entry.itemData.id == itemId)
            {
                return Mathf.Max(0, entry.threshold);
            }
        }

        return int.MaxValue;
    }

    public void SetThreshold(ItemData itemData, int threshold)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        int normalizedThreshold = Mathf.Max(0, threshold);

        foreach (OutputThresholdEntry entry in outputThresholds)
        {
            if (entry != null && entry.itemData.id == itemId)
            {
                entry.threshold = normalizedThreshold;
                RefreshTrackedNodes();
                return;
            }
        }

        outputThresholds.Add(new OutputThresholdEntry
        {
            itemData = itemData,
            threshold = normalizedThreshold
        });

        RefreshTrackedNodes();
    }

    public IReadOnlyList<ItemData> GetManagedOutputItems()
    {
        Dictionary<string, ItemData> outputItemsById = new();

        foreach (ResourceInstance instance in trackedNodes)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.TryGetGatherOutputItemData(out ItemData itemData)
                && itemData != null && !string.IsNullOrEmpty(itemData.id))
            {
                outputItemsById[itemData.id] = itemData;
            }
        }

        return outputItemsById.Values.ToList();
    }

    #endregion

    private void TryRegisterInstance(ResourceInstance instance)
    {
        if (instance == null || data == null || data.allowedCategories == null)
        {
            return;
        }
        if (!data.allowedCategories.Contains(instance.data.category))
        {
            return;
        }
        if (!trackedNodes.Add(instance))
        {
            return;
        }
        if (!CanMarkInstance(instance))
        {
            return;
        }

        instance.AddGatherer(this);
    }

    private void TryUnregisterInstance(ResourceInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (trackedNodes.Remove(instance))
        {
            instance.RemoveGatherer(this);
        }
    }

    public void NotifyRemovedResourceInstance(ResourceInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        trackedNodes.Remove(instance);
    }

    private void ResetResourceGatherer()
    {
        foreach (ResourceInstance resourceNode in trackedNodes)
        {
            if (resourceNode != null)
            {
                resourceNode.RemoveGathererSilent(this);
            }
        }

        trackedNodes.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gatheringRadius);
    }
}