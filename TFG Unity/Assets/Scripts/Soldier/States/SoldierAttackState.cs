using System.Collections;
using UnityEngine;

public class SoldierAttackState : ISoldierState
{
    private Soldier soldier;
    private readonly Transform target;
    private float lastAttackTime;

    public SoldierAttackState(Transform target)
    {
        this.target = target;
    }

    public void Enter(Soldier soldier)
    {
        this.soldier = soldier;
        soldier.agent.isStopped = true;
        lastAttackTime = -Mathf.Infinity;
    }

    public void Update()
    {
        if (target == null)
        {
            soldier.HandleCombatEnd();
            return;
        }

        float distance = Vector3.Distance(soldier.transform.position, target.position);
        if (distance > soldier.data.attackRange + 0.1f)
        {
            soldier.ChangeState(new SoldierChaseState(target));
            return;
        }

        if (Time.time - lastAttackTime >= soldier.data.attackCooldown)
        {
            ICombatTarget targetComponent = target.GetComponent<ICombatTarget>();
            if (targetComponent != null && targetComponent.IsAlive)
            {
                targetComponent.TakeDamage(soldier.data.attackDamage, soldier.transform.position);
                lastAttackTime = Time.time;
            }
            else
            {
                soldier.HandleCombatEnd();
            }
        }
    }

    public void Exit()
    {
        soldier.agent.isStopped = false;
    }
}
