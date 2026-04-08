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

        if (target != null && target.IsAlive)
        {
            soldier.unit.target = target;
            soldier.ChangeState(new SoldierChaseState());
        }
    }

    public void ExitState(Soldier soldier)
    {

    }
}