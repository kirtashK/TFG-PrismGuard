using UnityEngine;

[CreateAssetMenu(menuName = "Data/GatherResourceRecipe", fileName = "NewGatherResourceRecipe")]
public class GatherResourceRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Resource spawned")]

    [Tooltip("Prefab of the resource generated once node is gathered")]
    public GameObject resourceItemPrefab;

    [Tooltip("Data of the resource generated once node is gathered")]
    public ItemData resourceItemData;

    [Header("General stats")]

    [Tooltip("Time in seconds to gather the node")]
    public float workDuration = 3f;

    public float interactionRange = 1.5f;

    public int priority = 1;

    [Header("Number of resources to generate")]
    public int minResourcesToSpawn = 1;

    public int maxResourcesToSpawn = 2;
}