using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Data/Resource")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    [Range(0f, 3600f)]
    public float respawnTime;
}
