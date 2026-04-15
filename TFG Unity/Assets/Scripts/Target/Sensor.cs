using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class Sensor : MonoBehaviour
{
    private readonly HashSet<ITarget> targets = new();

    private SphereCollider sphereCollider;
    private Rigidbody rigidBody;

    private Transform root;
    private Faction faction = Faction.Neutral;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();

        rigidBody = GetComponent<Rigidbody>();

        sphereCollider.isTrigger = true;

        rigidBody.useGravity = false;
        rigidBody.isKinematic = true;
    }

    public void Initialize(Transform ownerRoot, Faction ownerFaction, float aggroRadius)
    {
        root = ownerRoot;
        faction = ownerFaction;

        SetRadius(aggroRadius);
    }

    public void SetRadius(float newRadius)
    {
        sphereCollider.radius = Mathf.Max(0.1f, newRadius);
    }

    public List<ITarget> GetSortedTargets(Vector3 origin, bool prioritizeUnits = false)
    {
        CleanupInvalidTargets();
        return TargetSearchUtility.SortTargets(targets, origin, prioritizeUnits);
    }

    public ITarget GetBestTarget(Vector3 origin, ITarget crystalFallback = null)
    {
        CleanupInvalidTargets();
        return TargetSearchUtility.GetBestTarget(targets, origin, faction, crystalFallback);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryAddTarget(other);
    }

    private void OnTriggerExit(Collider other)
    {
        TryRemoveTarget(other);
    }

    private void TryAddTarget(Collider other)
    {
        if (other == null || other.isTrigger)
        {
            return;
        }
        if (root != null && other.transform.IsChildOf(root))
        {
            return;
        }
        ITarget target = other.GetComponentInParent<ITarget>();
        if (target == null || !target.IsAlive)
        {
            return;
        }
        if (target.Faction == faction)
        {
            return;
        }

        if (targets.Add(target))
        {
            target.OnDeathStartedEvent += HandleTargetDeathStarted;
        }
    }

    private void TryRemoveTarget(Collider other)
    {
        if (other == null || other.isTrigger)
        {
            return;
        }
        ITarget target = other.GetComponentInParent<ITarget>();

        RemoveTarget(target);
    }

    private void RemoveTarget(ITarget target)
    {
        if (target == null)
        {
            return;
        }

        if (targets.Remove(target))
        {
            target.OnDeathStartedEvent -= HandleTargetDeathStarted;
        }
    }

    private void HandleTargetDeathStarted(ITarget deadTarget)
    {
        RemoveTarget(deadTarget);
    }

    private void CleanupInvalidTargets()
    {
        List<ITarget> targetsToRemove = null;

        foreach (ITarget target in targets)
        {
            if (target == null || !target.IsAlive || target.Faction == faction)
            {
                targetsToRemove ??= new List<ITarget>();
                targetsToRemove.Add(target);
            }
        }

        if (targetsToRemove == null)
        {
            return;
        }

        for (int i = 0; i < targetsToRemove.Count; i++)
        {
            targets.Remove(targetsToRemove[i]);
        }
    }

    private void OnDisable()
    {
        targets.Clear();
    }
}