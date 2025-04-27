using UnityEngine;
using System.Collections.Generic;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance { get; private set; }

    private List<Blueprint> blueprints = new List<Blueprint>();

    private float generateTasksInterval = 2f;
    private float nextGenerateTasksTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterBlueprint(Blueprint blueprint)
    {
        if (!blueprints.Contains(blueprint))
            blueprints.Add(blueprint);
    }

    public void UnregisterBlueprint(Blueprint blueprint)
    {
        blueprints.Remove(blueprint);
    }

    private void Update()
    {
        if (Time.time >= nextGenerateTasksTime)
        {
            nextGenerateTasksTime = Time.time + generateTasksInterval;

            foreach (Blueprint blueprint in blueprints)
            {
                GenerateTasksFor(blueprint);
            }
        }
        
    }

    private void GenerateTasksFor(Blueprint blueprint)
    {
        foreach (StructureData.ResourceRequirement requirement in blueprint.data.requirements)
        {
            int deliveredCount = blueprint.DeliveredCount(requirement.item);
            int pendingCount = blueprint.PendingCount(requirement.item);
            int totalAssigned = deliveredCount + pendingCount;
            int stillNeeded = requirement.quantity;

            if (stillNeeded <= 0 || pendingCount > 0)
                continue;

            while (totalAssigned < stillNeeded)
            {
                Warehouse warehouse =
                    WarehouseManager.Instance.FindNearestForRetrieve(
                        requirement.item,
                        blueprint.transform.position
                    );

                if (warehouse == null)
                {
                    break;
                }

                GameObject itemObject = warehouse.RetrieveItem();
                if (itemObject == null)
                {
                    Debug.LogWarning($"[{warehouse.name}] Error en RetrieveItem para {requirement.item.itemName}");
                    return;
                }

                MoveItemTask transportTask = itemObject.GetComponent<MoveItemTask>()
                                                ?? itemObject.AddComponent<MoveItemTask>();

                transportTask.TaskData = requirement.item;
                transportTask.Destination = blueprint.dropSpot.position;
                transportTask.OnArrivalCallback = deliveredObject =>
                {
                    blueprint.DeliverResource(requirement.item, deliveredObject);
                    Destroy(deliveredObject.GetComponent<MoveItemTask>());
                    deliveredObject.SetActive(true);
                };

                blueprint.RegisterPending(requirement.item);

                totalAssigned++;
            }
        }
    }
}
