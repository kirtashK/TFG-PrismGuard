using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;

public class Worker : MonoBehaviour, ICombatTarget
{
    private IWorkerState currentState;

    public NavMeshAgent agent { get; private set; }
    public ITask currentTask { get; set; }

    public WorkerData workerData;

    [SerializeField]
    [Tooltip("Capacidad actual")]
    public float currentLoad = 0f;

    [SerializeField]
    private Transform inventorySpot;

    private readonly List<GameObject> inventory = new();

    private float currentHealth;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("El trabajador debe tener un componente NavMeshAgent.");
        }

        currentHealth = workerData.maxHealth;

        agent.speed = workerData.moveSpeed;

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

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    /// <summary>
    /// Daña al worker, si sobrevive huye del peligro
    /// </summary>
    /// <param name="amount">Daño a recibir</param>
    /// <param name="attackerOrigin">Posicion del atacante</param>
    public void TakeDamage(float amount, Vector3 attackOrigin)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"Salud de {name} = {currentHealth}/{workerData.maxHealth}");

        DropAll(inventorySpot.position);

        if (currentHealth > 0f)
        {
            Retreat(attackOrigin);
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{name} ha muerto");
        // TODO Animacion muerte, sonido
        Destroy(gameObject);
    }

    /// <summary>
    /// El worker huye en direccion opuesta a threatPosition
    /// </summary>
    /// <param name="threatPosition">Posicion del peligro</param>
    /// <param name="retreatDistance">Distancia a huir</param>
    public void Retreat(Vector3 threatPosition, float retreatDistance = 5f)
    {
        Vector3 fleeDir = (transform.position - threatPosition).normalized;

        Vector3 rawTarget = transform.position + fleeDir * retreatDistance;

        // Comprobar si la posición de huida esta en un navmesh
        if (NavMesh.SamplePosition(rawTarget,
                                   out NavMeshHit hit,
                                   retreatDistance,
                                   NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            //agent.SetDestination(rawTarget);
        }
    }

    // ##############
    // # Inventario #
    // ##############

    public bool CanCarry(ItemData itemData)
        => currentLoad + itemData.weight <= workerData.maxCarryWeight;

    public void PickUp(GameObject gameObject, ItemData itemData)
    {
        inventory.Add(gameObject);
        currentLoad += itemData.weight;
        gameObject.transform.SetParent(inventorySpot, worldPositionStays: true);
        gameObject.SetActive(false);
    }

    public void DropAll(Vector3 dropPosition)
    {
        foreach (GameObject gameObject in inventory)
        {
            if (gameObject != null)
            {
                gameObject.transform.SetParent(null, worldPositionStays: true);
                gameObject.transform.position = dropPosition;
                gameObject.SetActive(true);
            }
        }
        inventory.Clear();
        currentLoad = 0f;
    }
}
