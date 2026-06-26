using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Data/Resource")]
public class ResourceData : BaseData
{
    [Header("Resource")]

    public ResourceCategory category;

    [Range(0f, 3600f)]
    public float respawnTime;
}
