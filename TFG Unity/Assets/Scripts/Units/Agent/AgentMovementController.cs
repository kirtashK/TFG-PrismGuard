using Unity.MLAgents;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovementController : MonoBehaviour
{
    private SoldierData data;

    private AgentSoldier agent;
    public NavMeshAgent navMeshAgent;

    [Tooltip("Distance considered reached")]
    public float stopDistance = 0.6f;

    private void Awake()
    {
        data = GetComponent<AgentSoldier>().data;

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
        agent = GetComponent<AgentSoldier>();

        navMeshAgent.speed = agent.moveSpeed;
        navMeshAgent.stoppingDistance = agent.attackRange;
    }

    public void SetDestination(Vector3 worldPosition)
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(worldPosition);
    }

    public void Stop()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (navMeshAgent == null)
        {
            return true;
        }

        if (!navMeshAgent.hasPath)
        {
            return true;
        }

        if (navMeshAgent.pathPending)
        {
            return false;
        }

        if (navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance, stopDistance))
        {
            return true;
        }

        return false;
    }
}