using UnityEngine;

[CreateAssetMenu(fileName = "NewSoldierData", menuName = "Data/Soldier")]
public class SoldierData : UnitData
{
    [Header("Soldier")]

    [Range(0f, 100f)]
    public float attackRange = 1.5f;
    [Range(0f, 100f)]
    public float attackDamage = 10f;
    [Range(0f, 100f)]
    public float attackCooldown = 1f;

    [Header("AI")]

    [Tooltip("Radius where soldier detects enemies to attack")]
    [Range(0f, 100f)]
    public float AggroRadius = 7.5f;
}