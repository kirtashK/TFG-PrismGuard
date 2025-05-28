using UnityEngine;

public class MovingState : IWorkerState
{
    public void EnterState(Worker worker)
    {
        if (worker.currentTask != null)
        {
            Vector3 taskPos = worker.currentTask.TaskPosition;
            Vector3 direction = (worker.transform.position - taskPos).normalized;
            Vector3 approachPosition = (direction != Vector3.zero)
                                         ? taskPos + direction * worker.currentTask.InteractionRange
                                         : taskPos;
            worker.agent.SetDestination(approachPosition);
        }
    }

    public void UpdateState(Worker worker)
    {
        if (!worker.agent.pathPending && worker.agent.remainingDistance < 0.5f)
        {
            worker.ChangeState(new WorkingState());
        }
    }

    public void ExitState(Worker worker)
    {

    }
}
