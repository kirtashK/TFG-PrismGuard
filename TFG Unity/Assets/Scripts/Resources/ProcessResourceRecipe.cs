using UnityEngine;

[CreateAssetMenu(menuName = "Data/ProcessResourceRecipe", fileName = "NewProcessResourceRecipe")]
public class ProcessResourceRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Input")]

    [Tooltip("Input's data")]
    public ItemData inputItemData;
    [Tooltip("Input consumed per batch")]
    [Range(0f, 100f)]
    public int inputPerBatch = 1;

    [Header("Fuel")]

    [Tooltip("Fuel consumed per batch (0 = disabled)")]
    [Range(0f, 10f)]
    public float fuelPerBatch = 0;

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
            Debug.LogWarning($"{name} missing {nameof(inputItemData)}");
        }
        if (outputItemData == null)
        {
            Debug.LogWarning($"{name} missing {nameof(outputItemData)}");
        }
    }
}