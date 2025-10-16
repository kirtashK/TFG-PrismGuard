using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/Enemy")]
public class EnemyData : UnitData
{
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Wave")]

    [Tooltip("Cost to spawn this enemy, its also the score added once defeated")]
    public int spawnCost = 5;

    [Header("Rewards")]

    [Tooltip("Experience added to the soldier that killed this enemy")]
    public int rewardExperience = 2;

    [Header("AI")]

    [Tooltip("Radius around enemy to detect targets")]
    public float AggroRadius = 5f;

    void OnValidate()
    {
        faction = Faction.Enemy;
        isPlayerControllable = false;

        // Fix negative values
        attackRange = Mathf.Max(0f, attackRange);
        attackDamage = Mathf.Max(0f, attackDamage);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        spawnCost = Mathf.Max(0, spawnCost);
        rewardExperience = Mathf.Max(0, rewardExperience);
        AggroRadius = Mathf.Max(0f, AggroRadius);
    }
}