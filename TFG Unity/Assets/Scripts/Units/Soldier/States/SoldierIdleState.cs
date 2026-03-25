using UnityEngine;

public class SoldierIdleState : ISoldierState
{
    private readonly Collider[] aggroBuffer = new Collider[16];

    public void EnterState(Soldier soldier)
    {
        soldier.unit.agent.isStopped = true;
    }

    public void UpdateState(Soldier soldier)
    {
        int hitCount = Physics.OverlapSphereNonAlloc
            (soldier.transform.position,
            soldier.aggroRadius,
            aggroBuffer,
            LayerMask.GetMask("EnemyUnit"));

        for (int i = 0; i < hitCount; i++)
        {
            ICombatTarget enemy = aggroBuffer[i].GetComponentInParent<ICombatTarget>();
            if (enemy != null && enemy.IsAlive)
            {
                soldier.ChangeState(new SoldierChaseState(enemy));
                break;
            }
        }
    }

    public void ExitState(Soldier soldier)
    {

    }
}