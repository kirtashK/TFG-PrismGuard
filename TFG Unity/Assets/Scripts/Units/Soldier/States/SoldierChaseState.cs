using UnityEngine;

public class SoldierChaseState : ISoldierState
{
    private readonly ICombatTarget target;

    public SoldierChaseState(ICombatTarget target)
    {
        this.target = target;
    }

    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = false;
        soldier.unit.agent.stoppingDistance = soldier.unit.attackRange;
    }

    public void UpdateState(Soldier soldier)
    {
        if (target == null || !target.IsAlive)
        {
            soldier.HandleCombatEnd();
            return;
        }

        soldier.unit.agent.SetDestination(target.Position);

        float distance = Vector3.Distance(soldier.transform.position, target.Position);
        if (distance <= soldier.unit.attackRange)
        {
            soldier.ChangeState(new SoldierAttackState(target));
        }
    }

    public void ExitState(Soldier soldier)
    {
        soldier.unit.agent.ResetPath();
    }
}
