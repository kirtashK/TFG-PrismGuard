using UnityEngine;

public class SoldierIdleState : ISoldierState
{
    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = true;
    }

    public void UpdateState(Soldier soldier)
    {
        ITarget target = soldier.FindNearestPlayerTarget();

        if (target != null)
        {
            soldier.ChangeState(new SoldierChaseState(target));
        }
    }

    public void ExitState(Soldier soldier)
    {

    }
}