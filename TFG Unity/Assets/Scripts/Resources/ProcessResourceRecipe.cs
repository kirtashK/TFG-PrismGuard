using UnityEngine;

[CreateAssetMenu(menuName = "Data/ProcessResourceRecipe", fileName = "NewProcessResourceRecipe")]
public class ProcessResourceRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Input")]
    [Tooltip("Input's data")]
    public ItemData inputItemData;
    [Tooltip("Max capacity of input")]
    [Range(0f, 100f)]
    public int inputMaxCapacity = 6;
    [Tooltip("Input consumed per batch")]
    [Range(0f, 100f)]
    public int inputPerBatch = 1;

    [Header("Fuel (Optional")]
    [Tooltip("true if it consumes fuel")]
    public bool requiresFuel = false;
    [Tooltip("Item to consume as fuel")]
    public ItemData fuelItemData;
    [Tooltip("Fuel consumed per batch")]
    [Range(0f, 100f)]
    public int fuelPerBatch = 1;
    [Tooltip("Max capacity of fuel")]
    [Range(0f, 100f)]
    public int fuelMaxCapacity = 5;

    [Header("Output")]
    [Tooltip("Output's data")]
    public ItemData outputItemData;
    [Tooltip("Max capacity of output")]
    [Range(0f, 100f)]
    public int outputMaxCapacity = 5;
    [Tooltip("Items generated per batch")]
    [Range(0f, 100f)]
    public int outputPerInput = 1;

    [Header("Processing")]
    [Tooltip("Time in seconds")]
    [Range(0f, 1000f)]
    public float processingTime = 10f;
    [Tooltip("Max amount of concurrent processing")]
    [Range(0f, 10f)]
    public int maxConcurrentBatches = 2;

    void OnValidate()
    {
        if (inputItemData == null)
        {
            Debug.LogWarning($"{name} missing inputItemData");
        }
        if (requiresFuel && fuelItemData == null)
        {
            Debug.LogWarning($"{name} missing fuelItem");
        }
        if (outputItemData == null)
        {
            Debug.LogWarning($"{name} missing outputItemData");
        }
    }
}