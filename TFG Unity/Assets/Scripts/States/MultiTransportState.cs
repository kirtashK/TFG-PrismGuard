using UnityEngine;

public class MultiTransportState : IWorkerState
{
    private float deliveryRange;

    public void EnterState(Worker worker)
    {
        var firstTask = worker.CurrentTask as MoveItemTask;
        deliveryRange = firstTask.InteractionRange;
        worker.Agent.SetDestination(firstTask.TaskPosition);
    }

    public void UpdateState(Worker worker)
    {
        if (worker.CurrentTask == null)
        {
            worker.ChangeState(new IdleState());
            return;
        }

        // Si aún puedo recoger y estoy cerca del objetivo actual...
        var task = worker.CurrentTask as MoveItemTask;
        if (worker.Agent.remainingDistance <= deliveryRange && worker.CanCarry(task.TaskData))
        {
            // Recoger
            worker.PickUp(task.gameObject, task.TaskData);
            TaskManager.Instance.CompleteTask(task);

            // Intentar siguiente
            var next = TaskManager.Instance.FindNearestMoveTask(
                worker.transform.position,
                worker.maxCarryWeight - task.TaskData.weight
            );
            if (next != null)
            {
                worker.CurrentTask = next;
                worker.Agent.SetDestination(next.TaskPosition);
                return;
            }

            // Ninguno más: preparar entrega
            worker.Agent.SetDestination(task.Destination);
            return;
        }
    }

    public void ExitState(Worker worker)
    {

    }
}
