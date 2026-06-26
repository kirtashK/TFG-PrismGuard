using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TargetSearchUtility
{
    public static List<ITarget> SortTargets(IEnumerable<ITarget> targets, Vector3 origin, bool prioritizeUnits = false)
    {
        if (targets == null)
        {
            return new List<ITarget>();
        }

        IEnumerable<ITarget> validTargets = targets.Where(target => target != null && target.IsAlive);

        if (prioritizeUnits)
        {
            validTargets = validTargets
                .OrderByDescending(target => target.Category == Category.Unit ? 1 : 0)
                .ThenBy(target => (target.Position - origin).sqrMagnitude);
        }
        else
        {
            validTargets = validTargets.OrderBy(target => (target.Position - origin).sqrMagnitude);
        }

        return validTargets.ToList();
    }

    public static ITarget GetBestTarget(IEnumerable<ITarget> targets, Vector3 origin, Faction requesterFaction, ITarget crystalFallback = null)
    {
        List<ITarget> sortedTargets = SortTargets(targets, origin, prioritizeUnits: true);

        ITarget bestUnit = sortedTargets.FirstOrDefault(target => target.Category == Category.Unit && target.Faction != requesterFaction);
        if (bestUnit != null)
        {
            return bestUnit;
        }

        ITarget bestStructure = sortedTargets.FirstOrDefault(target => target.Category == Category.Structure && target.Faction != requesterFaction);
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