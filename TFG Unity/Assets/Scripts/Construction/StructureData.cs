using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStructureData", menuName = "Data/Structure")]
public class StructureData : ScriptableObject
{
    public string structureName;

    [Tooltip("Icon shown on UI")]
    public Sprite icon;

    [Tooltip("Category to show in construction UI")]
    public StructureCategory category = StructureCategory.Misc;

    [Tooltip("Prefab of the structure once built")]
    public GameObject builtPrefab;

    [Tooltip("Prefab of the structure while being built")]
    public GameObject blueprintPrefab;

    [Tooltip("Prefab of the preview during build placement")]
    public GameObject previewPrefab;

    [Header("Placement")]
    [Tooltip("True to use snap to grid")]
    public bool snapToGrid = true;

    [Tooltip("Size of the grid if snapToGrid is true")]
    public float gridSize = 1f;

    [Tooltip("Radius to check colisions while placing")]
    public float placementRadius = 1f;

    [Tooltip("Allow rotation during placement")]
    public bool allowRotation = true;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData itemData;
        public int quantity;
    }

    public List<ResourceRequirement> requirements = new();
}
