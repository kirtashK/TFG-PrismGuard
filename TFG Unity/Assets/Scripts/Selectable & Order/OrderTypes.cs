using UnityEngine;

public struct MoveOrderOptions
{
    public bool returnToGuard;     // If true, set destination as guard point to return
    public bool attackMove;        // If true, unit may attack while moving
    public float formationSpacing; // Spacing between units in formation

    public static MoveOrderOptions Default => new()
    {
        returnToGuard = true,
        attackMove = true,
        formationSpacing = 1.5f
    };
}

public interface IOrderable
{
    void ReceiveMoveOrder(Vector3 destination, MoveOrderOptions options);
}

public interface IGuardable
{
    void SetGuardPoint(Vector3 point, bool returnToGuard);
    void ClearGuardPoint();
}

public interface IAttackMovable
{
    void SetAttackMove(bool toggle);
}