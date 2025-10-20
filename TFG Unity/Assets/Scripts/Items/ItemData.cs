using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    [Range(0f, 100f)]
    public float weight;

    [Range(0f, 100f)]
    public float interactionRange;
    //public int priority;

    [Tooltip("Warehouse type where this item can be stored")]
    public List<WarehouseType> storedIn = new();

    void OnValidate()
    {
        // Fix negative values
        weight = Mathf.Max(0f, weight);
        interactionRange = Mathf.Max(0f, interactionRange);
    }
}