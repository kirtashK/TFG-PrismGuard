using UnityEngine;

public interface ICombatTarget
{
    /// <summary>
    /// Position of the target
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Is alive or active
    /// </summary>
    bool isAlive { get; }

    /// <summary>
    /// Deals damage and tell from where
    /// </summary>
    /// <param name="amount">Damage dealt</param>
    /// <param name="attackOrigin">Position of the attacker</param>
    void TakeDamage(float amount, Vector3 attackOrigin);
}