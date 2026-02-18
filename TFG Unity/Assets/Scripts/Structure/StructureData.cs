using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStructureData", menuName = "Data/Structure/Structure")]
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
    [Range(0f, 100f)]
    public float gridSize = 1f;

    [Tooltip("Radius to check colisions while placing")]
    [Range(0f, 100f)]
    public float placementRadius = 1f;

    [Tooltip("Allow rotation during placement")]
    public bool allowRotation = true;

    [Tooltip("If true, only one instance of this structure can exist in the scene at a time")]
    public bool isUnique = false;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData itemData;
        [Range(0f, 100f)]
        public int quantity;
    }

    public List<ResourceRequirement> buildRequirements = new();

    void OnValidate()
    {
        if (builtPrefab == null)
        {
            Debug.LogWarning($"{name} missing builtPrefab");
        }
        if (blueprintPrefab == null)
        {
            Debug.LogWarning($"{name} missing blueprintPrefab");
        }
        if (previewPrefab == null)
        {
            Debug.LogWarning($"{name} missing previewPrefab");
        }
        if (buildRequirements == null || buildRequirements.Count == 0)
        {
            Debug.LogWarning($"{name}: buildRequirements not configured");
        }
    }
}
