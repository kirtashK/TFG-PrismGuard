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
    /// Position where this Target can be attacked from. Useful for big targets where Position might not be accesible
    /// </summary>
    /// <param name="attackerPosition"></param>
    /// <param name="attackPosition"></param>
    /// <returns></returns>
    bool TryGetAttackPosition(Vector3 attackerPosition, out Vector3 attackPosition);

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