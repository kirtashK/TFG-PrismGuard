using UnityEngine;

[CreateAssetMenu(fileName = "NewSoldierData", menuName = "Data/Soldier")]
public class SoldierData : UnitData
{
    [Header("Soldier")]

    [Header("AI")]

    [Tooltip("Radius where soldier detects enemies to attack")]
    [Range(0f, 100f)]
    public float AggroRadius = 7.5f;
}