using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceProcessor : MonoBehaviour, IItemConsumer
{
    [Header("Configuration")]
    [Tooltip("Gameobject where outputs will be stored")]
    public Transform Storage;

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

    private bool isRegistered = false;

    private void Awake()
    {
        InitializeRecipeStates();
    }

    private void InitializeRecipeStates()
    {
        recipeStates = new List<RecipeState>(recipes.Count);
        foreach (ProcessResourceRecipe recipe in recipes)
        {
            recipeStates.Add(new RecipeState
            {
                recipe = recipe,
                storedInput = 0,
                reservedInput = 0,
                storedFuel = 0,
                reservedFuel = 0,
                storedOutput = 0,
                reservedOutput = 0,
                processingCount = 0
            });
        }
    }

    private void OnEnable()
    {
        StopAllCoroutines();

        StartCoroutine(RegisterWhenReady());

        ResetInternalStateAndReleaseReservations();
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null)
        {
            yield return null;
        }
        ItemConsumerManager.Instance.Register(this);
        isRegistered = true;

        StartCoroutine(DispatchOutputToWarehouse());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (isRegistered && ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.Unregister(this);
            isRegistered = false;
        }

        ResetInternalStateAndReleaseReservations();
    }

    private void ResetInternalStateAndReleaseReservations()
    {
        if (recipeStates == null)
        {
            return;
        }

        // Release any reserved input/fuel and internal counters
        foreach (RecipeState state in recipeStates)
        {
            while (state.reservedInput > 0)
            {
                Release(state.recipe.inputItemData);
            }

            if (state.recipe.requiresFuel)
            {
                while (state.reservedFuel > 0)
                {
                    Release(state.recipe.fuelItemData);
                }
            }

            state.reservedOutput = 0;
            state.storedInput = 0;
            state.storedFuel = 0;
            state.storedOutput = 0;
            state.processingCount = 0;
        }
    }

    private void Update()
    {
        StartProcessingBatches();
        
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
                    recipeState.storedInput >= recipe.inputPerBatch
                    && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                        <= recipeState.recipe.outputMaxCapacity
                    && (!recipe.requiresFuel
                        || recipeState.storedFuel >= recipe.fuelPerBatch);
            }
        }
    }

    private IEnumerator ProcessBatch(RecipeState recipeState)
    {
        if (recipeState == null || recipeState.recipe == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(recipeState.recipe.processingTime);

        recipeState.storedOutput += recipeState.recipe.outputPerInput;
        recipeState.reservedOutput -= recipeState.recipe.outputPerInput;
        recipeState.processingCount--;

        Debug.Log($"{name} has processed a batch of {recipeState.recipe.name}" +
            $"\nStored amount = {recipeState.storedOutput}" +
            $"\nMax amount = {recipeState.recipe.outputMaxCapacity}");
    }

    private IEnumerator DispatchOutputToWarehouse()
    {
        const float dispatchInterval = 3.0f;

        while (true)
        {
            yield return new WaitForSeconds(dispatchInterval);

            if (recipeStates == null)
            {
                continue;
            }

            foreach (RecipeState recipeState in recipeStates)
            {
                int remainingOutput = recipeState.storedOutput;
                if (remainingOutput <= 0)
                {
                    continue;
                }

                Transform recipeParent = GetRecipeStorage(recipeState.recipe);

                ItemData itemData = recipeState.recipe.outputItemData;
                GameObject itemPrefab = recipeState.recipe.outputItemData.itemPrefab;

                int currentChildren = recipeParent.childCount;
                int maxCapacity = Mathf.Max(0, recipeState.recipe.outputMaxCapacity);
                int availableSlots = Mathf.Max(0, maxCapacity - currentChildren);
                int outputToCreate = Mathf.Min(remainingOutput, availableSlots);

                while (outputToCreate > 0)
                {
                    GameObject output = ItemManager.Instance.
                        CreateItem(recipeParent.position,
                                    itemPrefab);

                    output.transform.SetParent(recipeParent, worldPositionStays: false);
                    output.transform.localPosition = Vector3.zero;

                    MoveItemTask moveItemTask = output.GetComponent<MoveItemTask>();
                    moveItemTask.TaskData = itemData;

                    recipeState.storedOutput--;
                    outputToCreate--;
                }
            }
        }
    }

    // Returns the storage for a specific recipe,
    // if it doesnt exist it creates it first
    Transform GetRecipeStorage(ProcessResourceRecipe recipe)
    {
        string name = $"Storage_{recipe.outputItemData.itemName}";
        Transform transform = Storage.Find(name);
        if (transform != null)
        {
            return transform;
        }

        // This storage recipe doesnt exist yet, create it
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(Storage, worldPositionStays: false);
        gameObject.transform.localPosition = Vector3.zero;
        return gameObject.transform;
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
            if (recipe.requiresFuel && data == recipe.fuelItemData)
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
            if (recipe.requiresFuel && data == recipe.fuelItemData)
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
            if (recipe.requiresFuel && data == recipe.fuelItemData)
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

                Debug.Log($"{name} stored amount = {state.storedInput}" +
                    $"\nMax amount = {state.recipe.inputMaxCapacity}");

                return;
            }
            if (recipe.requiresFuel && data == recipe.fuelItemData)
            {
                state.storedFuel++;
                Release(data);
                return;
            }
        }
    }

    public void ConfirmRetrieval()
    {

    }
}