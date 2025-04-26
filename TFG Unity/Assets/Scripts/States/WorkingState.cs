using UnityEngine;

public class WorkingState : IWorkerState
{
    private bool started = false;

    public void EnterState(Worker worker)
    {
        //Debug.Log(worker.name + " - Comenzando tarea.");
        started = false;
    }

    public void UpdateState(Worker worker)
    {
        if (!started && worker.CurrentTask != null)
        {
            started = true;
            if (worker.CurrentTask is MoveItemTask)
            {
                worker.ChangeState(new MultiTransportState());
            }
            else
            {
                worker.CurrentTask.Execute(worker, () =>
                {
                    worker.CurrentTask = null;
                    worker.ChangeState(new IdleState());
                });
            }
        }
    }

    public void ExitState(Worker worker)
    {
        
    }
}
