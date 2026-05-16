using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

[CreateAssetMenu(fileName = "New AoE Heal Ability", menuName = "Data/Abilities/AoE Heal")]
public class AoEHealAbility : HeroAbility
{
    [Header("AoE Heal")]

    [Tooltip("Radius centered on the hero")]
    public float aoeRadius = 4f;

    [Tooltip("Percentage of max HP to restore per target")]
    [Range(0f, 1f)]
    public float healPercent = 0.3f;

    [Tooltip("Health fraction below which the ability is considered usable")]
    [Range(0f, 1f)]
    public float usableHealthThreshold = 0.75f;

    [Tooltip("Reward per 1 HP healed across all targets")]
    public float rewardPerHealHP = 0.02f;

    [Tooltip("Penalty per 1 HP of healing wasted above max HP across all targets")]
    public float penaltyPerWastedHP = 0.04f;

    [SerializeField] private LayerMask playerLayerMask;

    private static readonly Collider[] overlapBuffer = new Collider[32];

    public override void CollectObservations(AgentSoldier agent, VectorSensor sensor)
    {
        List<ITarget> allies = agent.GetNearbyAllies();

        // Observation 1: nearby ally count normalized
        int maxAllies = 8;
        sensor.AddObservation(Mathf.Clamp01((float)allies.Count / maxAllies));

        // Observation 2: average missing health of nearby allies + self normalized
        float totalMissingFraction = 1f - Mathf.Clamp01(
            agent.unit.currentHealth / agent.unit.maxHealth);

        for (int i = 0; i < allies.Count; i++)
        {
            totalMissingFraction += 1f - Mathf.Clamp01(
                allies[i].CurrentHealth / allies[i].MaxHealth);
        }

        int totalTargets = allies.Count + 1;
        sensor.AddObservation(totalMissingFraction / totalTargets);
    }

    public override bool IsUsable(AgentSoldier agent, ITarget target)
    {
        // Usable if the hero itself is below the threshold
        float selfFraction = agent.unit.currentHealth / agent.unit.maxHealth;
        if (selfFraction < usableHealthThreshold)
        {
            return true;
        }

        // Usable if any nearby ally is below the threshold
        List<ITarget> allies = agent.GetNearbyAllies();
        for (int i = 0; i < allies.Count; i++)
        {
            float allyFraction = allies[i].CurrentHealth / allies[i].MaxHealth;
            if (allyFraction < usableHealthThreshold)
            {
                return true;
            }
        }

        return false;
    }

    public override AbilityResult Execute(AgentSoldier agent, ITarget target)
    {
        AbilityResult result = default;

        // Heal self
        ApplyHealToTarget(agent.unit, agent.unit.maxHealth, ref result);

        // Heal allies within aoeRadius of the hero
        int hitCount = Physics.OverlapSphereNonAlloc(agent.unit.Position,
            aoeRadius, overlapBuffer, playerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
            {
                continue;
            }

            ITarget hitTarget = hit.GetComponentInParent<ITarget>();
            if (hitTarget == null 
                || !hitTarget.IsAlive
                || hitTarget == agent.unit as ITarget
                || hitTarget.Faction != agent.unit.Faction
                || hitTarget.CurrentHealth == hitTarget.MaxHealth)
            {
                continue;
            }

            ApplyHealToTarget(hitTarget, hitTarget.MaxHealth, ref result);
            result.targetsHit++;
        }

        Debug.Log($"{name}: {nameof(AoEHealAbility)}: targets healed {result.targetsHit}, Total healed {result.totalHealingDone}," +
            $"Total healing wasted {result.healingWasted}");

        result.wasExecuted = true;
        return result;
    }

    public override float CalculateReward(AgentSoldier agent, AbilityResult result)
    {
        return result.totalHealingDone * rewardPerHealHP
            - result.healingWasted * penaltyPerWastedHP;
    }

    private void ApplyHealToTarget(ITarget healTarget, float targetMaxHealth, ref AbilityResult result)
    {
        float healAmount = targetMaxHealth * healPercent;
        float healthBefore = healTarget.CurrentHealth;

        healTarget.Heal(healAmount);

        float actualHealed = healTarget.CurrentHealth - healthBefore;
        float wasted = healAmount - actualHealed;

        result.totalHealingDone += actualHealed;
        result.healingWasted += Mathf.Max(0f, wasted);
    }
}