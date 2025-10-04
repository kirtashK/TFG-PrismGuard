using UnityEngine;

public class SoldierChaseState : ISoldierState
{
    private Soldier soldier;
    private readonly Transform target;

    public SoldierChaseState(Transform target)
    {
        this.target = target;
    }

    public void Enter(Soldier soldier)
    {
        this.soldier = soldier;
        soldier.agent.isStopped = false;
        soldier.agent.stoppingDistance = soldier.data.attackRange;
    }

    public void Update()
    {
        if (target == null)
        {
            soldier.HandleCombatEnd();
            return;
        }

        soldier.agent.SetDestination(target.position);

        float distance = Vector3.Distance(soldier.transform.position, target.position);
        if (distance <= soldier.data.attackRange)
        {
            soldier.ChangeState(new SoldierAttackState(target));
        }
    }

    public void Exit()
    {
        soldier.agent.ResetPath();
    }
}
