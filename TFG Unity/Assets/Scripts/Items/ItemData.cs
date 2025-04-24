using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public float weight;
    public float interactionRange;
    //public int priority;

    [Tooltip("Tipos de almacen donde se puede guardar este item")]
    public List<WarehouseType> storedIn = new List<WarehouseType>();
}