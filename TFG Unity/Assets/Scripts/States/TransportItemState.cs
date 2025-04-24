using UnityEditor.Rendering;
using UnityEngine;

public class TransportItemState : IWorkerState
{
    private bool itemPickedUp = false;

    public void EnterState(Worker worker)
    {
        //Debug.Log(worker.name + " - Iniciando transporte de ítem.");
        if (worker.CurrentTask != null)
        {
            worker.Agent.SetDestination(worker.CurrentTask.TaskPosition);
        }
    }

    public void UpdateState(Worker worker)
    {
        if (worker.CurrentTask == null)
        {
            worker.ChangeState(new IdleState());
            return;
        }

        if (!itemPickedUp)
        {
            if (!worker.Agent.pathPending && worker.Agent.remainingDistance <= worker.CurrentTask.InteractionRange)
            {
                // "Recoge" el ítem: se desactiva para simular que el worker lo ha cogido
                GameObject itemObject = ((MoveItemTask)worker.CurrentTask).gameObject;
                itemObject.SetActive(false);
                itemPickedUp = true;

                itemObject.transform.SetParent(worker.transform, worldPositionStays: true);
                itemObject.transform.position = worker.transform.position;

                // TODO, meter en el inventario del worker si hay espacio

                //Debug.Log(worker.name + " - Ítem recogido, iniciando transporte al destino.");

                worker.Agent.SetDestination(((MoveItemTask)worker.CurrentTask).Destination);
            }
        }
        else
        {
            if (!worker.Agent.pathPending && worker.Agent.remainingDistance <= worker.CurrentTask.InteractionRange)
            {
                //Debug.Log(worker.name + " - Ítem entregado en destino.");
                ((MoveItemTask)worker.CurrentTask).OnArrivalCallback?.
                    Invoke(((MoveItemTask)worker.CurrentTask).gameObject);

                worker.CurrentTask = null;
                worker.ChangeState(new IdleState());
            }
        }
    }

    public void ExitState(Worker worker)
    {
        itemPickedUp = false;
    }
}
