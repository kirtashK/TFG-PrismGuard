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
    private Vector3 currentDeliveryDestination;

    private struct CollectedItem
    {
        public MoveItemTask task;
        public IItemConsumer targetConsumer;
    }

    private readonly List<CollectedItem> collectedItems = new();

    private const float maxPickupRadius = 10f;
    private const float destinationToleranceSqr = 0.01f;

    public void EnterState(Worker worker)
    {
        this.worker = worker;

        task = worker.CurrentTask as MoveItemTask;

        if (task == null)
        {
            Debug.LogWarning($"{nameof(MultiTransportState)}: null {nameof(task)}");
            worker.ChangeState(new IdleState());
            return;
        }

        consumer = task.TargetConsumer;
        arrivalRange = task.InteractionRange;

        if (consumer == null)
        {
            Debug.LogWarning($"{nameof(MultiTransportState)}: null {nameof(consumer)}");
            task.Reset();
            worker.CurrentTask = null;
            worker.ChangeState(new IdleState());
            return;
        }

        currentDeliveryDestination = consumer.GetReceivePosition();

        phase = Phase.Pickup;
        worker.unit.agent.SetDestination(task.TaskPosition);
        collectedItems.Clear();
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
        collectedItems.Clear();
    }

    private void HandlePickupPhase()
    {
        // If we havent arrived yet, exit
        if (worker.unit.agent.pathPending
            || worker.unit.agent.remainingDistance > arrivalRange)
        {
            return;
        }
        if (task == null)
        {
            Debug.LogWarning($"{nameof(MultiTransportState)}: null {nameof(task)}");
            worker.CurrentTask = null;
            worker.ChangeState(new IdleState());
            return;
        }
        if (consumer == null)
        {
            Debug.LogWarning($"{nameof(MultiTransportState)}: null {nameof(consumer)}");
            TryResetCollectedAndDrop();
            return;
        }

        worker.PickUp(task.gameObject);
        TaskManager.Instance.UnregisterTask(task);

        if (task.gameObject.TryGetComponent<ItemInstance>(out ItemInstance instance))
        {
            task.source?.ConfirmRetrieval(task.gameObject);
        }

        collectedItems.Add(new CollectedItem
        {
            task = task,
            targetConsumer = task.TargetConsumer
        });

        MoveItemTask next = TaskManager.Instance.RequestMoveItemTask(
            worker.transform.position,
            worker.maxCarryWeight - worker.currentLoad,
            currentDeliveryDestination,
            maxPickupRadius
        );

        if (next != null)
        {
            IItemConsumer nextConsumer = next.TargetConsumer;

            if (nextConsumer != null
                && AreSameDestination(nextConsumer.GetReceivePosition(), currentDeliveryDestination))
            {
                task = next;
                consumer = nextConsumer;
                arrivalRange = next.InteractionRange;
                worker.CurrentTask = next;
                worker.unit.agent.SetDestination(next.TaskPosition);
                return;
            }
        }

        phase = Phase.Delivery;
        worker.unit.agent.SetDestination(consumer.GetReceivePosition());
    }

    private void HandleDeliveryPhase()
    {
        // If we havent arrived yet, exit
        if (worker.unit.agent.pathPending
            || worker.unit.agent.remainingDistance > arrivalRange)
        {
            return;
        }
        if (consumer == null)
        {
            Debug.LogWarning($"{nameof(MultiTransportState)}: null {nameof(consumer)}");
            TryResetCollectedAndDrop();
            return;
        }

        foreach (CollectedItem collectedItem in collectedItems)
        {
            if (collectedItem.task == null || collectedItem.targetConsumer == null)
            {
                continue;
            }

            collectedItem.targetConsumer.OnReceived(collectedItem.task.gameObject, collectedItem.task.TaskData);
            worker.currentLoad = Mathf.Max(0f, worker.currentLoad - collectedItem.task.TaskData.weight);
            worker.ClearFromInventory(collectedItem.task.gameObject);

            if (collectedItem.task.gameObject.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
            {
                itemInstance.carrier = null;
            }
        }

        collectedItems.Clear();
        worker.CurrentTask = null;
        worker.ChangeState(new IdleState());
    }

    private void TryResetCollectedAndDrop()
    {
        foreach (CollectedItem collected in collectedItems)
        {
            if (collected.task == null)
            {
                continue;
            }

            collected.task.Reset();
        }

        worker.DropAll(worker.unit.Position);

        collectedItems.Clear();
        worker.CurrentTask = null;
        worker.ChangeState(new IdleState());
    }

    private bool AreSameDestination(Vector3 first, Vector3 second)
    {
        return (first - second).sqrMagnitude <= destinationToleranceSqr;
    }
}