using UnityEngine;

public class SoldierAttackState : ISoldierState
{
    private readonly ICombatTarget target;

    public SoldierAttackState(ICombatTarget target)
    {
        this.target = target;
    }

    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = true;
    }

    public void UpdateState(Soldier soldier)
    {
        if (target == null || !target.isAlive)
        {
            soldier.HandleCombatEnd();
            return;
        }

        float distance = Vector3.Distance(soldier.transform.position, target.Position);
        if (distance > soldier.unit.attackRange + 0.1f)
        {
            soldier.ChangeState(new SoldierChaseState(target));
            return;
        }

        soldier.unit.FaceTarget(target.Position, 720f);

        if (Time.time >= soldier.unit.nextAttackTime)
        {
            soldier.unit.Attack(target);

            soldier.unit.nextAttackTime = Time.time + soldier.unit.attackCooldown;
        }
    }

    public void ExitState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = false;
    }
}