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
    [Range(0f, 100f)]
    public float workDuration = 3f;

    [Range(0f, 100f)]
    public float interactionRange = 1.5f;

    [Range(0f, 20f)]
    public int priority = 1;

    [Header("Number of resources to generate")]
    [Range(0f, 49f)]
    public int minResourcesToSpawn = 1;

    [Range(0f, 50f)]
    public int maxResourcesToSpawn = 2;

    void OnValidate()
    {
        // Fix negative values
        workDuration = Mathf.Max(0f, workDuration);
        interactionRange = Mathf.Max(0f, interactionRange);
        priority = Mathf.Max(0, priority);
        minResourcesToSpawn = Mathf.Max(0, minResourcesToSpawn);
        maxResourcesToSpawn = Mathf.Max(0, maxResourcesToSpawn);

        if (resourceItemPrefab == null)
        {
            Debug.LogWarning($"{name} missing resourceItemPrefab");
        }
        if (resourceItemData == null)
        {
            Debug.LogWarning($"{name} missing resourceItemData");
        }
    }
}