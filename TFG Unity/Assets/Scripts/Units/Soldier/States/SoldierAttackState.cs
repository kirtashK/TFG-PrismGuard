using UnityEngine;

public class SoldierAttackState : ISoldierState
{
    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = true;
    }

    public void UpdateState(Soldier soldier)
    {
        if (soldier.unit.target == null || !soldier.unit.target.IsAlive)
        {
            soldier.HandleCombatEnd();
            return;
        }

        float distance = Vector3.Distance(soldier.transform.position, soldier.unit.GetTargetAttackPosition(soldier.unit.target));
        if (distance > soldier.unit.attackRange + 0.1f)
        {
            soldier.ChangeState(new SoldierChaseState());
            return;
        }

        soldier.unit.FaceTarget(soldier.unit.target.Position, 720f);

        if (Time.time >= soldier.unit.nextAttackTime)
        {
            soldier.unit.Attack(soldier.unit.target);

            soldier.unit.nextAttackTime = Time.time + soldier.unit.attackCooldown;
        }
    }

    public void ExitState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = false;
    }
}