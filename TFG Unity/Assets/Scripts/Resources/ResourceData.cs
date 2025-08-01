using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceData", menuName = "Data/Resource")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    public float respawnTime;
}
