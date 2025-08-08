using System.Collections.Generic;

public class MultiTransportState : IWorkerState
{
    private enum Phase { Pickup, Delivery }
    private Phase phase;

    private float arrivalRange;

    private Worker worker;

    private MoveItemTask task;
    private IItemConsumer consumer;

    private readonly List<MoveItemTask> collectedTasks = new();

    private const float maxPickupRadius = 10f;

    public void EnterState(Worker worker)
    {
        this.worker = worker;

        task = worker.currentTask as MoveItemTask;
        consumer = task.TargetConsumer;
        arrivalRange = task.InteractionRange;

        phase = Phase.Pickup;
        worker.agent.SetDestination(task.TaskPosition);
        collectedTasks.Clear();
    }

    public void UpdateState(Worker worker)
    {
        switch (phase)
        {
            case Phase.Pickup:
                HandlePickupPhase();
                break;

            case Phase.Delivery:
                HandleDeliveryPhase();
                break;
        }
    }

    public void ExitState(Worker worker)
    {
        collectedTasks.Clear();
    }

    private void HandlePickupPhase()
    {
        if (worker.agent.pathPending
            || worker.agent.remainingDistance > arrivalRange)
        {
            return;
        }

        worker.PickUp(task.gameObject, task.TaskData);
        TaskManager.Instance.CompleteTask(task);
        collectedTasks.Add(task);

        MoveItemTask next = TaskManager.Instance.RequestMoveItemTask(
            worker.transform.position,
            worker.workerData.maxCarryWeight - worker.currentLoad,
            consumer.GetReceivePosition(),
            maxPickupRadius
        );

        if (next != null)
        {
            task = next;
            arrivalRange = next.InteractionRange;
            worker.currentTask = next;
            worker.agent.SetDestination(next.TaskPosition);
            return;
        }

        phase = Phase.Delivery;
        worker.agent.SetDestination(consumer.GetReceivePosition());
    }


    private void HandleDeliveryPhase()
    {
        if (worker.agent.pathPending
            || worker.agent.remainingDistance > arrivalRange)
        {
            return;
        }

        foreach (MoveItemTask task in collectedTasks)
        {
            consumer.OnReceived(task.gameObject, task.TaskData);
            worker.currentLoad -= task.TaskData.weight;
        }

        collectedTasks.Clear();
        worker.currentTask = null;
        worker.ChangeState(new IdleState());
    }
}