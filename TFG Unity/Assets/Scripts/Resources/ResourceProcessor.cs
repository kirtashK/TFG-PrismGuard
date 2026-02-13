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

    [Tooltip("Seconds between checks for processing batches")]
    public float processingCheckInterval = 1f;
    private float processingCheckTimer = 0f;

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

        processingCheckTimer = 0f;
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
        processingCheckTimer -= Time.deltaTime;
        if (processingCheckTimer <= 0f)
        {
            StartProcessingBatches();
            processingCheckTimer = processingCheckInterval;
        }
    }

    private void StartProcessingBatches()
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            // Check if there is enough input & storage & fuel if needed:
            bool readyForBatch =
                recipeState.storedInput >= recipeState.recipe.inputPerBatch
                && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                    <= recipeState.recipe.outputMaxCapacity
                && (!recipeState.recipe.requiresFuel
                    || recipeState.storedFuel >= recipeState.recipe.fuelPerBatch);

            while (readyForBatch
                   && recipeState.processingCount < recipeState.recipe.maxConcurrentBatches)
            {
                // Consume input & fuel
                recipeState.storedInput -= recipeState.recipe.inputPerBatch;
                if (recipeState.recipe.requiresFuel)
                {
                    recipeState.storedFuel -= recipeState.recipe.fuelPerBatch;
                }

                recipeState.reservedOutput += recipeState.recipe.outputPerInput;

                recipeState.processingCount++;
                StartCoroutine(ProcessBatch(recipeState));

                // Check if another batch is possible
                readyForBatch =
                    recipeState.storedInput >= recipeState.recipe.inputPerBatch
                    && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                        <= recipeState.recipe.outputMaxCapacity
                    && (!recipeState.recipe.requiresFuel
                        || recipeState.storedFuel >= recipeState.recipe.fuelPerBatch);
            }
        }
    }

    private IEnumerator ProcessBatch(RecipeState recipeState)
    {
        if (recipeState == null || recipeState.recipe == null)
        {
            yield break;
        }

        Debug.Log($"{name} started processing a batch of {recipeState.recipe.name}. Will finish in {recipeState.recipe.processingTime} seconds");

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

                int currentChildren = recipeParent.childCount;
                int maxCapacity = Mathf.Max(0, recipeState.recipe.outputMaxCapacity);
                int availableSlots = Mathf.Max(0, maxCapacity - currentChildren);
                int outputToCreate = Mathf.Min(remainingOutput, availableSlots);

                while (outputToCreate > 0)
                {
                    GameObject output = ItemManager.Instance.
                        CreateItem(recipeParent.position,
                                    itemData.itemPrefab);

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
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData)
            {
                if (recipeState.storedInput + recipeState.reservedInput < recipeState.recipe.inputMaxCapacity)
                {
                    return true;
                }
            }
            if (recipeState.recipe.requiresFuel && data == recipeState.recipe.fuelItemData)
            {
                if (recipeState.storedFuel + recipeState.reservedFuel < recipeState.recipe.fuelMaxCapacity)
                {
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Can receive {data.itemName}. Stored {recipeState.storedFuel} + Reserved {recipeState.reservedFuel} < Max {recipeState.recipe.fuelMaxCapacity}");
                    return true;
                }
            }
        }
        return false;
    }

    public bool Reserve(ItemData data)
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData)
            {
                if (recipeState.storedInput + recipeState.reservedInput < recipeState.recipe.inputMaxCapacity)
                {
                    recipeState.reservedInput++;
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Reserved {data.itemName} [{recipeState.reservedInput}]");

                    return true;
                }
            }
            if (recipeState.recipe.requiresFuel && data == recipeState.recipe.fuelItemData)
            {
                if (recipeState.storedFuel + recipeState.reservedFuel < recipeState.recipe.fuelMaxCapacity)
                {
                    recipeState.reservedFuel++;
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Reserved {data.itemName} [{recipeState.reservedFuel}]");

                    return true;
                }
            }
        }
        return false;
    }

    public void Release(ItemData data)
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData)
            {
                if (recipeState.reservedInput > 0)
                {
                    recipeState.reservedInput--;
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Released {data.itemName} [{recipeState.reservedInput}]");

                    return;
                }
            }
            if (recipeState.recipe.requiresFuel && data == recipeState.recipe.fuelItemData)
            {
                if (recipeState.reservedFuel > 0)
                {
                    recipeState.reservedFuel--;
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Released {data.itemName} [{recipeState.reservedFuel}]");

                    return;
                }
            }
        }
    }
    public Vector3 GetReceivePosition() => transform.position;

    public void OnReceived(GameObject item, ItemData data)
    {
        Destroy(item);

        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData)
            {
                if (recipeState.storedInput < recipeState.recipe.inputMaxCapacity)
                {
                    recipeState.storedInput++;
                    Release(data);
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Input {data.itemName} stored amount = {recipeState.storedInput}/{recipeState.recipe.inputMaxCapacity}");
                    
                    return;
                }
            }
            if (recipeState.recipe.requiresFuel && data == recipeState.recipe.fuelItemData)
            {
                if (recipeState.storedFuel < recipeState.recipe.fuelMaxCapacity)
                {
                    recipeState.storedFuel++;
                    Release(data);
                    //Debug.Log($"{name}: Recipe {recipeState.recipe.recipeName}: Fuel {data.itemName} stored amount = {recipeState.storedFuel}/{recipeState.recipe.fuelMaxCapacity}");
                    
                    return;
                }
            }
        }
    }

    public void ConfirmRetrieval()
    {

    }
}