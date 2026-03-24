using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ResourceGatherer : MonoBehaviour
{
    private ResourceGathererData resourceGathererData;

    private float gatheringRadius;

    private readonly HashSet<ResourceInstance> trackedNodes = new();
    private readonly Collider[] scanResults = new Collider[5];
    private int nodeLayerMask;

    private SphereCollider detectionArea;
    private Rigidbody rigidBody;

    private void Awake()
    {
        if (!TryGetComponent<Structure>(out Structure structure))
        {
            Debug.LogError($"{name} missing Structure component");
        }
        if (structure.structureData is ResourceGathererData resourceGathererData)
        {
            this.resourceGathererData = resourceGathererData;

            gatheringRadius = resourceGathererData.gatheringRadius;
        }
        else
        {
            Debug.LogWarning($"{name} couldnt get ResourceGathererData from Structure");
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

    private void Start()
    {
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

        if (!resourceGathererData.allowedCategories.Contains(instance.data.category))
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

    private void OnDisable()
    {
        ResetResourceGatherer();
    }

    private void OnDestroy()
    {
        ResetResourceGatherer();
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