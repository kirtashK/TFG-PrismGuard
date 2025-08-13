using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ResourceInstance : MonoBehaviour
{
    public ResourceData data;
    public ResourceCategory category;

    private GatherResourceTask gatherTask;

    // ResourceGatherers currently covering this resource
    private readonly HashSet<ResourceGatherer> coveringGatherers = new();

    private void Awake()
    {
        gatherTask = GetComponent<GatherResourceTask>();

        if (gatherTask == null)
        {
            Debug.LogWarning("ResourceInstance " + name + " is missing GatherResourceTask");
        }
    }

    private void OnEnable()
    {
        // Enable if covered by at least 1 ResourceGatherer
        if (gatherTask != null)
        {
            gatherTask.enabled = coveringGatherers.Count > 0;
        }
    }

    private void OnDestroy()
    {
        foreach (ResourceGatherer gatherer in coveringGatherers)
        {
            if (gatherer != null)
            {
                gatherer.NotifyRemovedResourceInstance(this);
            }
        }
        coveringGatherers.Clear();
    }

    public void AddGatherer(ResourceGatherer gatherer)
    {
        if (gatherer == null)
        {
            return;
        }

        bool wasAdded = coveringGatherers.Add(gatherer);
        if (wasAdded && gatherTask != null && coveringGatherers.Count == 1)
        {
            gatherTask.enabled = true;
        }
    }

    public void RemoveGatherer(ResourceGatherer gatherer)
    {
        if (gatherer == null)
        {
            return;
        }

        bool wasRemoved = coveringGatherers.Remove(gatherer);
        if (wasRemoved && gatherTask != null && coveringGatherers.Count == 0)
        {
            gatherTask.enabled = false;
        }
    }

    public bool IsCovered()
    {
        return coveringGatherers.Count > 0;
    }

    public void RemoveGathererSilent(ResourceGatherer gatherer)
    {
        if (gatherer == null)
        {
            return;
        }

        coveringGatherers.Remove(gatherer);
        if (gatherTask != null && coveringGatherers.Count == 0)
        {
            gatherTask.enabled = false;
        }
    }
}