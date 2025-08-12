using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ResourceGatherer : MonoBehaviour
{
    [Tooltip("This gatherer will mark only the choosen categories")]
    public List<ResourceCategory> allowedCategories;

    [Tooltip("Detection radius")]
    public float detectionRadius = 10f;

    private readonly HashSet<ResourceInstance> trackedNodes = new();
    private readonly Collider[] scanResults = new Collider[5];
    private int nodeLayerMask;

    private SphereCollider sphereCollider;
    private Rigidbody rb;

    private void Awake()
    {
        nodeLayerMask = 1 << LayerMask.NameToLayer("ResourceNode");

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }
        sphereCollider.isTrigger = true;
        sphereCollider.radius = detectionRadius;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Start()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position, 
            detectionRadius, 
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

        if (!allowedCategories.Contains(instance.category))
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
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}