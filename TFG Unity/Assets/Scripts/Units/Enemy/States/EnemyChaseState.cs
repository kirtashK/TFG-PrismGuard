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
        enemy.agent.stoppingDistance = enemy.attackRange;
        enemy.agent.SetDestination(target.Position);
    }

    public void UpdateState(Enemy enemy)
    {
        if (target == null || !target.isAlive)
        {
            enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));
            return;
        }

        if (enemy.behaviour == Enemy.Behaviour.Guard)
        {
            float guardChaseRadius = enemy.guardRadius + enemy.guardChaseBuffer;
            float distanceFromHome = Vector3.Distance(enemy.Position, enemy.homePosition);
            if (distanceFromHome > guardChaseRadius)
            {
                enemy.ChangeState(new EnemyGuardState(enemy.homePosition, enemy.guardRadius, enemy.guardChaseBuffer));
                return;
            }
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
            && enemy.agent.remainingDistance <= enemy.attackRange)
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
