using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/StatKey")]
public class StatKey : BaseData
{
    [Header("Stat key")]

    [Tooltip("Name of the stat field as written in the code")]
    public string fieldName;
    public enum ValueKind { Float, Int }
    public ValueKind valueKind = ValueKind.Float;

    [Tooltip("Default base value")]
    public float defaultBaseValue = 0f;
}

public static class StatValueResolver
{
    /// <summary>
    /// Try to get a base value for a given BaseData and StatKey
    /// </summary>
    /// <param name="data"></param>
    /// <param name="statKey"></param>
    /// <param name="value">Out parameter with the retrieved base value</param>
    /// <returns>True if any value was retrieved</returns>
    public static bool TryGetBaseFloat(BaseData data, StatKey statKey, out float value)
    {
        value = 0f;
        if (statKey == null)
        {
            return false;
        }

        if (data != null)
        {
            if (data.TryGetStatBase(statKey, out float baseFromList))
            {
                value = baseFromList;
                return true;
            }
        }

        // Fallback
        value = statKey.defaultBaseValue;
        return true;
    }
}