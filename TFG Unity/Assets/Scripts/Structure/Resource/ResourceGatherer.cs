using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    private readonly HashSet<ResourceInstance> trackedNodes = new();
    private readonly Collider[] scanResults = new Collider[5];
    private int nodeLayerMask;

    private SphereCollider detectionArea;
    private Rigidbody rigidBody;

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
        while (StatModifierManager.Instance == null)
        {
            yield return null;
        }

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
    }

    private void OnDisable()
    {
        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
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

        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position, 
            gatheringRadius, 
            scanResults, 
            nodeLayerMask);

        for (int i = 0; i < numColliders; i++)
        {
            ResourceInstance instance = scanResults[i]
                .GetComponentInParent<ResourceInstance>();
            if (instance != null)
            {
                TryRegisterInstance(instance);
            }
        }
    }

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

    private void TryRegisterInstance(ResourceInstance instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!data.allowedCategories.Contains(instance.data.category))
        {
            return;
        }

        if (trackedNodes.Add(instance))
        {
            instance.AddGatherer(this);
        }
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