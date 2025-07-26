using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;

public class ResourceProcessor : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Radius to search input")]
    public float detectionRadius = 10f;
    [Tooltip("Drop spot to generate outputs")]
    public Transform dropSpot;
    [Tooltip("List of recipes this building has availible")]
    public List<ProcessResourceRecipe> recipes;

    // Internal state of each recipe
    private class RecipeState
    {
        public ProcessResourceRecipe recipe;
        public int storedInput;
        public int reservedInput;
        public int storedFuel;
        public int reservedFuel;
        public int storedOutput;
        public int reservedOutput;
        public int processingCount;
    }
    private List<RecipeState> recipeStates;

    private void Awake()
    {
        recipeStates = new List<RecipeState>(recipes.Count);
        foreach (ProcessResourceRecipe recipe in recipes)
        {
            recipeStates.Add(new RecipeState { recipe = recipe });
        }
    }

    private void Update()
    {
        ReserveInputs();
        ReserveFuelFromWarehouse();
        StartProcessingBatches();
        
    }

    private void Start()
    {
        StartCoroutine(DispatchOutputToWarehouse());
    }

    private void ReserveInputs()
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            ProcessResourceRecipe recipe = recipeState.recipe;

            // Skip if we already have enough
            if (recipeState.storedInput + recipeState.reservedInput
                >= recipe.inputMaxCapacity)
            {
                continue;
            }

            // Search closest warehouse that has needed input
            Warehouse sourceWarehouse = WarehouseManager.Instance
                .FindNearestForRetrieve(recipe.inputItemData, transform.position);
            if (sourceWarehouse == null || !sourceWarehouse.CanRetrieve(recipe.inputItemData))
            {
                continue;
            }

            sourceWarehouse.ReserveRetrieveSlot();
            recipeState.reservedInput++;

            GameObject retrievedItem = sourceWarehouse.RetrieveItem();
            if (retrievedItem == null)
            {
                sourceWarehouse.ReleaseStoreReservation();
                recipeState.reservedInput--;
                continue;
            }

            // Delete previous MoveItemTask if it had one
            if (retrievedItem.TryGetComponent<MoveItemTask>(out MoveItemTask existingMoveItemTask))
            {
                Destroy(existingMoveItemTask);
            }
            MoveItemTask moveTask = retrievedItem.AddComponent<MoveItemTask>();
                        
            moveTask.TaskData = recipe.inputItemData;
            moveTask.Destination = transform.position;
            moveTask.OnArrivalCallback = arrivedInput =>
            {
                Destroy(arrivedInput);
                recipeState.storedInput++;
                recipeState.reservedInput--;
                sourceWarehouse.ConfirmRetrieval();

                Debug.Log(name + " received input " + retrievedItem.name
                    + ". Current input amount = " + recipeState.storedInput
                    + ". Max amount of input = " + recipe.inputMaxCapacity);
            };
        }
    }

    private void ReserveFuelFromWarehouse()
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            ProcessResourceRecipe recipe = recipeState.recipe;
            if (!recipe.requiresFuel)
            {
                continue;
            }

            // TODO Test if fuel works properly
            // TODO Test if fuel reservation works properly

            while (recipeState.storedFuel + recipeState.reservedFuel < recipe.fuelPerBatch)
            {
                Warehouse warehouse = WarehouseManager.Instance.FindNearestForRetrieve(
                    recipe.fuelItem,
                    transform.position
                );
                // Couldnt find fuel or its not possible to retrieve from warehouse
                if (warehouse == null || !warehouse.CanRetrieve(recipe.fuelItem))
                {
                    break;
                }

                GameObject fuelObject = warehouse.RetrieveItem();
                if (fuelObject == null)
                {
                    Debug.LogError("Null item extracted: " + fuelObject.name);
                    break;
                }

                recipeState.reservedFuel++;

                MoveItemTask fuelTask = fuelObject.GetComponent<MoveItemTask>()
                               ?? fuelObject.AddComponent<MoveItemTask>();
                fuelTask.TaskData = recipe.fuelItem;
                fuelTask.Destination = transform.position;
                fuelTask.OnArrivalCallback = deliveredFuel =>
                {
                    recipeState.storedFuel++;
                    recipeState.reservedFuel--;
                    Destroy(deliveredFuel.GetComponent<MoveItemTask>());
                };
            }
        }
    }

    private void StartProcessingBatches()
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            ProcessResourceRecipe recipe = recipeState.recipe;

            // Check if there is enough input & storage & fuel if needed:
            bool readyForBatch = 
                recipeState.storedInput >= 1
                && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                    <= recipeState.recipe.outputMaxCapacity
                && (!recipe.requiresFuel
                    || recipeState.storedFuel >= recipe.fuelPerBatch);

            while (readyForBatch
                   && recipeState.processingCount < recipe.maxConcurrentBatches)
            {
                //Debug.Log(name + " procesando batch...");

                // Consume input & fuel
                recipeState.storedInput--;
                if (recipe.requiresFuel)
                {
                    recipeState.storedFuel -= recipe.fuelPerBatch;
                }

                recipeState.reservedOutput += recipe.outputPerInput;

                recipeState.processingCount++;
                StartCoroutine(ProcessBatch(recipeState));

                // Check if another batch is possible
                readyForBatch =
                    recipeState.storedInput >= 1
                    && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                        <= recipeState.recipe.outputMaxCapacity
                    && (!recipe.requiresFuel
                        || recipeState.storedFuel >= recipe.fuelPerBatch);
            }
        }
    }

    /// <summary>
    /// Coroutine that waits processing time then generates output
    /// </summary>
    private IEnumerator ProcessBatch(RecipeState recipeState)
    {
        yield return new WaitForSeconds(recipeState.recipe.processingTime);

        recipeState.storedOutput += recipeState.recipe.outputPerInput;
        recipeState.reservedOutput -= recipeState.recipe.outputPerInput;
        recipeState.processingCount--;
        Debug.Log(name + " has processed a batch of " + recipeState.recipe.name 
            + ". Stored amount = " + recipeState.storedOutput
            + ". Max amount = " + recipeState.recipe.outputMaxCapacity);
    }

    private IEnumerator DispatchOutputToWarehouse()
    {
        yield return new WaitForSeconds(5.0f);

        foreach (RecipeState recipeState in recipeStates)
        {
            int remainingOutput = recipeState.storedOutput;

            while (remainingOutput > 0)
            {
                Warehouse targetWarehouse = WarehouseManager.Instance
                    .FindNearestForStore(dropSpot.position, recipeState.recipe.outputItemData);
                if (targetWarehouse == null)
                {
                    break;
                }

                int outputToSend = Mathf.Min(remainingOutput, targetWarehouse.FreeSlots);
                for (int i = 0; i < outputToSend; i++)
                {
                    GameObject output = ItemManager.Instance.CreateItem(dropSpot.position, recipeState.recipe.outputPrefab);
                    if (output == null)
                    {
                        Debug.LogError(name + ": generated null output");
                    }

                    targetWarehouse.ReserveStoreSlot();

                    MoveItemTask moveOutTask = output.AddComponent<MoveItemTask>();
                    moveOutTask.TaskData = recipeState.recipe.outputItemData;
                    moveOutTask.Destination = targetWarehouse.GetStoragePosition();
                    moveOutTask.OnArrivalCallback = storedItem =>
                    {
                        storedItem.SetActive(true);
                        targetWarehouse.StoreItem(storedItem, recipeState.recipe.outputItemData);
                        Destroy(storedItem.GetComponent<MoveItemTask>());
                    };
                }
                remainingOutput -= outputToSend;
                Debug.Log("RemaningOutput = " + remainingOutput + " & outputToSend = " + outputToSend);
            }
            recipeState.storedOutput = remainingOutput;
        }
        StartCoroutine(DispatchOutputToWarehouse());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}