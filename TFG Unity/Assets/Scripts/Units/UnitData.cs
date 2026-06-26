using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UnitData : BaseData
{
    [Header("Unit")]

    [Tooltip("List of prefabs this unit can spawn as")]
    public List<AssetReferenceGameObject> PrefabVariants = new();

    [Range(0f, 1000f)]
    public float buildTime;
    [Range(0f, 1000f)]
    public int scoreCost;
    public List<ResourceRequirement> createCosts = new();

    [Header("Runtime")]

    public Faction faction = Faction.Player;
    public bool isPlayerControllable = true;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData itemData;
        [Range(0f, 1000f)]
        public int quantity;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (PrefabVariants == null || PrefabVariants.Count == 0)
        {
            Debug.LogWarning($"{name}: missing {nameof(PrefabVariants)}");
        }
        if (faction == Faction.Player && (createCosts == null || createCosts.Count == 0))
        {
            Debug.LogWarning($"{name}: {nameof(createCosts)} not configured");
        }
    }
#endif

    public AssetReferenceGameObject GetRandomPrefabReference()
    {
        int validVariantCount = 0;

        if (PrefabVariants != null)
        {
            foreach (AssetReferenceGameObject prefabVariant in PrefabVariants)
            {
                if (prefabVariant != null && prefabVariant.RuntimeKeyIsValid())
                {
                    validVariantCount++;
                }
            }
        }

        if (validVariantCount > 0)
        {
            int selectedIndex = Random.Range(0, validVariantCount);

            foreach (AssetReferenceGameObject prefabVariant in PrefabVariants)
            {
                if (prefabVariant == null || !prefabVariant.RuntimeKeyIsValid())
                {
                    continue;
                }

                if (selectedIndex == 0)
                {
                    return prefabVariant;
                }

                selectedIndex--;
            }
        }

        return null;
    }
}