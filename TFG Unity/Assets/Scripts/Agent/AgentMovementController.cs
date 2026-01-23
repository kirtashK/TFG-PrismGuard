using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovementController : MonoBehaviour
{
    private SoldierData data;

    public NavMeshAgent agent;

    [Tooltip("Distance considered reached")]
    public float stopDistance = 0.6f;

    private void Awake()
    {
        data = GetComponent<AgentSoldier>().data;

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        agent.speed = data.moveSpeed;
        agent.stoppingDistance = data.attackRange;
    }

    public void SetDestination(Vector3 worldPosition)
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(worldPosition);
    }

    public void Stop()
    {
        if (agent == null)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (agent == null)
        {
            return true;
        }

        if (!agent.hasPath)
        {
            return true;
        }

        if (agent.pathPending)
        {
            return false;
        }

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, stopDistance))
        {
            return true;
        }

        return false;
    }
}