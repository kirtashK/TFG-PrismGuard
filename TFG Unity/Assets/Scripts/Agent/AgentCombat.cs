using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;

[DisallowMultipleComponent]
public class AgentCombat : MonoBehaviour, ICombatTarget
{
    private SoldierData data;

    public float currentHealth;

    public Vector3 Position => transform.position;
    public bool isAlive => currentHealth > 0f;

    private float lastAttackTime = -Mathf.Infinity;

    private Agent mlAgent;

    [Header("Rewards")]

    [Tooltip("Reward scale per 1 HP damage dealt")]
    public float rewardPerDamage = 0.01f;
    [Tooltip("Penalty scale per 1 HP received")]
    public float penaltyPerDamageTaken = 0.02f;

    [Tooltip("Bonus when an attack is successful")]
    public float rewardPerSuccessfulAttack = 0.01f;

    [Tooltip("Reward when slaying an enemy")]
    public float rewardOnKill = 1.0f;
    [Tooltip("Penalty when diying")]
    public float penaltyOnDeath = 1.0f;

    [Tooltip("Reward per HP healed")]
    public float rewardPerHealHP = 0.01f;
    [Tooltip("Penalty for wasting healing")]
    public float penaltyWastedHeal = 0.05f;

    private void Awake()
    {
        mlAgent = GetComponent<Agent>();
        data = GetComponent<AgentSoldier>().data;

        currentHealth = data.maxHealth;
    }

    /// <summary>
    /// Try to perform an attack on the target
    /// Returns true if an attack was performed
    /// </summary>
    public bool TryAttack(ICombatTarget target)
    {
        if (!target.isAlive || target == null || !isAlive)
        {
            return false;
        }

        // Cooldown
        if (Time.time < lastAttackTime + data.attackCooldown)
        {
            return false;
        }

        // Range
        float distSqr = (target.Position - Position).sqrMagnitude;
        if (distSqr > data.attackRange * data.attackRange)
        {
            return false;
        }

        target.TakeDamage(data.attackDamage, Position);

        float reward = 0;
        reward += rewardPerSuccessfulAttack;

        reward += data.attackDamage * rewardPerDamage;

        if (!target.isAlive)
        {
            reward += rewardOnKill;
        }

        mlAgent.AddReward(reward);

        lastAttackTime = Time.time;
        return true;
    }

    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"{name} took {amount} damage" +
            $"\nHealth of {name}: {currentHealth}/{data.maxHealth}");

        mlAgent.AddReward(-amount * penaltyPerDamageTaken);

        if (currentHealth <= 0f)
        {
            mlAgent.AddReward(-penaltyOnDeath);

            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} has died");

        if (mlAgent != null)
        {
            mlAgent.EndEpisode();
        }

        Destroy(gameObject);
    }

    public void Heal(float amount)
    {
        if (!isAlive) { return; }

        float before = currentHealth;
        currentHealth = Mathf.Min(data.maxHealth, currentHealth + Mathf.Max(0f, amount));
        float healed = currentHealth - before;

        mlAgent.AddReward(healed * rewardPerHealHP);

        if (before / data.maxHealth > 0.9f)
        {
            mlAgent.AddReward(-penaltyWastedHeal);
        }
    }

    /// <summary>
    /// Returns normalized cooldown remaining in [0,1]
    /// </summary>
    public float GetAttackCooldownNormalized()
    {
        if (data.attackCooldown <= 0f) 
        { 
            return 0f; 
        }

        float elapsed = Time.time - lastAttackTime;
        float remaining = Mathf.Clamp01(1f - (elapsed / data.attackCooldown));

        return remaining;
    }
}