using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWarehouseData", menuName = "Data/Structure/Warehouse")]
public class WarehouseData : StructureData
{
    [Header("Warehouse")]

    [Tooltip("Categories this warehouse accepts. If a category is an ancestor of an item's category it will match")]
    public List<ItemCategory> acceptedCategories = new();

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (acceptedCategories == null || acceptedCategories.Count == 0)
        {
            Debug.LogWarning($"{name}: Category not configured");
        }
    }
#endif
}