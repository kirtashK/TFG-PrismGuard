using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;

    public GameObject itemPrefab;

    [Range(0f, 100f)]
    public float weight = 1;

    [Range(0f, 100f)]
    public float interactionRange = 1;
    //public int priority;

    [Tooltip("Warehouse type where this item can be stored")]
    public List<WarehouseType> storedIn = new();

    [Header("Trading")]
    public bool canBeBought = true;
    [Min(0)]
    public int buyPrice = 10;

    public bool canBeSold = true;
    [Range(0f, 1f)]
    [Tooltip("Sell price is buy price * sellFraction")]
    public float sellFraction = 0.25f;
}