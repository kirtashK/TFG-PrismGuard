using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyType", menuName = "Enemies/Enemy Type")]
public class EnemyData : ScriptableObject
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3.5f;
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Rewards upon defeating")]
    public int rewardGold = 5;
    public int rewardExperience = 2;

    [Header("IA")]
    [Tooltip("Distancia máxima a la que consideran atacar obstáculo")]
    public float maxChaseDistance = 20f;
}