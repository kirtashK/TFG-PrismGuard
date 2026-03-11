using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum Faction
{
    Player,
    Enemy,
    Neutral
}

public class UnitData : BaseData
{
    [Header("Unit")]

    public AssetReferenceGameObject PrefabReference;

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

    protected override void OnValidate()
    {
        base.OnValidate();

        if (PrefabReference == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(PrefabReference)}");
        }
        if (faction == Faction.Player && (createCosts == null || createCosts.Count == 0))
        {
            Debug.LogWarning($"{name}: {nameof(createCosts)} not configured");
        }
    }
}