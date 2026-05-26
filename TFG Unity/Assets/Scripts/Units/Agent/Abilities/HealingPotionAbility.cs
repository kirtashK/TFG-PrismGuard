using Unity.MLAgents.Sensors;
using UnityEngine;

[CreateAssetMenu(fileName = "New Healing Potion Ability", menuName = "Data/Abilities/Healing Potion")]
public class HealingPotionAbility : HeroAbility
{
    [Header("Healing Potion")]

    [Tooltip("Percentage of max HP to restore")]
    [Range(0f, 1f)]
    public float healPercent = 0.45f;

    [Tooltip("Reward per 1 HP healed")]
    public float rewardPerHealHP = 0.02f;

    [Tooltip("Penalty per 1 HP of healing wasted above max HP")]
    public float penaltyPerWastedHP = 0.05f;

    [Tooltip("Only use potion when heal % is below threshold")]
    [Range(0f, 1f)]
    public float usableHealthThreshold = 0.6f;

    public override void CollectObservations(AgentSoldier agent, VectorSensor sensor)
    {
        // Observation 1: missing health normalized
        float healthNorm = Mathf.Clamp01(agent.unit.currentHealth / agent.unit.maxHealth);
        sensor.AddObservation(1f - healthNorm);

        // Observation 2: padding
        sensor.AddObservation(0f);
    }

    public override bool IsUsable(AgentSoldier agent, ITarget target)
    {
        float healthFraction = agent.unit.currentHealth / agent.unit.maxHealth;
        return healthFraction < usableHealthThreshold;
    }

    public override AbilityResult Execute(AgentSoldier agent, ITarget target)
    {
        AbilityResult result = default;

        float healAmount = agent.unit.maxHealth * healPercent;
        float healthBefore = agent.unit.currentHealth;

        agent.unit.Heal(healAmount);

        float actualHealed = agent.unit.currentHealth - healthBefore;
        float wasted = healAmount - actualHealed;

        result.wasExecuted = true;
        result.totalHealingDone = actualHealed;
        result.healingWasted = Mathf.Max(0f, wasted);

        Debug.Log($"{name}: {nameof(HealingPotionAbility)}: Heal {healAmount}, actual heal {actualHealed}, wasted {wasted}");

        return result;
    }

    public override float CalculateReward(AgentSoldier agent, AbilityResult result)
    {
        return result.totalHealingDone * rewardPerHealHP
            - result.healingWasted * penaltyPerWastedHP;
    }
}