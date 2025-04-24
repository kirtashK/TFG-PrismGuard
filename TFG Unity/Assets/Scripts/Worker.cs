using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Worker : MonoBehaviour
{
    private IWorkerState currentState;

    public NavMeshAgent Agent { get; private set; }
    public ITask CurrentTask { get; set; }

    public float maxCarryWeight = 10f;
    private float currentLoad = 0f;
    private readonly List<GameObject> inventory = new List<GameObject>();

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

    // Inventario

    public bool CanCarry(ItemData data)
        => currentLoad + data.weight <= maxCarryWeight;

    public void PickUp(GameObject obj, ItemData data)
    {
        inventory.Add(obj);
        currentLoad += data.weight;
        obj.transform.SetParent(transform, worldPositionStays: true);
        obj.SetActive(false);
    }

    public void DropAll(Vector3 dropPosition)
    {
        foreach (var obj in inventory)
        {
            obj.transform.SetParent(null, worldPositionStays: true);
            obj.transform.position = dropPosition;
            obj.SetActive(true);
        }
        inventory.Clear();
        currentLoad = 0f;
    }
}
