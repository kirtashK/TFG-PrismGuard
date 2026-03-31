using UnityEngine;

public class SoldierMoveState : ISoldierState
{
    private Vector3 destination;
    private readonly bool attackMove;
    private readonly bool setAsGuard;

    public SoldierMoveState(Vector3 destination, bool attackMove, bool setAsGuard)
    {
        this.destination = destination;
        this.attackMove = attackMove;
        this.setAsGuard = setAsGuard;
    }

    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = false;
        soldier.unit.agent.stoppingDistance = 0.5f;
        soldier.unit.agent.SetDestination(destination);
    }

    public void UpdateState(Soldier soldier)
    {
        // If enemies and attackMove enabled, switch to chase/attack
        if (attackMove)
        {
            ITarget target = soldier.FindNearestEnemyTarget();

            if (target != null)
            {
                soldier.ChangeState(new SoldierChaseState(target));
            }
        }

        // If reached destination, set it as guardPoint if enabled and go idle
        if (!soldier.unit.agent.pathPending && soldier.unit.agent.remainingDistance <= soldier.unit.agent.stoppingDistance + 0.1f)
        {
            if (setAsGuard)
            {
                soldier.SetGuardPoint(destination, true);
            }
            soldier.ChangeState(new SoldierIdleState());
        }
    }

    public void ExitState(Soldier soldier)
    {
        
    }
}