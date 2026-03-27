using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private readonly ITarget target;

    public EnemyAttackState(ITarget target)
    {
        this.target = target;
    }

    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = true;
    }

    public void UpdateState(Enemy enemy)
    {
        if (target == null || !target.IsAlive)
        {
            enemy.unit.agent.isStopped = false;
            enemy.ChangeState(new EnemyChaseState(enemy.MainTarget));
            return;
        }

        float distance = Vector3.Distance(enemy.transform.position, target.Position);
        if (distance > enemy.unit.attackRange + 0.1f)
        {
            enemy.ChangeState(new EnemyChaseState(target));
            return;
        }

        enemy.unit.FaceTarget(target.Position, 720f);

        if (Time.time >= enemy.unit.nextAttackTime)
        {
            enemy.unit.Attack(target);

            enemy.unit.nextAttackTime = Time.time + enemy.unit.attackCooldown;
        }
    }

    public void ExitState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
    }
}