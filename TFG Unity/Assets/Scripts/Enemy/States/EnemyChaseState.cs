using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseState : IEnemyState
{
    public void EnterState(Enemy enemy)
    {
        enemy.agent.SetDestination(enemy.crystalTransform.position);
    }

    public void UpdateState(Enemy enemy)
    {
        if (!enemy.agent.pathPending
            && enemy.agent.remainingDistance <= enemy.data.attackRange)
        {
            enemy.ChangeState(new EnemyAttackState());
        }
        else
        {
            enemy.agent.SetDestination(enemy.crystalTransform.position);
        }
    }

    public void ExitState(Enemy enemy) { }
}
