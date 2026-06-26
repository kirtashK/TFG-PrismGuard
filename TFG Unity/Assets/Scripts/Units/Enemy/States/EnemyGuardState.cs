using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy stays around homePosition, 
/// wanders inside guardRadius,
/// detects player units inside AggroRadius and switches to chase
/// </summary>
public class EnemyGuardState : IEnemyState
{
    private readonly Vector3 homePosition;
    private readonly float guardRadius;
    private readonly float leashBuffer;

    private readonly float wanderPointTolerance = 0.5f;
    private Vector3 currentWanderTarget;
    private bool hasWanderTarget;

    private readonly float detectionInterval = 0.25f;
    private float detectionTimer;

    private bool isWaiting;
    private float waitTimer;

    public EnemyGuardState(Vector3 homePosition, float guardRadius, float leashBuffer)
    {
        this.homePosition = homePosition;
        this.guardRadius = guardRadius;
        this.leashBuffer = leashBuffer;
    }

    public void EnterState(Enemy enemy)
    {
        enemy.unit.agent.isStopped = false;
        enemy.unit.agent.stoppingDistance = 0;

        hasWanderTarget = false;
        detectionTimer = 0f;
        isWaiting = false;
        waitTimer = 0f;
    }

    public void UpdateState(Enemy enemy)
    {
        detectionTimer -= Time.deltaTime;
        if (detectionTimer <= 0f)
        {
            detectionTimer = detectionInterval;

            ITarget intruder = enemy.FindNearestPlayerTarget(isGuarding: true);

            if (intruder != null && intruder.IsAlive)
            {
                enemy.unit.target = intruder;
                enemy.ChangeState(new EnemyChaseState());
                return;
            }
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                hasWanderTarget = false;
            }
            else
            {
                return;
            }
        }

        // Keep wandering within guardRadius
        if (!hasWanderTarget || Vector3.Distance(enemy.unit.Position, currentWanderTarget) <= wanderPointTolerance)
        {
            if (hasWanderTarget && Vector3.Distance(enemy.unit.Position, currentWanderTarget) <= wanderPointTolerance)
            {
                float minDelay = Mathf.Min(enemy.patrolDelayMin, enemy.patrolDelayMax);
                float maxDelay = Mathf.Max(enemy.patrolDelayMin, enemy.patrolDelayMax);
                waitTimer = Random.Range(minDelay, maxDelay);
                isWaiting = true;
                return;
            }

            Vector3 randomPoint = Random.insideUnitCircle * guardRadius;
            Vector3 candidate = homePosition + new Vector3(randomPoint.x, 0f, randomPoint.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                currentWanderTarget = hit.position;
                hasWanderTarget = true;
                enemy.unit.agent.SetDestination(currentWanderTarget);
            }
            else
            {
                // Fallback to home
                currentWanderTarget = homePosition;
                hasWanderTarget = true;
                enemy.unit.agent.SetDestination(currentWanderTarget);
            }
        }

        // Dont wander far from home
        float distanceFromHome = Vector3.Distance(enemy.unit.Position, homePosition);
        if (distanceFromHome > guardRadius + leashBuffer)
        {
            hasWanderTarget = false;
            enemy.unit.agent.SetDestination(homePosition);
        }
    }

    public void ExitState(Enemy enemy)
    {
        hasWanderTarget = false;
        enemy.unit.agent.stoppingDistance = enemy.unit.attackRange;
    }
}