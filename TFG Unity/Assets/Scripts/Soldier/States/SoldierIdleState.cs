using UnityEngine;

public class SoldierIdleState : ISoldierState
{
    private Soldier soldier;

    private readonly Collider[] aggroBuffer = new Collider[16];

    public void Enter(Soldier soldier)
    {
        this.soldier = soldier;
        soldier.agent.isStopped = true;
    }

    public void Update()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            soldier.transform.position,
            soldier.data.AggroRadius,
            aggroBuffer,
            LayerMask.GetMask("EnemyUnit")
        );

        for (int i = 0; i < hitCount; i++)
        {
            Enemy enemy = aggroBuffer[i].GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive)
            {
                soldier.ChangeState(new SoldierChaseState(enemy.transform));
                break;
            }
        }
    }

    public void Exit()
    {

    }
}