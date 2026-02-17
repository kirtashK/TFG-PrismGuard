using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum Faction
{
    Player,
    Enemy,
    Neutral
}

public class UnitData : ScriptableObject
{
    public string Name;
    public Sprite icon;
    public AssetReferenceGameObject PrefabReference;

    [Range(0f, 1000f)]
    public float buildTime;
    [Range(0f, 1000f)]
    public int scoreCost;
    public List<ResourceRequirement> createCosts = new();

    [Header("Runtime")]

    public Faction faction = Faction.Player;
    public bool isPlayerControllable = true;

    [Header("Stats")]

    [Range(0f, 1000f)]
    public float maxHealth = 20f;
    [Range(0f, 100f)]
    public float moveSpeed = 3.5f;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData itemData;
        [Range(0f, 1000f)]
        public int quantity;
    }

    void OnValidate()
    {
        if (PrefabReference == null)
        {
            Debug.LogWarning($"UnitData '{name}' missing PrefabReference");
        }
        if (createCosts == null || createCosts.Count == 0)
        {
            Debug.LogWarning($"{name}: createCosts not configured");
        }
    }
}