using UnityEngine;

public class SoldierIdleState : ISoldierState
{
    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = true;
    }

    public void UpdateState(Soldier soldier)
    {
        ITarget target = soldier.FindNearestEnemyTarget();

        if (target != null)
        {
            soldier.ChangeState(new SoldierChaseState(target));
        }
    }

    public void ExitState(Soldier soldier)
    {

    }
}