using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Progress;

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
        //Debug.Log($"New state: {newState}");
        currentState?.ExitState(this);
        currentState = newState;
        currentState?.EnterState(this);
    }

    public Vector3 Position => transform.position;

    public bool IsAlive => currentHealth > 0f;

    /// <summary>
    /// Damages worker, if its still alive, it runs away
    /// </summary>
    /// <param name="amount">Damage to receive</param>
    /// <param name="attackerOrigin">Attacker's position</param>
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
        Debug.Log($"{name} has died");
        // TODO Animacion muerte, sonido
        Destroy(gameObject);
    }

    /// <summary>
    /// Worker runs away from threatPosition
    /// </summary>
    /// <param name="threatPosition">Threat's position</param>
    /// <param name="retreatDistance">Distance to retreat</param>
    public void Retreat(Vector3 threatPosition, float retreatDistance = 5f)
    {
        Vector3 fleeDir = (transform.position - threatPosition).normalized;

        Vector3 rawTarget = transform.position + fleeDir * retreatDistance;

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

    // #############
    // # Inventory #
    // #############

    public bool CanCarry(ItemData itemData)
        => currentLoad + itemData.weight <= workerData.maxCarryWeight;

    public void PickUp(GameObject item)
    {
        inventory.Add(item);

        if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.carrier = this;
            currentLoad += itemInstance.itemData.weight;
            itemInstance.SetVisible(false);
        }

        item.transform.SetParent(inventorySpot, worldPositionStays: true);
    }

    public void ClearFromInventory(GameObject item)
    {
        inventory.Remove(item);
    }

    public void DropAll(Vector3 dropPosition)
    {
        foreach (GameObject carriedItem in inventory)
        {
            if (carriedItem == null)
            {
                continue;
            }

            if (carriedItem.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
            {
                itemInstance.carrier = null;
                itemInstance.SetVisible(true);
            }

            carriedItem.transform.SetParent(null, worldPositionStays: true);
            carriedItem.transform.position = dropPosition;
        }
        inventory.Clear();
        currentLoad = 0f;
    }

    public void DropItem(GameObject carriedItem, Vector3 dropPosition)
    {
        if (carriedItem == null)
        {
            return;
        }

        if (carriedItem.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.carrier = null;
            currentLoad = Mathf.Max(0f, currentLoad - itemInstance.itemData.weight);
            itemInstance.SetVisible(true);
        }

        inventory.Remove(carriedItem);

        carriedItem.transform.SetParent(null, worldPositionStays: true);
        carriedItem.transform.position = dropPosition;
    }
}