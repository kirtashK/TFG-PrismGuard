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
            worker.CurrentTask.Execute(worker, () =>
            {
                worker.CurrentTask = null;
                worker.ChangeState(new IdleState());
                //Debug.Log(worker.name + " - Tarea completada, volviendo a Idle.");
            });
        }
        else if (worker.CurrentTask is MoveItemTask)
        {
            worker.ChangeState(new TransportItemState());
        }
    }

    public void ExitState(Worker worker)
    {
        
    }
}
