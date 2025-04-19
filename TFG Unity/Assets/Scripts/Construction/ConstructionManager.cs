using UnityEngine;
using System.Collections.Generic;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager Instance { get; private set; }

    private List<Blueprint> blueprints = new List<Blueprint>();

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
        foreach (var blueprint in blueprints)
        {
            GenerateTasksFor(blueprint);
        }
    }

    private void GenerateTasksFor(Blueprint blueprint)
    {
        foreach (var requirement in blueprint.data.requirements)
        {
            int alreadyDelivered = blueprint.DeliveredCount(requirement.item);
            int remainingNeeded = requirement.quantity - alreadyDelivered;
            
            if (remainingNeeded <= 0)
                continue;

            // Para cada unidad pendiente, creamos una tarea de transporte
            for (int i = 0; i < remainingNeeded; i++)
            {
                Warehouse warehouse =
                    WarehouseManager.Instance.FindNearestForRetrieve(
                        requirement.item,
                        blueprint.transform.position
                        
                    );
                if (warehouse == null)
                    break;

                warehouse.RetrieveItem();

                // Instancia un item invisible que el worker recogerá
                GameObject ghostItem = new GameObject($"Ghost_{requirement.item.itemName}");
                ghostItem.transform.position = warehouse.GetStoragePosition();

                MoveItemTask transportTask = ghostItem.AddComponent<MoveItemTask>();
                transportTask.Destination = blueprint.dropSpot.position;
                //transportTask.InteractionRange = requirement.item.interactionRange;
                transportTask.OnArrivalCallback = deliveredObject =>
                {
                    blueprint.DeliverResource(requirement.item);
                    Destroy(deliveredObject);
                };
            }
        }
    }
}
