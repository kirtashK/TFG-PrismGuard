using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseData : ScriptableObject
{
    [Header("Base")]

    public string Name;
    public string id;
    public Sprite icon;

    [Tooltip("Optional, if filled, this asset will require this research as requisite to be unlocked/usable")]
    public ResearchData requiredResearch;

    [Header("Stats")]
    public List<StatBaseEntry> statBases = new();    

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning($"{name} missing {nameof(Name)}");
        }

        //id = Name;

        // Assign ID a GUID to make it unique
        if (string.IsNullOrWhiteSpace(id))
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid))
                {
                    id = guid;
                }
                else
                {
                    id = System.Guid.NewGuid().ToString("N");
                }
            }
            // Created at runtime:
            else
            {
                id = System.Guid.NewGuid().ToString("N");
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Try to get base value of a statKey
    /// </summary>
    /// <param name="statKey"></param>
    /// <param name="baseValue">out parameter, holds base value paired with provided StatKey</param>
    /// <returns>True if found</returns>
    public bool TryGetStatBase(StatKey statKey, out float baseValue)
    {
        baseValue = 0f;
        if (statKey == null || statBases == null || statBases.Count == 0)
        {
            return false;
        }

        foreach (StatBaseEntry entry in statBases)
        {
            if (entry != null && entry.statKey == statKey)
            {
                baseValue = entry.value;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class StatBaseEntry
{
    public StatKey statKey;

    public float value;
}