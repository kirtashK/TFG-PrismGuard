using System.Collections.Generic;
using UnityEngine;

public class MultiTransportState : IWorkerState
{
    private enum Phase { Pickup, Delivery }

    private Phase phase;
    private Vector3 pickupTarget;
    private Vector3 deliveryTarget;
    private float arrivalRange;

    private Worker workerRef;

    private readonly List<MoveItemTask> collectedTasks = new();

    private const float maxPickupRadius = 10f;

    public void EnterState(Worker worker)
    {
        workerRef = worker;

        MoveItemTask firstTask = worker.CurrentTask as MoveItemTask;
        pickupTarget = firstTask.TaskPosition;
        deliveryTarget = firstTask.Destination;
        arrivalRange = firstTask.InteractionRange;

        phase = Phase.Pickup;
        worker.Agent.SetDestination(pickupTarget);
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

    // ##########################
    // #    Fase de Recogida    #
    // ##########################

    private void HandlePickupPhase()
    {
        MoveItemTask task = workerRef.CurrentTask as MoveItemTask;

        if (task == null)
        {
            workerRef.ChangeState(new IdleState());
            return;
        }

        if (!workerRef.Agent.pathPending 
            && workerRef.Agent.remainingDistance <= arrivalRange)
        {
            if (!workerRef.CanCarry(task.TaskData))
            {
                phase = Phase.Delivery;
                workerRef.Agent.SetDestination(deliveryTarget);
                return;
            }

            workerRef.PickUp(task.gameObject, task.TaskData);
            TaskManager.Instance.CompleteTask(task);
            collectedTasks.Add(task);

            float remainingCapacity = workerRef.maxCarryWeight - workerRef.currentLoad;

            MoveItemTask next = TaskManager.Instance.RequestMoveItemTask(
                workerRef.transform.position,
                remainingCapacity,
                deliveryTarget,
                maxPickupRadius
            );

            if (next != null)
            {
                workerRef.CurrentTask = next;
                pickupTarget = next.TaskPosition;
                workerRef.Agent.SetDestination(pickupTarget);
                return;
            }

            phase = Phase.Delivery;
            workerRef.Agent.SetDestination(deliveryTarget);
        }
    }

    // ##########################
    // #    Fase de Entrega     #
    // ##########################

    private void HandleDeliveryPhase()
    {
        Worker worker = workerRef;

        if (!worker.Agent.pathPending && worker.Agent.remainingDistance <= arrivalRange)
        {
            foreach (MoveItemTask task in collectedTasks)
            {
                task.OnArrivalCallback?.Invoke(task.gameObject);
                worker.currentLoad -= task.TaskData.weight;
            }

            worker.CurrentTask = null;
            worker.ChangeState(new IdleState());
        }
    }
}
