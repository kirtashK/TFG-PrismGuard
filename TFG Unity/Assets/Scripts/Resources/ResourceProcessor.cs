using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceProcessor : MonoBehaviour, IItemConsumer
{
    [HideInInspector] public Structure structure;
    private ProcessorData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey maxConcurrentBatchesStat;
    [SerializeField] private StatKey processingSpeedStat;

    private float fuelMaxCapacity;
    private bool requiresFuel;

    [Tooltip("Gameobject where outputs will be stored")]
    public Transform storage;

    [Tooltip("Seconds between checks for processing batches")]
    public float processingCheckInterval = 1f;
    private float processingCheckTimer = 0f;

    private readonly Dictionary<ItemData, InputState> inputState = new();

    [Header("Debug")]

    [SerializeField]
    private float storedFuel = 0;
    [SerializeField]
    private float reservedFuel = 0;
    [SerializeField]
    private int maxConcurrentBatches = 1;
    [SerializeField]
    private int processingCount = 0;
    [SerializeField] 
    private float processingSpeed = 1;

    public Vector3 GetReceivePosition() => transform.position;

    private class InputState
    {
        public int Stored;
        public int Reserved;
        public int MaxCapacity;

        public InputState(int capacity)
        {
            MaxCapacity = Mathf.Max(0, capacity);
            Stored = 0;
            Reserved = 0;
        }
    }

    // Internal state of each recipe
    private class RecipeState
    {
        public ProcessResourceRecipe recipe;

        public int storedOutput;
        public int reservedOutput;
    }
    private List<RecipeState> recipeStates;

    private bool isRegistered = false;

    private void Awake()
    {
        if (TryGetComponent<Structure>(out Structure structure))
        {
            this.structure = structure;
            data = (ProcessorData)structure.structureData;
        }
        else
        {
            Debug.LogError($"{name}: missing {nameof(structure)}");
        }

        CheckNullStats();

        if (storage == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(storage)}");
        }
        if (data == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(data)}");
        }

        InitializeRecipeStates();
    }

    private void InitializeRecipeStates()
    {
        inputState.Clear();

        recipeStates = new List<RecipeState>(data.recipes.Count);
        foreach (ProcessResourceRecipe recipe in data.recipes)
        {
            recipeStates.Add(new RecipeState
            {
                recipe = recipe,
                storedOutput = 0,
                reservedOutput = 0
            });

            ItemData key = recipe.inputItemData;
            if (!inputState.ContainsKey(key))
            {
                inputState[key] = new InputState(recipe.inputPerBatch);
            }
            // If ItemData already exists, keep MaxCapacity as the maximun input needed 
            else
            {
                inputState[key].MaxCapacity = Mathf.Max(inputState[key].MaxCapacity, recipe.inputPerBatch);
            }
        }
    }

    private void Start()
    {
        RefreshStats();

        structure.currentHealth = structure.maxHealth;

        fuelMaxCapacity = data.fuelMaxCapacity;
        requiresFuel = data.requiresFuel;
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
        while (ItemConsumerManager.Instance == null
            || StatModifierManager.Instance == null)
        {
            yield return null;
        }
        ItemConsumerManager.Instance.Register(this);
        isRegistered = true;

        structure.OnDeathStartedEvent += OnDeathStarted;
        structure.OnDeathCleanupEvent += OnDeathCleanup;
        structure.OnDamageTakenEvent += OnDamageTaken;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;

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

        if (StatModifierManager.Instance != null)
        {
            StatModifierManager.Instance.OnModifiersChanged -= HandleModifiersChanged;
        }

        structure.OnDeathStartedEvent -= OnDeathStarted;
        structure.OnDeathCleanupEvent -= OnDeathCleanup;
        structure.OnDamageTakenEvent -= OnDamageTaken;

        ResetInternalStateAndReleaseReservations();
    }

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (maxConcurrentBatchesStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxConcurrentBatchesStat)}");
        }
        if (processingSpeedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(processingSpeedStat)}");
        }
    }

    void HandleModifiersChanged(string targetId, string statKeyId)
    {
        if (targetId == data.id)
        {
            RefreshStats();
        }
        // Global modifier:
        else if (string.IsNullOrEmpty(targetId))
        {
            RefreshStats();
        }
    }

    public void RefreshStats()
    {
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxHealthStat, out float finalValue))
        {
            structure.maxHealth = finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, maxConcurrentBatchesStat, out finalValue))
        {
            maxConcurrentBatches = (int)finalValue;
        }
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, processingSpeedStat, out finalValue))
        {
            processingSpeed = finalValue;
        }
    }

    private void OnDamageTaken(float amount, Vector3 attackOrigin)
    {

    }

    private void OnDeathStarted()
    {

    }

    private void OnDeathCleanup()
    {

    }

    /// <summary>
    /// Releases reservations and restores values to initial state
    /// </summary>
    private void ResetInternalStateAndReleaseReservations()
    {
        if (recipeStates == null)
        {
            return;
        }

        reservedFuel = 0;
        storedFuel = 0;

        processingCount = 0;

        foreach (KeyValuePair<ItemData, InputState> input in inputState)
        {
            while (input.Value.Reserved > 0)
            {
                Release(input.Key);
            }
            input.Value.Stored = 0;
        }

        foreach (RecipeState state in recipeStates)
        {
            state.reservedOutput = 0;
            state.storedOutput = 0;
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
        if (recipeStates == null || recipeStates.Count == 0) { return; }

        foreach (RecipeState recipeState in recipeStates)
        {
            if (!inputState.TryGetValue(recipeState.recipe.inputItemData, out InputState state))
            {
                Debug.LogError($"{name} couldnt get value of {nameof(InputState)} with key {recipeState.recipe.inputItemData}");
                return;
            }

            // Check if there is enough input & storage & fuel if needed:
            bool readyForBatch =
                state.Stored >= recipeState.recipe.inputPerBatch
                && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                    <= recipeState.recipe.outputMaxCapacity
                && (!requiresFuel || storedFuel >= recipeState.recipe.fuelPerBatch);

            while (readyForBatch && processingCount < maxConcurrentBatches)
            {
                // Consume input & fuel
                state.Stored -= recipeState.recipe.inputPerBatch;
                if (requiresFuel)
                {
                    storedFuel -= recipeState.recipe.fuelPerBatch;
                }

                recipeState.reservedOutput += recipeState.recipe.outputPerInput;

                processingCount++;
                StartCoroutine(ProcessBatch(recipeState));

                // Check if another batch is possible
                readyForBatch =
                    state.Stored >= recipeState.recipe.inputPerBatch
                    && recipeState.storedOutput + recipeState.recipe.outputPerInput + recipeState.reservedOutput
                        <= recipeState.recipe.outputMaxCapacity
                    && (!requiresFuel || storedFuel >= recipeState.recipe.fuelPerBatch);
            }
        }
    }

    private IEnumerator ProcessBatch(RecipeState recipeState)
    {
        if (recipeState == null || recipeState.recipe == null)
        {
            yield break;
        }

        float processingTime = recipeState.recipe.processingTime * processingSpeed;

        Debug.Log($"{name} started processing a batch of {recipeState.recipe.name}. " +
            $"Will finish in {processingTime} seconds. " +
            $"Consumed input: {recipeState.recipe.inputPerBatch}. " +
            $"Consumed fuel [{requiresFuel}]: {recipeState.recipe.fuelPerBatch}.");

        yield return new WaitForSeconds(processingTime);

        recipeState.storedOutput += recipeState.recipe.outputPerInput;
        recipeState.reservedOutput -= recipeState.recipe.outputPerInput;
        processingCount--;

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
                    GameObject output = ItemManager.Instance.CreateItem(recipeParent.position, itemData.itemPrefab);

                    output.transform.SetParent(recipeParent, worldPositionStays: false);
                    output.transform.localPosition = Vector3.zero;

                    if (!output.TryGetComponent<MoveItemTask>(out MoveItemTask moveItemTask))
                    {
                        Debug.LogError($"{name}: null {nameof(MoveItemTask)}");
                    }
                    moveItemTask.TaskData = itemData;

                    recipeState.storedOutput--;
                    outputToCreate--;
                }
            }
        }
    }

    /// <summary>
    /// Returns the storage for a specific recipe,
    /// if it doesnt exist it creates it first
    /// </summary>
    /// <param name="recipe"></param>
    /// <returns>Transform of the storage</returns>
    private Transform GetRecipeStorage(ProcessResourceRecipe recipe)
    {
        string name = $"Storage_{recipe.outputItemData.Name}";
        Transform transform = storage.Find(name);
        if (transform != null)
        {
            return transform;
        }

        // This storage recipe doesnt exist yet, create it
        GameObject gameObject = new(name);
        gameObject.transform.SetParent(storage, worldPositionStays: false);
        gameObject.transform.localPosition = Vector3.zero;
        return gameObject.transform;
    }

    public bool CanReceive(ItemData data)
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData && inputState.TryGetValue(data, out InputState state))
            {
                if (state.Stored + state.Reserved < state.MaxCapacity)
                {
                    //Debug.Log($"{name}: Can receive input {data.itemName}. Stored {state.Stored} + Reserved {state.Reserved} < Max {state.MaxCapacity}");
                    return true;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (storedFuel + reservedFuel + data.fuelValue <= fuelMaxCapacity)
            {
                //Debug.Log($"{name}: Can receive fuel {data.itemName}. Stored {storedFuel} + Reserved {reservedFuel} + fuelValue {data.fuelValue} <= Max {fuelMaxCapacity}");
                return true;
            }
        }
        return false;
    }

    public bool Reserve(ItemData data)
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData && inputState.TryGetValue(data, out InputState state))
            {
                if (state.Stored + state.Reserved < state.MaxCapacity)
                {
                    state.Reserved++;
                    //Debug.Log($"{name}: Reserved input {data.itemName}. Stored {state.Stored} + Reserved {state.Reserved} < Max {state.MaxCapacity}");

                    return true;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (storedFuel + reservedFuel + data.fuelValue <= fuelMaxCapacity)
            {
                reservedFuel = Mathf.Min(fuelMaxCapacity, reservedFuel + data.fuelValue);
                //Debug.Log($"{name}: Reserved fuel {data.itemName} [{reservedFuel}]");

                return true;
            }
        }
        return false;
    }

    public void OnReceived(GameObject item, ItemData data)
    {
        Destroy(item);

        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData && inputState.TryGetValue(data, out InputState state))
            {
                if (state.Stored < state.MaxCapacity)
                {
                    state.Stored++;
                    Release(data);
                    //Debug.Log($"{name}: Received input {data.itemName}. Stored = {state.Stored}. Max = {state.MaxCapacity}");

                    return;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (storedFuel < fuelMaxCapacity)
            {
                storedFuel = Mathf.Min(storedFuel + data.fuelValue, fuelMaxCapacity);
                Release(data);
                //Debug.Log($"{name}: Received fuel {data.itemName}. Stored = {storedFuel}. Max = {fuelMaxCapacity}");

                return;
            }
        }
    }

    public void Release(ItemData data)
    {
        foreach (RecipeState recipeState in recipeStates)
        {
            if (data == recipeState.recipe.inputItemData && inputState.TryGetValue(data, out InputState state))
            {
                if (state.Reserved > 0)
                {
                    state.Reserved--;
                    //Debug.Log($"{name}: Released input {data.itemName}. Still reserved: {state.Reserved}");

                    return;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (reservedFuel > 0)
            {
                reservedFuel = Mathf.Max(0, reservedFuel - data.fuelValue);
                //Debug.Log($"{name}: Released fuel {data.itemName}, still reserved: [{reservedFuel}]");

                return;
            }
        }
    }

    public void ConfirmRetrieval(ItemData item)
    {

    }
}