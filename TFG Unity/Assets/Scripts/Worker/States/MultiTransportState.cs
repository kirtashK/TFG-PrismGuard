using System.Collections.Generic;
using UnityEngine;

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

        if (task == null)
        {
            Debug.LogWarning("[MultiTransportState] EnterState: null task");
            worker.ChangeState(new IdleState());
            return;
        }

        consumer = task.TargetConsumer;
        arrivalRange = task.InteractionRange;

        if (consumer == null)
        {
            Debug.LogWarning("[MultiTransportState] EnterState: null consumer");
            task.Reset();
            worker.currentTask = null;
            worker.ChangeState(new IdleState());
            return;
        }

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

        if (task == null)
        {
            Debug.LogWarning("[MultiTransportState] HandlePickupPhase: null task");
            worker.currentTask = null;
            worker.ChangeState(new IdleState());
            return;
        }

        if (consumer == null)
        {
            Debug.LogWarning("[MultiTransportState] HandlePickupPhase: null consumer");
            TryResetCollectedAndDrop();
            return;
        }

        worker.PickUp(task.gameObject);
        TaskManager.Instance.CompleteTask(task);

        // If there is a source (such as warehouse),
        // confirm retrieval (pickup) of the item
        task.source?.ConfirmRetrieval();

        collectedTasks.Add(task);

        MoveItemTask next = TaskManager.Instance.RequestMoveItemTask(
            worker.transform.position,
            worker.workerData.maxCarryWeight - worker.currentLoad,
            consumer.GetReceivePosition(),
            maxPickupRadius
        );

        if (consumer == null)
        {
            Debug.LogWarning("[MultiTransportState] HandlePickupPhase: null consumer after pickup");
            TryResetCollectedAndDrop();
            return;
        }

        if (next != null)
        {
            task = next;
            arrivalRange = next.InteractionRange;
            worker.currentTask = next;
            worker.agent.SetDestination(next.TaskPosition);
            return;
        }

        if (consumer == null || (consumer is MonoBehaviour monoBehaviour && !monoBehaviour.isActiveAndEnabled))
        {
            Debug.LogWarning("[MultiTransportState] HandlePickupPhase: null consumer or is deactivated");
            TryResetCollectedAndDrop();
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

        if (consumer == null)
        {
            Debug.LogWarning("[MultiTransportState] HandlePickupPhase: null consumer");
            TryResetCollectedAndDrop();
            return;
        }

        foreach (MoveItemTask task in collectedTasks)
        {
            if (task == null)
            {
                continue;
            }

            if (!consumer.CanReceive(task.TaskData))
            {
                Debug.LogWarning("[MultiTransportState] HandlePickupPhase: consumer cant receive");
                TryResetCollectedAndDrop();
                return;
            }

            consumer.OnReceived(task.gameObject, task.TaskData);
            worker.currentLoad = Mathf.Max(0f, worker.currentLoad - task.TaskData.weight);
        }

        collectedTasks.Clear();
        worker.currentTask = null;
        worker.ChangeState(new IdleState());
    }

    private void TryResetCollectedAndDrop()
    {
        foreach (MoveItemTask collected in collectedTasks)
        {
            if (collected == null)
            {
                continue;
            }

            try
            {
                collected.Reset();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MultiTransportState] Error resetting MoveItemTask {collected.name}: {ex.Message}");
            }
        }

        try
        {
            worker.DropAll(worker.Position);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MultiTransportState] Error al forzar DropAll en worker {worker.name}: {ex.Message}");
        }

        collectedTasks.Clear();
        worker.currentTask = null;
        worker.ChangeState(new IdleState());
    }
}