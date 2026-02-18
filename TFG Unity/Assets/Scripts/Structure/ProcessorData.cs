using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewProcessorData", menuName = "Data/Structure/Processor")]
public class ProcessorData : StructureData
{
    [Header("Processor")]
    [Tooltip("List of recipes this building has availible")]
    public List<ProcessResourceRecipe> recipes;

    void OnValidate()
    {
        if (recipes == null || recipes.Count == 0)
        {
            Debug.LogWarning($"{name}: Recipes not configured");
        }
    }
}
