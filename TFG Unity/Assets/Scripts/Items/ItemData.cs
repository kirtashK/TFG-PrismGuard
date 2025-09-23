using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public float weight;
    public float interactionRange;
    //public int priority;

    [Tooltip("Warehouse type where this item can be stored")]
    public List<WarehouseType> storedIn = new();
}