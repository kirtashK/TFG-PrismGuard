using System.Collections;
using UnityEngine;

public class SoldierAttackState : ISoldierState
{
    private readonly ICombatTarget target;
    private float lastAttackTime;

    public SoldierAttackState(ICombatTarget target)
    {
        this.target = target;
    }

    public void EnterState(Soldier soldier)
    {
        soldier.agent.isStopped = true;
        lastAttackTime = -Mathf.Infinity;
    }

    public void UpdateState(Soldier soldier)
    {
        if (target == null || !target.isAlive)
        {
            soldier.HandleCombatEnd();
            return;
        }

        float distance = Vector3.Distance(soldier.transform.position, target.Position);
        if (distance > soldier.attackRange + 0.1f)
        {
            soldier.ChangeState(new SoldierChaseState(target));
            return;
        }

        if (Time.time - lastAttackTime >= soldier.attackCooldown)
        {
            target.TakeDamage(soldier.attackDamage, soldier.Position);
            lastAttackTime = Time.time;
        }
    }

    public void ExitState(Soldier soldier)
    {
        soldier.agent.isStopped = false;
    }
}
