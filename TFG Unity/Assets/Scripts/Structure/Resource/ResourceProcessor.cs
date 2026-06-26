using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceProcessor : MonoBehaviour, IItemConsumer, IThresholdProvider
{
    [HideInInspector] public Structure structure;
    private ProcessorData data;

    [Header("Stats")]

    [SerializeField] private StatKey maxHealthStat;
    [SerializeField] private StatKey healOnWaveCompletedStat;
    [SerializeField] private StatKey maxConcurrentBatchesStat;
    [SerializeField] private StatKey processingSpeedStat;

    private float fuelMaxCapacity;
    private bool requiresFuel;

    [Tooltip("Gameobject where outputs will be stored")]
    public Transform storage;

    [Tooltip("Seconds between checks for processing batches")]
    public float processingCheckInterval = 1f;
    private float processingCheckTimer = 0f;

    private readonly Dictionary<string, InputState> inputState = new();

    [Header("Thresholds")]

    [SerializeField] private List<RecipeThresholdEntry> recipeThresholds = new();

    [System.Serializable]
    private class RecipeThresholdEntry
    {
        public ProcessResourceRecipe recipe;
        [Range(1, 100)]
        public int threshold = 1;
    }

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

    public int MaxConcurrentBatches => maxConcurrentBatches;
    public int ProcessingCount => processingCount;
    public float ProcessingSpeed => processingSpeed;

    public event System.Action<int> OnProcessingCountChanged;

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

    private class RecipeState
    {
        public ProcessResourceRecipe recipe;

        public int storedOutput;
        public int reservedOutput;
    }

    private List<RecipeState> recipeStates;

    private bool isRegistered = false;

    #region Unity methods

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

            string inputId = recipe.inputItemData.id;
            if (string.IsNullOrEmpty(inputId))
            {
                continue;
            }

            if (!inputState.TryGetValue(inputId, out InputState state))
            {
                inputState[inputId] = new InputState(recipe.inputPerBatch);
            }
            // If ItemData already exists, keep MaxCapacity as the maximun input needed 
            else
            {
                inputState[inputId].MaxCapacity = Mathf.Max(inputState[inputId].MaxCapacity, recipe.inputPerBatch);
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
            || StatModifierManager.Instance == null
            || InventoryManager.Instance == null)
        {
            yield return null;
        }

        ItemConsumerManager.Instance.Register(this);
        isRegistered = true;

        StatModifierManager.Instance.OnModifiersChanged += HandleModifiersChanged;
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;

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
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        ResetInternalStateAndReleaseReservations();
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

    #endregion

    #region Stats

    private void CheckNullStats()
    {
        if (maxHealthStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(maxHealthStat)}");
        }
        if (healOnWaveCompletedStat == null)
        {
            Debug.LogError($"{name}: missing {nameof(healOnWaveCompletedStat)}");
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
        if (StatModifierManager.Instance.TryGetValueAfterModifiers(data, healOnWaveCompletedStat, out finalValue))
        {
            structure.healOnWaveCompleted = finalValue;
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

    #endregion

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
        OnProcessingCountChanged?.Invoke(processingCount);

        foreach (KeyValuePair<string, InputState> input in inputState)
        {
            input.Value.Reserved = 0;
            input.Value.Stored = 0;
        }

        foreach (RecipeState state in recipeStates)
        {
            state.reservedOutput = 0;
            state.storedOutput = 0;
        }
    }

    private void StartProcessingBatches()
    {
        if (recipeStates == null || recipeStates.Count == 0) 
        { 
            return; 
        }

        foreach (RecipeState recipeState in recipeStates)
        {
            string inputId = recipeState.recipe.inputItemData.id;

            if (!inputState.TryGetValue(inputId, out InputState state))
            {
                Debug.LogError($"{name} couldnt get value of {nameof(InputState)} with key {recipeState.recipe.inputItemData}");
                return;
            }

            bool readyForBatch = CanStartBatch(recipeState, state);

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
                OnProcessingCountChanged?.Invoke(processingCount);

                StartCoroutine(ProcessBatch(recipeState));

                readyForBatch = CanStartBatch(recipeState, state);
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

#if UNITY_EDITOR
        Debug.Log($"{name} started processing a batch of {recipeState.recipe.name}. " +
            $"Will finish in {processingTime} seconds. " +
            $"Consumed input: {recipeState.recipe.inputPerBatch}. " +
            $"Consumed fuel [{requiresFuel}]: {recipeState.recipe.fuelPerBatch}.");
#endif

        yield return new WaitForSeconds(processingTime);

        recipeState.storedOutput += recipeState.recipe.outputPerInput;
        recipeState.reservedOutput -= recipeState.recipe.outputPerInput;

        processingCount--;
        OnProcessingCountChanged?.Invoke(processingCount);

#if UNITY_EDITOR
        Debug.Log($"{name} has processed a batch of {recipeState.recipe.name}" +
            $"\nStored amount = {recipeState.storedOutput}" +
            $"\nMax amount = {recipeState.recipe.outputMaxCapacity}");
#endif
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

    private bool CanStartBatch(RecipeState recipeState, InputState state)
    {
        if (recipeState == null || recipeState.recipe == null || state == null)
        {
            return false;
        }
        if (state.Stored < recipeState.recipe.inputPerBatch)
        {
            return false;
        }
        if (requiresFuel && storedFuel < recipeState.recipe.fuelPerBatch)
        {
            return false;
        }
        int pendingOutput = GetPendingOutputCount(recipeState);
        if (pendingOutput + recipeState.recipe.outputPerInput > recipeState.recipe.outputMaxCapacity)
        {
            return false;
        }

        int currentTotalOutput = GetCurrentTotalOutputCount(recipeState);
        int threshold = GetThreshold(recipeState.recipe.outputItemData);        

        return currentTotalOutput + recipeState.recipe.outputPerInput <= threshold;
    }

    private int GetPendingOutputCount(RecipeState recipeState)
    {
        int pendingOutput = recipeState.storedOutput + recipeState.reservedOutput;

        Transform recipeStorage = TryGetRecipeStorage(recipeState.recipe);
        if (recipeStorage != null)
        {
            pendingOutput += recipeStorage.childCount;
        }

        return pendingOutput;
    }

    private int GetCurrentTotalOutputCount(RecipeState recipeState)
    {
        int storedInWarehouses = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetTotal(recipeState.recipe.outputItemData)
            : 0;

        return storedInWarehouses + GetPendingOutputCount(recipeState);
    }

    #region Thresholds

    public List<ItemData> GetOutputs()
    {
        List<ItemData> outputs = new();

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState?.recipe.outputItemData != null)
            {
                outputs.Add(recipeState.recipe.outputItemData);
            }
        }

        return outputs;
    }

    public int GetThreshold(ItemData itemData)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            return int.MaxValue;
        }

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.outputItemData.id == itemId)
            {
                foreach (RecipeThresholdEntry entry in recipeThresholds)
                {
                    if (entry != null && entry.recipe != null && entry.recipe.id == recipeState.recipe.id)
                    {
                        return Mathf.Max(0, entry.threshold);
                    }
                }

                return Mathf.Max(1, recipeState.recipe.outputMaxCapacity);
            }
        }
        
        return int.MaxValue;
    }

    public void SetThreshold(ItemData itemData, int threshold)
    {
        string itemId = itemData.id;

        if (string.IsNullOrEmpty(itemId))
        {
            return;
        }

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.outputItemData.id == itemId)
            {
                int normalizedThreshold = Mathf.Max(0, threshold);

                foreach (RecipeThresholdEntry entry in recipeThresholds)
                {
                    if (entry != null && entry.recipe != null && entry.recipe.id == recipeState.recipe.id)
                    {
                        entry.threshold = normalizedThreshold;
                        processingCheckTimer = 0f;
                        StartProcessingBatches();
                        return;
                    }
                }

                recipeThresholds.Add(new RecipeThresholdEntry
                {
                    recipe = recipeState.recipe,
                    threshold = normalizedThreshold
                });

                processingCheckTimer = 0f;
                StartProcessingBatches();
                return;
            }
        }
    }

    public IReadOnlyList<ProcessResourceRecipe> GetRecipes()
    {
        return data.recipes;
    }

    private void HandleInventoryChanged(ItemData itemData, int currentTotal)
    {
        if (itemData == null || recipeStates == null)
        {
            return;
        }

        if (!HasRecipeWithOutput(itemData))
        {
            return;
        }

        processingCheckTimer = 0f;
        StartProcessingBatches();
    }

    private bool HasRecipeWithOutput(ItemData itemData)
    {
        string itemId = itemData.id;

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState != null
                && recipeState.recipe != null
                && recipeState.recipe.outputItemData.id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    private Transform TryGetRecipeStorage(ProcessResourceRecipe recipe)
    {
        if (storage == null || recipe == null || recipe.outputItemData == null)
        {
            return null;
        }

        return storage.Find($"Storage_{recipe.outputItemData.Name}");
    }

    /// <summary>
    /// Returns the storage for a specific recipe,
    /// if it doesnt exist it creates it first
    /// </summary>
    /// <param name="recipe"></param>
    /// <returns>Transform of the storage</returns>
    private Transform GetRecipeStorage(ProcessResourceRecipe recipe)
    {
        Transform existingStorage = TryGetRecipeStorage(recipe);
        if (existingStorage != null)
        {
            return existingStorage;
        }

        // This storage recipe doesnt exist yet, create it
        string storageName = $"Storage_{recipe.outputItemData.Name}";
        GameObject storageGameObject = new(storageName);
        storageGameObject.transform.SetParent(storage, worldPositionStays: false);
        storageGameObject.transform.localPosition = Vector3.zero;
        return storageGameObject.transform;
    }

    #region Item Consumer

    public bool CanReceive(ItemData data)
    {
        string itemId = data.id;

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.inputItemData.id == itemId
                && inputState.TryGetValue(itemId, out InputState state))
            {
                if (state.Stored + state.Reserved < state.MaxCapacity)
                {
                    return true;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (storedFuel + reservedFuel + data.fuelValue <= fuelMaxCapacity)
            {
                return true;
            }
        }
        return false;
    }

    public bool Reserve(ItemData data)
    {
        string itemId = data.id;

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.inputItemData.id == itemId
                && inputState.TryGetValue(itemId, out InputState state))
            {
                if (state.Stored + state.Reserved < state.MaxCapacity)
                {
                    state.Reserved++;

                    return true;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (storedFuel + reservedFuel + data.fuelValue <= fuelMaxCapacity)
            {
                reservedFuel = Mathf.Min(fuelMaxCapacity, reservedFuel + data.fuelValue);

                return true;
            }
        }
        return false;
    }

    public void OnReceived(GameObject item, ItemData data)
    {
        Destroy(item);

        string itemId = data.id;

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.inputItemData.id == itemId 
                && inputState.TryGetValue(itemId, out InputState state))
            {
                if (state.Stored < state.MaxCapacity)
                {
                    state.Stored++;
                    Release(data);

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

                return;
            }
        }
    }

    public void Release(ItemData data)
    {
        string itemId = data.id;

        foreach (RecipeState recipeState in recipeStates)
        {
            if (recipeState.recipe.inputItemData.id == itemId
                && inputState.TryGetValue(itemId, out InputState state))
            {
                if (state.Reserved > 0)
                {
                    state.Reserved--;

                    return;
                }
            }
        }

        if (requiresFuel && data.isFuel)
        {
            if (reservedFuel > 0)
            {
                reservedFuel = Mathf.Max(0, reservedFuel - data.fuelValue);

                return;
            }
        }
    }

    public void ConfirmRetrieval(GameObject item)
    {
        
    }

    #endregion
}