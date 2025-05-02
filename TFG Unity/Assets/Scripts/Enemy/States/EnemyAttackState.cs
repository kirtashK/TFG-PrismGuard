using UnityEngine;
using System.Collections;
using TMPro;

public class EnemyAttackState : IEnemyState
{
    private bool onCooldown;

    public void EnterState(Enemy enemy)
    {
        onCooldown = false;
        enemy.agent.isStopped = true;
    }

    public void UpdateState(Enemy enemy)
    {
        if (enemy.agent.remainingDistance > enemy.data.attackRange + 0.1f)
        {
            enemy.agent.isStopped = false;
            enemy.ChangeState(new EnemyChaseState());
            return;
        }

        if (!onCooldown)
        {
            enemy.crystalTransform.GetComponent<Crystal>()?.
                TakeDamage(enemy.data.attackDamage);

            Debug.Log("Atacado con daño = " + enemy.data.attackRange);

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
