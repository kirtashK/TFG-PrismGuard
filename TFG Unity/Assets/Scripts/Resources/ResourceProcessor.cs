using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;

public class ResourceProcessor : MonoBehaviour, IItemConsumer
{
    [Header("Configuration")]
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
        StartCoroutine(RegisterWhenReady());

        recipeStates = new List<RecipeState>(recipes.Count);
        foreach (ProcessResourceRecipe recipe in recipes)
        {
            recipeStates.Add(new RecipeState { recipe = recipe });
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null)
        {
            yield return null;
        }
        ItemConsumerManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.Unregister(this);
        }
    }

    private void Update()
    {
        StartProcessingBatches();
        
    }

    private void Start()
    {
        StartCoroutine(DispatchOutputToWarehouse());
    }

    private void StartProcessingBatches()
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            ProcessResourceRecipe recipe = recipeState.recipe;

            // Check if there is enough input & storage & fuel if needed:
            bool readyForBatch =
                recipeState.storedInput >= recipe.inputPerBatch
                && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                    <= recipeState.recipe.outputMaxCapacity
                && (!recipe.requiresFuel
                    || recipeState.storedFuel >= recipe.fuelPerBatch);

            while (readyForBatch
                   && recipeState.processingCount < recipe.maxConcurrentBatches)
            {
                //Debug.Log(name + " procesando batch...");

                // Consume input & fuel
                recipeState.storedInput -= recipe.inputPerBatch;
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
        yield return new WaitForSeconds(3.0f);

        foreach (RecipeState recipeState in recipeStates)
        {
            int remainingOutput = recipeState.storedOutput;
            ItemData data = recipeState.recipe.outputItemData;
            GameObject prefab = recipeState.recipe.outputPrefab;

            while (remainingOutput > 0)
            {
                GameObject output = ItemManager.Instance.CreateItem(
                    dropSpot.position,
                    prefab);

                MoveItemTask move = output.GetComponent<MoveItemTask>()
                           ?? output.AddComponent<MoveItemTask>();
                move.TaskData = data;

                recipeState.storedOutput--;
                remainingOutput--;
            }
        }
        StartCoroutine(DispatchOutputToWarehouse());
    }

    public bool CanReceive(ItemData data)
    {
        foreach (RecipeState state in recipeStates)
        {
            ProcessResourceRecipe recipe = state.recipe;
            if (data == recipe.inputItemData)
            {
                if (state.storedInput + state.reservedInput < recipe.inputMaxCapacity)
                {
                    return true;
                }
            }
            if (recipe.requiresFuel && data == recipe.fuelItem)
            {
                if (state.storedFuel + state.reservedFuel < recipe.fuelPerBatch)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool Reserve(ItemData data)
    {
        foreach (RecipeState state in recipeStates)
        {
            ProcessResourceRecipe recipe = state.recipe;
            if (data == recipe.inputItemData)
            {
                state.reservedInput++;
                return true;
            }
            if (recipe.requiresFuel && data == recipe.fuelItem)
            {
                state.reservedFuel++;
                return true;
            }
        }
        return false;
    }

    public void Release(ItemData data)
    {
        foreach (RecipeState state in recipeStates)
        {
            ProcessResourceRecipe recipe = state.recipe;
            if (data == recipe.inputItemData)
            {
                state.reservedInput = Mathf.Max(0, state.reservedInput - 1);
                return;
            }
            if (recipe.requiresFuel && data == recipe.fuelItem)
            {
                state.reservedFuel = Mathf.Max(0, state.reservedFuel - 1);
                return;
            }
        }
    }
    public Vector3 GetReceivePosition() => transform.position;

    public void OnReceived(GameObject item, ItemData data)
    {
        Destroy(item);

        foreach (RecipeState state in recipeStates)
        {
            ProcessResourceRecipe recipe = state.recipe;
            if (data == recipe.inputItemData)
            {
                state.storedInput++;
                Release(data);
                return;
            }
            if (recipe.requiresFuel && data == recipe.fuelItem)
            {
                state.storedFuel++;
                Release(data);
                return;
            }
        }
    }
}