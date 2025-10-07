using System.Collections.Generic;
using UnityEngine;

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
    public GameObject Prefab;
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
}