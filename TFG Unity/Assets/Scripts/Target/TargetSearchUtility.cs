using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TargetSearchUtility
{
    private static readonly Collider[] overlapBuffer = new Collider[16];

    /// <summary>
    /// Searches all targets within searchRadius with faction different than requesterFaction, sorted by distance
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="searchRadius"></param>
    /// <param name="requesterFaction">Faction of the requester</param>
    /// <returns>All targets found, sorted by distance</returns>
    public static List<ITarget> SearchTargets(Vector3 origin, float searchRadius, Faction requesterFaction)
    {
        int enemyFactionMask = requesterFaction == Faction.Player
            ? LayerMask.GetMask("EnemyUnit", "Structure")
            : LayerMask.GetMask("PlayerUnit", "Structure");

        int hitCount = Physics.OverlapSphereNonAlloc(origin, searchRadius, overlapBuffer, enemyFactionMask);

        List<ITarget> targets = new();

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

            targets.Add(target);
        }

        return targets
            .OrderBy(target => Vector3.SqrMagnitude(target.Position - origin))
            .ToList();
    }

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
    public static ITarget SearchTarget(Vector3 origin, float searchRadius, Faction requesterFaction, ITarget crystalFallback = null)
    {
        List<ITarget> targets = SearchTargets(origin, searchRadius, requesterFaction);

        ITarget bestUnit = targets.FirstOrDefault(target => target.Category == Category.Unit);
        if (bestUnit != null)
        {
            return bestUnit;
        }

        ITarget bestStructure = targets.FirstOrDefault(target => target.Category == Category.Structure);
        if (bestStructure != null && requesterFaction == Faction.Enemy)
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