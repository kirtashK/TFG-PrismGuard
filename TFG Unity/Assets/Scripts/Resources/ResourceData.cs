using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Resources/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    public GameObject prefab;
    public float respawnTime;
}
