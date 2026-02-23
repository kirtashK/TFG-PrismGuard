using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewProcessorData", menuName = "Data/Structure/Processor")]
public class ProcessorData : StructureData
{
    [Header("Processor")]
    [Tooltip("List of recipes this building has availible")]
    public List<ProcessResourceRecipe> recipes;

    [Header("Fuel")]

    [Tooltip("true if it consumes fuel")]
    public bool requiresFuel = false;
    [Tooltip("Max capacity of fuel")]
    [Range(0f, 100f)]
    public float fuelMaxCapacity = 5;

    void OnValidate()
    {
        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning($"{name}: {nameof(recipes)} not configured");
        }
        if (requiresFuel && fuelMaxCapacity <= 0)
        {
            Debug.LogWarning($"{name} requires fuel but has no capacity for fuel");
        }
    }
}
