using UnityEngine;

public class MovingState : IWorkerState
{
    // Optional destination, given by player order
    private readonly Vector3? explicitDestination;
    private Vector3 activeDestination;
    private bool usingTask;
    private float arrivalThreshold = 0.5f;

    public MovingState()
    {
        explicitDestination = null;
    }

    public MovingState(Vector3 destination, float arrivalThreshold = 0.5f)
    {
        this.explicitDestination = destination;
        this.arrivalThreshold = arrivalThreshold;
    }

    public void EnterState(Worker worker)
    {
        if (worker == null || worker.unit.agent == null)
        {
            return;
        }

        if (worker.currentTask != null)
        {
            usingTask = true;

            Vector3 taskPos = worker.currentTask.TaskPosition;
            Vector3 direction = (worker.transform.position - taskPos).normalized;
            Vector3 approachPosition = (direction != Vector3.zero)
                ? taskPos + direction * worker.currentTask.InteractionRange
                : taskPos;

            activeDestination = approachPosition;

            // Stop in task's interaction range
            worker.unit.agent.stoppingDistance = worker.CurrentTask.InteractionRange;
        }
        else if (explicitDestination.HasValue)
        {
            usingTask = false;
            activeDestination = explicitDestination.Value;
            worker.unit.agent.stoppingDistance = arrivalThreshold;
        }
        else
        {
            // Fallback, go idle
            worker.ChangeState(new IdleState());
            return;
        }

        worker.unit.agent.isStopped = false;
        worker.unit.agent.SetDestination(activeDestination);
    }

    public void UpdateState(Worker worker)
    {
        if (worker == null || worker.unit.agent == null)
        {
            return;
        }

        // If task is cancelled while moving, go idle
        if (usingTask && worker.currentTask == null)
        {
            worker.ChangeState(new IdleState());
            return;
        }

        // If still moving, break
        if (worker.unit.agent.pathPending)
        {
            return;
        }

        float remaining = worker.unit.agent.remainingDistance;
        bool arrived = remaining <= worker.unit.agent.stoppingDistance + 0.1f;

        // Check if we arrived to task or to order destination
        if (arrived)
        {
            if (usingTask && worker.currentTask != null)
            {
                worker.ChangeState(new WorkingState());
                return;
            }

            worker.ChangeState(new IdleState());
        }
    }

    public void ExitState(Worker worker)
    {
        // Restore default stoppingDistance
        if (worker != null && worker.unit.agent != null)
        {
            worker.unit.agent.stoppingDistance = 0.5f;
        }
    }
}
