using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWarehouseData", menuName = "Data/Structure/Warehouse")]
public class WarehouseData : StructureData
{
    [Header("Warehouse")]
    [Tooltip("Categories this warehouse accepts. If a category is an ancestor of an item's category it will match")]
    public List<ItemCategory> acceptedCategories = new();

    [Tooltip("Maximum total number of items that can be stored")]
    [Range(1, 100)]
    public int maxCapacity = 12;

    void OnValidate()
    {
        if (acceptedCategories == null || acceptedCategories.Count == 0)
        {
            Debug.LogWarning($"{name}: Category not configured");
        }
    }
}