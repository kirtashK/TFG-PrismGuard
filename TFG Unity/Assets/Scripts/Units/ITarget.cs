using System;
using System.Collections;
using UnityEngine;

public enum Faction
{
    Player,
    Enemy,
    Neutral,
}

public enum Category
{
    Unit,
    Structure,
}

public interface ITarget
{
    /// <summary>
    /// Position of the Target
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Is Target alive or active?
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Faction this Target belongs to
    /// </summary>
    Faction Faction { get; }

    /// <summary>
    /// Category of this Target
    /// </summary>
    Category Category { get; }

    /// <summary>
    /// Deals damage to the Target, specifying from where
    /// </summary>
    /// <param name="amount">Damage dealt to Target</param>
    /// <param name="attackOrigin">Position of the attacker</param>
    void TakeDamage(float amount, Vector3 attackOrigin);

    /// <summary>
    /// Called when a Target takes damage
    /// </summary>
    /// <param name="amount">Damage received</param>
    /// <param name="attackOrigin"></param>
    void OnDamageTaken(float amount, Vector3 attackOrigin);

    /// <summary>
    /// Event with damage amount & origin of the attack
    /// </summary>
    public event Action<float, Vector3> OnDamageTakenEvent;

    /// <summary>
    /// Heal the target's hp by a flat value
    /// </summary>
    /// <param name="healAmount"></param>
    void Heal(float healAmount);

    /// <summary>
    /// Heal the target's hp by a percentage of the max hp
    /// </summary>
    /// <param name="percent">Percent of max hp to heal</param>
    void HealPercentage(float percent);

    /// <summary>
    /// Deal damage to Target
    /// </summary>
    /// <param name="combatTarget"></param>
    void Attack(ITarget combatTarget);

    /// <summary>
    /// Called when the attack hits the Target, for animations or weapon colliders
    /// </summary>
    public void OnAttackHit();

    void Die();

    /// <summary>
    /// Called near the start of a Target diying
    /// </summary>
    void OnDeathStarted();

    IEnumerator DeathCleanupDelay();

    /// <summary>
    /// Call OnDeathCleanups, release addressable handle (if any) and destroys Target
    /// </summary>
    void CompleteDeathCleanup();

    /// <summary>
    /// Called near the end of a Target diying
    /// </summary>
    void OnDeathCleanup();

    public event Action OnDeathStartedEvent;
    public event Action OnDeathCleanupEvent;
}

public static class TargetSearchUtility
{
    private static readonly Collider[] overlapBuffer = new Collider[16];

    /// <summary>
    /// Searches the closest target with faction different than requesterFaction.
    /// Prioritizes units over structures.
    /// If crystal fallback is provided and requesterFaction is enemy, returns it when no targets are found
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="searchRadius"></param>
    /// <param name="requesterFaction">Faction of the requester</param>
    /// <param name="crystalFallback">Crystal target, only useful for enemy faction</param>
    /// <returns>Closest target found of a different faction, prioritizing units over structures</returns>
    public static ITarget SearchTarget(Vector3 origin, float searchRadius,
        Faction requesterFaction, ITarget crystalFallback = null)
    {
        int enemyFactionMask = requesterFaction == Faction.Player
            ? LayerMask.GetMask("EnemyUnit", "Structure")
            : LayerMask.GetMask("PlayerUnit", "Structure");

        int hitCount = Physics.OverlapSphereNonAlloc(origin, searchRadius,
            overlapBuffer, enemyFactionMask);

        if (hitCount <= 0)
        {
            return GetCrystalFallback(requesterFaction, crystalFallback);
        }

        ITarget bestUnit = null;
        float bestUnitDistance = float.MaxValue;

        ITarget bestStructure = null;
        float bestStructureDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = overlapBuffer[i];
            if (collider == null)
            {
                continue;
            }

            ITarget target = collider.GetComponentInParent<ITarget>();
            if (target == null || !target.IsAlive)
            {
                continue;
            }

            if (target.Faction == requesterFaction)
            {
                continue;
            }

            float distance = Vector3.Distance(origin, target.Position);

            if (target.Category == Category.Unit)
            {
                if (distance < bestUnitDistance)
                {
                    bestUnitDistance = distance;
                    bestUnit = target;
                }
            }
            else if (requesterFaction == Faction.Enemy && target.Category == Category.Structure)
            {
                if (distance < bestStructureDistance)
                {
                    bestStructureDistance = distance;
                    bestStructure = target;
                }
            }
        }

        if (bestUnit != null)
        {
            return bestUnit;
        }

        if (bestStructure != null)
        {
            return bestStructure;
        }

        return GetCrystalFallback(requesterFaction, crystalFallback);
    }

    private static ITarget GetCrystalFallback(Faction requesterFaction, ITarget crystalFallback)
    {
        if (requesterFaction == Faction.Enemy
            && crystalFallback != null
            && crystalFallback.IsAlive)
        {
            return crystalFallback;
        }

        return null;
    }
}