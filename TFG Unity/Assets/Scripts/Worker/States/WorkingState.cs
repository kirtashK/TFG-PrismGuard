using UnityEngine;

public class WorkingState : IWorkerState
{
    private bool started = false;

    public void EnterState(Worker worker)
    {
        started = false;
    }

    public void UpdateState(Worker worker)
    {
        if (!started && worker.currentTask != null)
        {
            started = true;
            if (worker.currentTask is MoveItemTask)
            {
                worker.ChangeState(new MultiTransportState());
            }
            else
            {
                worker.currentTask.Execute(worker, () =>
                {
                    worker.currentTask = null;
                    worker.ChangeState(new IdleState());
                });
            }
        }
    }

    public void ExitState(Worker worker)
    {
        
    }
}
