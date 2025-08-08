using System.Collections.Generic;
using UnityEngine;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance { get; private set; }

    private readonly List<Blueprint> blueprints = new();

    private readonly float generateTasksInterval = 2f;
    private float nextGenerateTasksTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    public void RegisterBlueprint(Blueprint blueprint)
    {
        if (!blueprints.Contains(blueprint))
        {
            blueprints.Add(blueprint);
        }
    }

    public void UnregisterBlueprint(Blueprint blueprint)
    {
        blueprints.Remove(blueprint);
    }

    private void Update()
    {
        if (Time.time < nextGenerateTasksTime)
        {
            return;
        }

        nextGenerateTasksTime = Time.time + generateTasksInterval;

        foreach (Blueprint blueprint in blueprints)
        {
            GenerateTasksFor(blueprint);
        }
    }

    private void GenerateTasksFor(Blueprint blueprint)
    {
        foreach (StructureData.ResourceRequirement requirement in blueprint.structureData.requirements)
        {
            int deliveredCount = blueprint.DeliveredCount(requirement.itemData);
            int pendingCount = blueprint.PendingCount(requirement.itemData);
            int totalAssigned = deliveredCount + pendingCount;
            int stillNeeded = requirement.quantity;

            if (stillNeeded <= 0 || pendingCount > 0)
                continue;

            while (totalAssigned < stillNeeded)
            {
                Warehouse warehouse =
                    WarehouseManager.Instance.FindNearestForRetrieve(
                        requirement.itemData,
                        blueprint.transform.position
                    );

                if (warehouse == null)
                {
                    break;
                }

                GameObject retrievedItem = warehouse.RetrieveItem();
                if (retrievedItem == null)
                {
                    //Debug.LogWarning($"[{warehouse.name}] Error en RetrieveItem para {requirement.itemData.itemName}");
                    break;
                }

                MoveItemTask transportTask;
                if (retrievedItem.TryGetComponent<MoveItemTask>(out MoveItemTask existingMoveItemTask))
                {
                    transportTask = existingMoveItemTask;
                }
                else
                {
                    transportTask = retrievedItem.AddComponent<MoveItemTask>();
                }

                transportTask.TaskData = requirement.itemData;

                //blueprint.RegisterPending(requirement.itemData);

                //totalAssigned++;
            }
        }
    }
}