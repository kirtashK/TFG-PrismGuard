using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly ITarget target;

    public EnemyChaseState(ITarget target)
    {
        this.target = target;
    }

    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
        enemy.unit.agent.stoppingDistance = enemy.unit.attackRange;
        enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(target));
    }

    public void UpdateState(Enemy enemy)
    {
        if (target == null || !target.IsAlive)
        {
            enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));
            return;
        }

        if (enemy.behaviour == Enemy.Behaviour.Guard)
        {
            float guardChaseRadius = enemy.guardRadius + enemy.guardChaseBuffer;
            float distanceFromHome = Vector3.Distance(enemy.unit.Position, enemy.homePosition);
            if (distanceFromHome > guardChaseRadius)
            {
                enemy.ChangeState(new EnemyGuardState(enemy.homePosition, enemy.guardRadius, enemy.guardChaseBuffer));
                return;
            }
        }

        ITarget soldier = enemy.FindNearestPlayerTarget();

        if (soldier != null 
            && soldier.IsAlive
            && soldier != target)
        {
            enemy.ChangeState(new EnemyChaseState(soldier));
            return;
        }

        if (!enemy.unit.agent.pathPending
            && enemy.unit.agent.remainingDistance <= enemy.unit.attackRange)
        {
            enemy.ChangeState(new EnemyAttackState(target));
        }
        else
        {
            enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(target));
        }
    }

    public void ExitState(Enemy enemy) 
    { 

    }
}
