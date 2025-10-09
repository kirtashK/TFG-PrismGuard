using UnityEngine;

[CreateAssetMenu(fileName = "NewSoldierData", menuName = "Data/Soldier")]
public class SoldierData : UnitData
{
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("AI")]

    [Tooltip("Radius where soldier detects enemies to attack")]
    public float AggroRadius = 7.5f;

    void OnValidate()
    {
        // Fix negative values
        attackRange = Mathf.Max(0f, attackRange);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        AggroRadius = Mathf.Max(0f, AggroRadius);
    }
}