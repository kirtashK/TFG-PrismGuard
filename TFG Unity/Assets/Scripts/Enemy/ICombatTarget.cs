using UnityEngine;

public interface ICombatTarget
{
    /// <summary>
    /// Posición a la que dirigir la persecución
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Si ya ha sido destruido o está inactivo
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Inflige daño y comunica desde qué punto viene el ataque.
    /// </summary>
    /// <param name="amount">Daño a infligir</param>
    /// <param name="attackOrigin">Posicion del atacante</param>
    void TakeDamage(float amount, Vector3 attackOrigin);
}