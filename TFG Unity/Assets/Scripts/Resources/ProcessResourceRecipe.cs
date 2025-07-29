using UnityEngine;

[CreateAssetMenu(menuName = "Data/ProcessResourceRecipe", fileName = "NewProcessResourceRecipe")]
public class ProcessResourceRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Input")]
    [Tooltip("Input's data")]
    public ItemData inputItemData;
    [Tooltip("Max capacity of input")]
    public int inputMaxCapacity = 6;

    [Header("Fuel (Optional")]
    [Tooltip("true if it consumes fuel")]
    public bool requiresFuel = false;
    [Tooltip("Item to consume as fuel")]
    public ItemData fuelItem;
    [Tooltip("Fuel consumed per batch")]
    public int fuelPerBatch = 1;
    [Tooltip("Max capacity of fuel")]
    public int fuelMaxCapacity = 5;

    [Header("Output")]
    [Tooltip("Output's data")]
    public ItemData outputItemData;
    [Tooltip("Output's prefab")]
    public GameObject outputPrefab;
    [Tooltip("Max capacity of output")]
    public int outputMaxCapacity = 5;
    [Tooltip("Items generated per batch")]
    public int outputPerInput = 1;

    [Header("Processing")]
    [Tooltip("Time in seconds")]
    public float processingTime = 10f;
    [Tooltip("Max amount of concurrent processing")]
    public int maxConcurrentBatches = 2;
}