using UnityEngine;
using UnityEngine.AI;

public class Worker : MonoBehaviour
{
    public enum WorkerState
    {
        Idle,
        Moving,
        Working
    }

    private WorkerState currentState = WorkerState.Idle;

    private NavMeshAgent agent;

    private Task currentTask;

    private void Start()
    {
        currentState = WorkerState.Idle;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("El trabajador debe tener un componente NavMeshAgent.");
        }
        Debug.Log(gameObject.name + " - Estado: " + currentState);
    }

    private void Update()
    {
        switch (currentState)
        {
            case WorkerState.Idle:
                // Aquí se buscará una tarea pendiente
                SearchForTask();
                break;

            case WorkerState.Moving:
                // Podrías verificar cuando llegue al destino y cambiar el estado a Working
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    currentState = WorkerState.Working;
                    Debug.Log(gameObject.name + " - Ha llegado al destino, comienza a trabajar.");
                }
                break;

            case WorkerState.Working:
                // Ejecuta la tarea. Puedes implementar una duración o verificación para finalizarla.
                DoWork();
                break;
        }
    }

    private void SearchForTask()
    {
        if (currentTask != null)
        {
            return;
        }

        currentTask = TaskManager.Instance.RequestTask();

        if (currentTask != null)
        {
            MoveTo(currentTask.taskPosition);
        }
        else
        {
            Debug.Log(name + " - No hay tareas disponibles.");
            Invoke("SearchForTask", 2f); // Reintenta más tarde
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
            currentState = WorkerState.Moving;
            Debug.Log(gameObject.name + " - Moviéndose hacia: " + destination);
        }
    }

    private void DoWork()
    {
        if (currentTask is ChopTreeTask chopTask)
        {
            currentState = WorkerState.Working;
            chopTask.Execute(() =>
            {
                currentTask = null;
                currentState = WorkerState.Idle;
            });
        }
        else
        {
            Debug.LogWarning(name + " - Tarea inválida o no reconocida.");
            currentTask = null;
            currentState = WorkerState.Idle;
        }
    }

        private void FinishWork()
        {
            currentState = WorkerState.Idle;
            Debug.Log(gameObject.name + " - Tarea completada.");
        }
    }
