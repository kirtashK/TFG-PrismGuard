using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3.5f;
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
    [Tooltip("Maximun distance to chase an enemy/structure")]
    public float maxChaseDistance = 20f;

    [Tooltip("Radius around enemy to detect targets")]
    public float AggroRadius = 5f;
}