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
    //public GameObject Prefab;
    public AssetReferenceGameObject PrefabReference;
    public float buildTime;
    public int scoreCost;
    public List<ResourceRequirement> createCosts = new();

    [Header("Runtime")]

    public Faction faction = Faction.Player;
    public bool isPlayerControllable = true;

    [Header("Stats")]

    public float maxHealth = 20f;
    public float moveSpeed = 3.5f;

    [System.Serializable]
    public struct ResourceRequirement
    {
        public ItemData itemData;
        public int quantity;
    }

    void OnValidate()
    {
        // Fix negative values
        buildTime = Mathf.Max(0f, buildTime);
        scoreCost = Mathf.Max(0, scoreCost);
        maxHealth = Mathf.Max(0f, maxHealth);
        moveSpeed = Mathf.Max(0f, moveSpeed);

        if (PrefabReference == null)
        {
            Debug.LogWarning($"UnitData '{name}' missing PrefabReference");
        }
    }
}