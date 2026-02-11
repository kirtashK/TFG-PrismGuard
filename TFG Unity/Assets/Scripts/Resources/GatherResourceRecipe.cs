using UnityEngine;

[CreateAssetMenu(menuName = "Data/GatherResourceRecipe", fileName = "NewGatherResourceRecipe")]
public class GatherResourceRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Resource spawned")]

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

    [Tooltip("Amount of the resource this node holds. 0 = infinite & respawns")]
    [Min(0)]
    public int resourceAmount = 0;

    [Tooltip("If resourceAmount > 0, a random deviation is applied and the actual amount is within the rango of the max deviation (50 resource, 10 deviation -> range of 40-60)")]
    [Min(0)]
    public int resourceMaxDeviation = 0;

    void OnValidate()
    {
        if (resourceItemData == null)
        {
            Debug.LogWarning($"{name} missing resourceItemData");
        }

        if (resourceAmount > 0)
        {
            if (resourceAmount - resourceMaxDeviation < 1)
            {
                Debug.LogWarning($"{name} has a deviation bigger than the amount of resources it can hold. It will be clamped to 1");
            }
        }
    }
}