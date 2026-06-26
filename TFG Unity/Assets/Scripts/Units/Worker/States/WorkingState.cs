
public class WorkingState : IWorkerState
{
    private bool started = false;
    private WorkType currentWorkType = WorkType.None;

    public void EnterState(Worker worker)
    {
        started = false;
        currentWorkType = worker.CurrentTask.WorkType;
        worker.SetWorkAnimation(currentWorkType);
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
        worker.ClearWorkAnimation();
    }
}
