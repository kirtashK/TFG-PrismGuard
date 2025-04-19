using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Structure/Structure Data")]
public class StructureData : ScriptableObject
{
    [Tooltip("Nombre de la estructura")]
    public string structureName;

    [Tooltip("Prefab de la estructura una vez construida")]
    public GameObject builtPrefab;

    [Tooltip("Prefab del blueprint de la estructura en construccion")]
    public GameObject blueprintPrefab;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData item;
        public int quantity;
    }

    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
}
