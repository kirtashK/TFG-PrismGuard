using UnityEngine;

[CreateAssetMenu(fileName = "newWarehouseType", menuName = "Data/Warehouse")]
public class WarehouseType : ScriptableObject
{
    [Tooltip("Nombre del tipo de almacén")]
    public string typeName;

    //[Tooltip("Icono para UI de este almacén")]
    //public Sprite icon;
}
