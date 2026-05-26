using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New AoE Attack Ability", menuName = "Data/Abilities/AoE Attack")]
public class AoEAttackAbility : HeroAbility
{
    [Header("AoE Attack")]

    [Tooltip("Should the ability be centered on the target? " +
        "If false, its centered on the hero")]
    public bool isTargetCenter = false;

    [Tooltip("Radius centered on Center")]
    public float aoeRadius = 3f;

    [Tooltip("Multiplier to hero's attackDamage stat")]
    public float damageMultiplier = 2f;

    [Tooltip("Reward per enemy hit by the AoE")]
    public float rewardPerTargetHit = 0.1f;

    [Tooltip("Reward per 1 HP of damage dealt")]
    public float rewardPerDamage = 0.01f;

    [SerializeField] private LayerMask enemyLayerMask;

    private static readonly Collider[] overlapBuffer = new Collider[32];

    public override void CollectObservations(AgentSoldier agent, VectorSensor sensor)
    {
        // Observation 1: nearby enemy count normalized
        List<ITarget> enemies = agent.GetNearbyEnemies();
        sensor.AddObservation(Mathf.Clamp01((float)enemies.Count / agent.kNearest));

        // Observation 2: average enemy distance normalized to aoeRadius
        if (enemies.Count == 0)
        {
            sensor.AddObservation(1f);
            return;
        }

        float totalDistance = 0f;
        for (int i = 0; i < enemies.Count; i++)
        {
            totalDistance += Vector3.Distance(agent.unit.Position, enemies[i].Position);
        }

        float avgDistance = totalDistance / enemies.Count;
        sensor.AddObservation(Mathf.Clamp01(avgDistance / aoeRadius));
    }

    public override bool IsUsable(AgentSoldier agent, ITarget target)
    {
        if (target == null || !target.IsAlive)
        {
            return false;
        }

        if (isTargetCenter)
        {
            return true;
        }

        float distSqr = (target.Position - agent.unit.Position).sqrMagnitude;
        return distSqr <= aoeRadius * aoeRadius;
    }

    public override AbilityResult Execute(AgentSoldier agent, ITarget target)
    {
        AbilityResult result = default;

        if (target == null || !target.IsAlive)
        {
            return result;
        }

        float damage = agent.unit.attackDamage * damageMultiplier;

        Vector3 center;
        if (isTargetCenter)
        {
            center = target.Position;
        }
        else
        {
            center = agent.unit.Position;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(center,
            aoeRadius, overlapBuffer, enemyLayerMask, QueryTriggerInteraction.Ignore);

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
                || hitTarget.Faction == agent.unit.Faction)
            {
                continue;
            }

            hitTarget.TakeDamage(damage, agent.unit.Position);
            result.targetsHit++;
            result.totalDamageDealt += damage;
        }

        Debug.Log($"{name}: {nameof(AoEAttackAbility)}: DMG {damage}, total DMG {result.totalDamageDealt}, enemies hit {result.targetsHit}");

        result.wasExecuted = true;
        return result;
    }

    public override float CalculateReward(AgentSoldier agent, AbilityResult result)
    {
        return result.targetsHit * rewardPerTargetHit
            + result.totalDamageDealt * rewardPerDamage;
    }
}