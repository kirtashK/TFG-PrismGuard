using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class StatModifierManager : MonoBehaviour
{
    public static StatModifierManager Instance { get; private set; }

    public enum ModifierKind
    {
        Additive,
        Multiplicative,
    }

    public class ModifierRecord
    {
        public string sourceId;
        public string targetId;
        public StatKey statKey;
        public string statKeyId;
        public ModifierKind kind;
        public float value;
    }

    private readonly Dictionary<string, Dictionary<string, List<ModifierRecord>>> storage = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ModifierRecord>> bySource = new(StringComparer.Ordinal);

    public event Action<string, string> OnModifiersChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Register a modifier
    /// </summary>
    public void AddModifier(string sourceId, string targetId, StatKey statKey, ModifierKind kind, float value)
    {
        if (string.IsNullOrEmpty(statKey.id) || statKey == null || string.IsNullOrEmpty(sourceId))
        {
            Debug.LogWarning($"{name}: invalid arguments");
            return;
        }

        string targetKey = string.IsNullOrEmpty(targetId) ? "" : targetId;
        string statKeyId = statKey.id;

        if (!storage.TryGetValue(targetKey, out Dictionary<string, List<ModifierRecord>> statMap))
        {
            statMap = new Dictionary<string, List<ModifierRecord>>(StringComparer.Ordinal);
            storage[targetKey] = statMap;
        }

        if (!statMap.TryGetValue(statKeyId, out List<ModifierRecord> list))
        {
            list = new List<ModifierRecord>();
            statMap[statKeyId] = list;
        }

        // Skip duplicates
        bool duplicate = list.Exists(m => m.sourceId == sourceId && m.kind == kind && Mathf.Approximately(m.value, value));
        if (duplicate)
        {
            return;
        }

        ModifierRecord record = new() 
        { 
            sourceId = sourceId, 
            targetId = targetKey, 
            statKey = statKey, 
            statKeyId = statKeyId,
            kind = kind, 
            value = value 
        };

        list.Add(record);

        if (!bySource.TryGetValue(sourceId, out List<ModifierRecord> srcList))
        {
            srcList = new List<ModifierRecord>();
            bySource[sourceId] = srcList;
        }
        srcList.Add(record);

        OnModifiersChanged?.Invoke(targetKey, statKey.id);
    }

    /// <summary>
    /// Try to get the final value after modifiers for the provided Stat
    /// </summary>
    /// <param name="data"></param>
    /// <param name="Stat"></param>
    /// <param name="finalValue">Out parameter with the final value after modifiers</param>
    /// <returns>True if found</returns>
    public bool TryGetValueAfterModifiers(BaseData data, StatKey Stat, out float finalValue)
    {
        finalValue = 0f;
        if (data.TryGetStatBase(Stat, out float baseValue))
        {
            finalValue = GetEffectiveFloat(data, Stat, baseValue);

            if (baseValue != finalValue)
            {
                Debug.Log($"{name}: {Stat.fieldName}: {baseValue} upgraded to {finalValue}");
            }

            return true;
        }
        else
        {
            Debug.LogWarning($"{name}: Missing base value for {Stat.name}");
        }

        return false;
    }

    /// <summary>
    /// Compute final float stat given a base value
    /// </summary>
    /// <param name="targetId"></param>
    /// <param name="statKey"></param>
    /// <param name="baseValue"></param>
    /// <returns>Effective value</returns>
    public float GetEffectiveFloat(string targetId, StatKey statKey, float baseValue)
    {
        if (statKey == null || string.IsNullOrEmpty(statKey.id))
        {
            return baseValue;
        }
        float additive = 0f;
        double multiplicativeProduct = 1.0;

        // Apply global modifiers:
        GatherModifiersAndAccumulate("", statKey.id, ref additive, ref multiplicativeProduct);

        // Apply target-specific modifiers:
        string targetKey = string.IsNullOrEmpty(targetId) ? "" : targetId;
        if (targetKey != "")
        {
            GatherModifiersAndAccumulate(targetKey, statKey.id, ref additive, ref multiplicativeProduct);
        }

        float result = (baseValue + additive) * (float)multiplicativeProduct;
        return result;
    }

    /// <summary>
    /// Compute final float stat given a base value
    /// </summary>
    /// <param name="targetData"></param>
    /// <param name="statKey"></param>
    /// <param name="baseValue"></param>
    /// <returns>Effective value</returns>
    public float GetEffectiveFloat(BaseData targetData, StatKey statKey, float baseValue)
    {
        string id = targetData != null ? targetData.id : "";
        return GetEffectiveFloat(id, statKey, baseValue);
    }

    private void GatherModifiersAndAccumulate(string tKey, string statName, ref float additive, ref double multiplicativeProduct)
    {
        if (!storage.TryGetValue(tKey, out Dictionary<string, List<ModifierRecord>> statMap))
            return;
        if (!statMap.TryGetValue(statName, out List<ModifierRecord> list))
            return;

        foreach (ModifierRecord modifier in list)
        {
            if (modifier.kind == ModifierKind.Additive)
            {
                additive += modifier.value;
            }
            else if (modifier.kind == ModifierKind.Multiplicative)
            {
                multiplicativeProduct *= (1.0 + modifier.value);
            }
        }
    }

    // TODO Call on load or new game
    /// <summary>
    /// Called at start-up to reapply modifiers
    /// </summary>
    public void ReapplyFromCompletedResearch()
    {
        if (ResearchManager.Instance == null) 
        { 
            return; 
        }

        foreach (string completedResearchId in ResearchManager.Instance.AllCompletedResearchIds())
        {
            if (!ResearchManager.Instance.TryGetResearchData(completedResearchId, out ResearchData researchData) 
                || researchData.effects == null) 
            { 
                continue; 
            }

            foreach (ResearchEffect effect in researchData.effects)
            {
                if (effect is ModifyStatEffect modifyStatEffect)
                {
                    modifyStatEffect.ApplyEffect(researchData.id);
                }
            }
        }
    }
}