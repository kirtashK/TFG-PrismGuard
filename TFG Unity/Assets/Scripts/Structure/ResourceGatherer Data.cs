using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewResourceGathererData", menuName = "Data/Structure/Gathering Flag")]
public class ResourceGathererData : StructureData
{
    [Header("Gathering Flag")]

    [Tooltip("This gatherer will mark only the choosen categories")]
    public List<ResourceCategory> allowedCategories;

    [Tooltip("Radius around this gatherer where resources are gathered")]
    [Min(0)]
    public float gatheringRadius = 10f;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (allowedCategories == null || allowedCategories.Count == 0)
        {
            Debug.LogWarning($"{name}: Allowed categories not configured");
        }
    }
}
