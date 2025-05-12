using UnityEngine;

public interface ICombatTarget
{
    /// <summary>
    /// Posición a la que dirigir la persecución.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Si ya ha sido destruido o está inactivo.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Inflige daño a este objetivo.
    /// </summary>
    void TakeDamage(float amount);
}