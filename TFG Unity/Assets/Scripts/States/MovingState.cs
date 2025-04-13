using UnityEngine;

public class MovingState : IWorkerState
{
    public void EnterState(Worker worker)
    {
        if (worker.CurrentTask != null)
        {
            Debug.Log(worker.name + " - Moviéndose hacia: " + worker.CurrentTask.TaskPosition);
            worker.Agent.SetDestination(worker.CurrentTask.TaskPosition);
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
