using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Data/Resource")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    public GameObject prefab;
    public float respawnTime;
}
