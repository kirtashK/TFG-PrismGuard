using UnityEngine;

public class SoldierIdleState : ISoldierState
{
    private Soldier soldier;

    private readonly int maxColliders = 10;

    public void Enter(Soldier soldier)
    {
        this.soldier = soldier;
        soldier.agent.isStopped = true;
    }

    public void Update()
    {
        Collider[] aggroBuffer = new Collider[maxColliders];

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