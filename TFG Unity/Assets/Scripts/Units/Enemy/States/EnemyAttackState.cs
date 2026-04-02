using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = true;
    }

    public void UpdateState(Enemy enemy)
    {
        if (enemy.target == null || !enemy.target.IsAlive)
        {
            enemy.unit.agent.isStopped = false;
            enemy.target = enemy.MainTarget;
            enemy.ChangeState(new EnemyChaseState());
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, enemy.unit.GetTargetAttackPosition(enemy.target));
        if (distance > enemy.unit.attackRange + 0.1f)
        {
            enemy.ChangeState(new EnemyChaseState());
            return;
        }

        enemy.unit.FaceTarget(enemy.target.Position, 720f);

        if (Time.time >= enemy.unit.nextAttackTime)
        {
            enemy.unit.Attack(enemy.target);

            enemy.unit.nextAttackTime = Time.time + enemy.unit.attackCooldown;
        }
    }

    public void ExitState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
    }
}