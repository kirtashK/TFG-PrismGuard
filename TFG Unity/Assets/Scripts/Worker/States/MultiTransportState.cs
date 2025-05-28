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

        MoveItemTask firstTask = worker.currentTask as MoveItemTask;
        pickupTarget = firstTask.TaskPosition;
        deliveryTarget = firstTask.Destination;
        arrivalRange = firstTask.InteractionRange;

        phase = Phase.Pickup;
        worker.agent.SetDestination(pickupTarget);
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
        MoveItemTask task = workerRef.currentTask as MoveItemTask;

        if (task == null)
        {
            workerRef.ChangeState(new IdleState());
            return;
        }

        if (!workerRef.agent.pathPending 
            && workerRef.agent.remainingDistance <= arrivalRange)
        {
            if (!workerRef.CanCarry(task.TaskData))
            {
                phase = Phase.Delivery;
                workerRef.agent.SetDestination(deliveryTarget);
                return;
            }

            workerRef.PickUp(task.gameObject, task.TaskData);
            TaskManager.Instance.CompleteTask(task);
            collectedTasks.Add(task);

            float remainingCapacity = workerRef.workerData.maxCarryWeight - workerRef.currentLoad;

            MoveItemTask next = TaskManager.Instance.RequestMoveItemTask(
                workerRef.transform.position,
                remainingCapacity,
                deliveryTarget,
                maxPickupRadius
            );

            if (next != null)
            {
                workerRef.currentTask = next;
                pickupTarget = next.TaskPosition;
                workerRef.agent.SetDestination(pickupTarget);
                return;
            }

            phase = Phase.Delivery;
            workerRef.agent.SetDestination(deliveryTarget);
        }
    }

    // ##########################
    // #    Fase de Entrega     #
    // ##########################

    private void HandleDeliveryPhase()
    {
        Worker worker = workerRef;

        if (!worker.agent.pathPending && worker.agent.remainingDistance <= arrivalRange)
        {
            foreach (MoveItemTask task in collectedTasks)
            {
                task.OnArrivalCallback?.Invoke(task.gameObject);
                worker.currentLoad -= task.TaskData.weight;
            }

            worker.currentTask = null;
            worker.ChangeState(new IdleState());
        }
    }
}
