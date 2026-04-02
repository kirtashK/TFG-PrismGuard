using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
        enemy.unit.agent.stoppingDistance = enemy.unit.attackRange;
        enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(enemy.target));
    }

    public void UpdateState(Enemy enemy)
    {
        if (enemy.target == null || !enemy.target.IsAlive)
        {
            enemy.target = enemy.MainTarget;
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
            && soldier != enemy.target)
        {
            enemy.target = soldier;
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
            enemy.unit.agent.SetDestination(enemy.unit.GetTargetAttackPosition(enemy.target));
        }
    }

    public void ExitState(Enemy enemy) 
    { 

    }
}
