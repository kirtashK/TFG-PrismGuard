using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
        enemy.unit.agent.stoppingDistance = enemy.unit.attackRange;
        enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(enemy.unit.target));
    }

    public void UpdateState(Enemy enemy)
    {
        if (enemy.unit.target == null || !enemy.unit.target.IsAlive)
        {
            if (enemy.MainTarget == null || !enemy.MainTarget.IsAlive)
            {
                enemy.ChangeState(new EnemyIdleState());
            }

            enemy.unit.target = enemy.MainTarget;
            enemy.ChangeState(new EnemyChaseState());
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
            && soldier != enemy.unit.target)
        {
            enemy.unit.target = soldier;
            enemy.ChangeState(new EnemyChaseState());
            return;
        }

        if (!enemy.unit.agent.pathPending
            && enemy.unit.agent.remainingDistance <= enemy.unit.attackRange)
        {
            enemy.ChangeState(new EnemyAttackState());
        }
        else
        {
            enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(enemy.unit.target));
        }
    }

    public void ExitState(Enemy enemy) 
    { 

    }
}
