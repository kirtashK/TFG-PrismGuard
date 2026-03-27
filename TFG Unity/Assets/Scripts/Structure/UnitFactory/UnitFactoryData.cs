using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUnitFactoryData", menuName = "Data/Structure/UnitFactory")]
public class UnitFactoryData : StructureData
{
    [Header("Unit Factory")]

    [Tooltip("Units this factory can produce. Empty = all units")]
    public List<UnitData> producibleUnits = new();

    [Tooltip("Max queue length (-1 = unlimited)")]
    [Min(-1)]
    public int maxQueueLength = 5;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (producibleUnits == null)
        {
            Debug.LogWarning($"{name}: producible units not configured");
        }
    }
}
