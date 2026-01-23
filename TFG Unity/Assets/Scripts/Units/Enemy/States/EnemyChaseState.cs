using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseState : IEnemyState
{
    private readonly ICombatTarget target;

    public EnemyChaseState(ICombatTarget target)
    {
        this.target = target;
    }

    public void EnterState(Enemy enemy)
    {
        enemy.agent.isStopped = false;
        enemy.agent.stoppingDistance = enemy.data.attackRange;
        enemy.agent.SetDestination(target.Position);
    }

    public void UpdateState(Enemy enemy)
    {
        if (target == null || !target.isAlive)
        {
            enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));
            return;
        }

        ICombatTarget soldier = enemy.FindNearestPlayerUnit();
        if (soldier != null 
            && soldier.isAlive
            && soldier != target)
        {
            enemy.ChangeState(new EnemyChaseState(soldier));
            return;
        }

        if (!enemy.agent.pathPending
            && enemy.agent.remainingDistance <= enemy.data.attackRange)
        {
            enemy.ChangeState(new EnemyAttackState(target));
        }
        else
        {
            enemy.agent.SetDestination(target.Position);
        }
    }

    public void ExitState(Enemy enemy) 
    { 

    }
}
