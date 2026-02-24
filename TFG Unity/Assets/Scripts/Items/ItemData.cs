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

    [Tooltip("Category (leaf) of the item")]
    public ItemCategory category;

    [Header("Fuel")]

    [Tooltip("Can this item be used as fuel?")]
    public bool isFuel = false;

    [Tooltip("If its fuel, refills this much fuel")]
    [Range(0f, 10f)]
    public float fuelValue = 0f;

    [Header("Trading")]
    public bool canBeBought = true;
    [Min(0)]
    public int buyPrice = 10;

    public bool canBeSold = true;
    [Range(0f, 1f)]
    [Tooltip("Sell price is buy price * sellFraction")]
    public float sellFraction = 0.25f;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning($"{name} missing {nameof(itemName)}");
        }
        if (icon == null)
        {
            Debug.LogWarning($"{name} missing {nameof(icon)}");
        }
        if (itemPrefab == null)
        {
            Debug.LogWarning($"{name} missing {nameof(itemPrefab)}");
        }
        if (category == null)
        {
            Debug.LogWarning($"{name} missing {nameof(category)}");
        }
        if (isFuel && fuelValue == 0f)
        {
            Debug.LogWarning($"{name} is fuel but has no fuel value");
        }
    }
}