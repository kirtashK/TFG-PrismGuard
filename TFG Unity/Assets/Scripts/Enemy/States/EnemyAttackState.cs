using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyAttackState : IEnemyState
{
    private readonly ICombatTarget target;
    private bool onCooldown;

    public EnemyAttackState(ICombatTarget target)
    {
        this.target = target;
    }

    public void EnterState(Enemy enemy)
    {
        enemy.agent.isStopped = true;
        onCooldown = false;
    }

    public void UpdateState(Enemy enemy)
    {
        if (target == null || !target.IsAlive)
        {
            enemy.agent.isStopped = false;
            enemy.ChangeState(
                new EnemyChaseState(enemy.MainTarget)
            );
            return;
        }

        float dist = Vector3.Distance(
            enemy.transform.position,
            target.Position
        );
        if (dist > enemy.data.attackRange + 0.1f)
        {
            enemy.agent.isStopped = false;
            enemy.ChangeState(new EnemyChaseState(target));
            return;
        }

        if (!onCooldown)
        {
            Debug.Log($"{enemy.name} ataca {target}");

            target.TakeDamage(enemy.data.attackDamage);
            enemy.StartCoroutine(AttackCooldown(enemy));
        }
    }

    public void ExitState(Enemy enemy)
    {
        enemy.agent.isStopped = false;
    }

    private IEnumerator AttackCooldown(Enemy enemy)
    {
        onCooldown = true;
        yield return new WaitForSeconds(enemy.data.attackCooldown);
        onCooldown = false;
    }
}
