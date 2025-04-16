using UnityEngine;
using UnityEngine.AI;

public class Worker : MonoBehaviour
{
    private IWorkerState currentState;

    public NavMeshAgent Agent { get; private set; }
    public ITask CurrentTask { get; set; }

    private void Start()
    {
        Agent = GetComponent<NavMeshAgent>();
        if (Agent == null)
        {
            Debug.LogError("El trabajador debe tener un componente NavMeshAgent.");
        }

        ChangeState(new IdleState());
    }

    private void Update()
    {
        currentState?.UpdateState(this);
    }

    public void ChangeState(IWorkerState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState?.EnterState(this);
    }
}
