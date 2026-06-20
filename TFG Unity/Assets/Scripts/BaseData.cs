using System;
using System.Collections.Generic;
using UnityEditor;
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

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(Name))
        {
            Debug.LogWarning($"{name} missing {nameof(Name)}");
        }

        CheckID();
    }
#endif

    private void OnEnable()
    {
        CheckID();
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

        string targetId = statKey.id;

        foreach (StatBaseEntry entry in statBases)
        {
            if (entry == null || entry.statKey == null)
            {
                continue;
            }

            if (entry.statKey.id == targetId)
            {
                baseValue = entry.value;
                return true;
            }
        }

        return false;
    }

    private void CheckID()
    {
        if (!string.IsNullOrEmpty(id))
        {
            return;
        }

#if UNITY_EDITOR
        string assetPath = AssetDatabase.GetAssetPath(this);

        if (!string.IsNullOrEmpty(assetPath))
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            id = !string.IsNullOrEmpty(guid) ? guid : Guid.NewGuid().ToString("N");
        }
        else
        {
            id = Guid.NewGuid().ToString("N");
        }

        EditorUtility.SetDirty(this);
#else
        // Runtime
        id = Guid.NewGuid().ToString("N");
#endif
    }
}

[Serializable]
public class StatBaseEntry
{
    public StatKey statKey;

    public float value;
}