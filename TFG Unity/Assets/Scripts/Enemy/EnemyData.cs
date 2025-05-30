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
    [Tooltip("Coste para generar este enemigo, tambien es la puntuacion añadida al derrotar este enemigo")]
    public int spawnCost = 5;

    [Header("Rewards")]
    [Tooltip("Experienca añadida al soldado que derrote este enemigo")]
    public int rewardExperience = 2;

    [Header("AI")]
    [Tooltip("Distancia máxima a la que consideran atacar un obstáculo")]
    public float maxChaseDistance = 20f;

    [Tooltip("Radio en el que detecta soldados a atacar")]
    public float AggroRadius = 5f;
}