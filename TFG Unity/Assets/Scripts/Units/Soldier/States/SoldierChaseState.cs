using UnityEngine;

public class SoldierChaseState : ISoldierState
{
    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = false;
        soldier.unit.agent.stoppingDistance = soldier.unit.attackRange;
    }

    public void UpdateState(Soldier soldier)
    {
        if (soldier.unit.target == null || !soldier.unit.target.IsAlive)
        {
            soldier.HandleCombatEnd();
            return;
        }

        soldier.unit.agent.SetDestination(soldier.unit.GetTargetAttackPosition(soldier.unit.target));

        float distance = Vector3.Distance(soldier.transform.position, soldier.unit.GetTargetAttackPosition(soldier.unit.target));
        if (distance <= soldier.unit.attackRange)
        {
            soldier.ChangeState(new SoldierAttackState());
        }
    }

    public void ExitState(Soldier soldier)
    {
        soldier.unit.agent.ResetPath();
    }
}
