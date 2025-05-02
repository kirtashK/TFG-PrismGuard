using UnityEngine;

public class MovingState : IWorkerState
{
    public void EnterState(Worker worker)
    {
        if (worker.CurrentTask != null)
        {
            Vector3 taskPos = worker.CurrentTask.TaskPosition;
            Vector3 direction = (worker.transform.position - taskPos).normalized;
            Vector3 approachPosition = (direction != Vector3.zero)
                                         ? taskPos + direction * worker.CurrentTask.InteractionRange
                                         : taskPos;
            worker.Agent.SetDestination(approachPosition);
        }
    }

    public void UpdateState(Worker worker)
    {
        if (!worker.Agent.pathPending && worker.Agent.remainingDistance < 0.5f)
        {
            worker.ChangeState(new WorkingState());
        }
    }

    public void ExitState(Worker worker)
    {

    }
}
