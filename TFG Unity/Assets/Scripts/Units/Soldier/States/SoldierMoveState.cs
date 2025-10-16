using UnityEngine;

public class SoldierMoveState : ISoldierState
{
    private Vector3 destination;
    private readonly bool attackMove;
    private readonly bool setAsGuard;

    private readonly Collider[] aggroBuffer = new Collider[16];

    public SoldierMoveState(Vector3 destination, bool attackMove, bool setAsGuard)
    {
        this.destination = destination;
        this.attackMove = attackMove;
        this.setAsGuard = setAsGuard;
    }

    public void EnterState(Soldier soldier)
    {
        soldier.agent.isStopped = false;
        soldier.agent.stoppingDistance = 0.5f;
        soldier.agent.SetDestination(destination);
    }

    public void UpdateState(Soldier soldier)
    {
        // If enemies and attackMove enabled, switch to chase/attack
        if (attackMove)
        {
            int hitCount = Physics.OverlapSphereNonAlloc
                (soldier.transform.position,
                soldier.data.AggroRadius,
                aggroBuffer,
                LayerMask.GetMask("EnemyUnit"));

            for (int i = 0; i < hitCount; i++)
            {
                ICombatTarget enemy = aggroBuffer[i].GetComponentInParent<ICombatTarget>();
                if (enemy != null && enemy.IsAlive)
                {
                    soldier.ChangeState(new SoldierChaseState(enemy));
                    break;
                }
            }
        }

        // If reached destination, set it as guardPoint if enabled and go idle
        if (!soldier.agent.pathPending && soldier.agent.remainingDistance <= soldier.agent.stoppingDistance + 0.1f)
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