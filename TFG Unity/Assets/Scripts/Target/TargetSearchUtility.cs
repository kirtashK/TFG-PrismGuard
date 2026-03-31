using UnityEngine;

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