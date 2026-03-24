using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveItemTask : MonoBehaviour, ITask
{
    [SerializeField]
    private int priority = 2;

    [SerializeField]
    private float interactionRange = 1f;

    public Vector3 TaskPosition => transform.position;
    public Vector3 TaskLookAt => transform.position;

    public int Priority => priority;

    public float InteractionRange => interactionRange;

    public Vector3 Destination => target?.GetReceivePosition() ?? Vector3.zero;

    public ItemData TaskData { get; set; }

    private ItemInstance itemInstance;

    private IItemConsumer target;
    public IItemConsumer source = null;
    private bool isRegisteredToTaskManager;
    private bool isSubscribedToConsumerEvents;
    private const float pollInterval = 1f;
    public IItemConsumer TargetConsumer => target;

    private IItemConsumer lastConsumer;
    private bool isStored = false;

    public WorkType WorkType => WorkType.None;


    private void Awake()
    {
        itemInstance = GetComponent<ItemInstance>();

        if (itemInstance == null)
        {
            Debug.LogError($"{name}: null ItemInstance");
        }
    }

    private void OnEnable()
    {
        if (TaskData == null && itemInstance != null)
        {
            TaskData = itemInstance.itemData;
        }

        if (ItemConsumerManager.Instance != null && TaskManager.Instance != null)
        {
            SubscribeToConsumerEvents();
            StartCoroutine(PollForConsumer());
        }
        else
        {
            StartCoroutine(RegisterWhenReady());
        }
    }

    private void OnDisable()
    {
        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }

        UnsubscribeFromConsumerEvents();
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null || TaskManager.Instance == null)
        {
            yield return null;
        }

        SubscribeToConsumerEvents();

        StartCoroutine(PollForConsumer());
    }

    private void SubscribeToConsumerEvents()
    {
        if (isSubscribedToConsumerEvents)
        {
            return;
        }

        ItemConsumerManager.Instance.OnConsumerUnregistered += HandleCancelTask;
        isSubscribedToConsumerEvents = true;
    }

    private void UnsubscribeFromConsumerEvents()
    {
        if (!isSubscribedToConsumerEvents)
        {
            return;
        }

        if (ItemConsumerManager.Instance != null)
        {
            ItemConsumerManager.Instance.OnConsumerUnregistered -= HandleCancelTask;
        }
        isSubscribedToConsumerEvents = false;
    }

    private IEnumerator PollForConsumer()
    {
        WaitForSeconds wait = new(pollInterval);

        while (target == null)
        {
            IEnumerable<IItemConsumer> candidates = ItemConsumerManager.Instance.FindCandidatesFor(TaskData, transform.position, lastConsumer);

            foreach (IItemConsumer candidate in candidates)
            {
                if (candidate == null)
                {
                    continue;
                }
                // Skip warehouses if item is already in a warehouse
                if (isStored && candidate is Warehouse)
                {
                    continue;
                }
                if (candidate is MonoBehaviour monoBehaviour && !monoBehaviour.isActiveAndEnabled)
                {
                    continue;
                }
                if (!candidate.Reserve(TaskData))
                {
                    continue;
                }

                source = GetComponentInParent<IItemConsumer>();
                target = candidate;
                TaskManager.Instance.RegisterTask(this);
                isRegisteredToTaskManager = true;
                yield break;
            }

            yield return wait;
        }
    }

    private void HandleCancelTask(IItemConsumer consumer)
    {
        if (consumer != target)
        {
            return;
        }

        Debug.Log($"{name} target consumer {target} unregistered: {consumer}");

        consumer.Release(TaskData);

        lastConsumer = null;
        target = null;

        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }

        // Drop item if it was being transported to
        // a consumer that no longer exists
        if (itemInstance != null && itemInstance.carrier != null)
        {
            Worker carrier = itemInstance.carrier;

            carrier.DropItem(gameObject, itemInstance.carrier.unit.Position);
            Debug.Log($"{name} has been dropped by {carrier}. Resetting...");

            carrier.CurrentTask = null;
            carrier.ChangeState(new IdleState());
            carrier.unit.agent.SetDestination(carrier.transform.position);

            Reset();

            return;
        }
        // Item is not being carried yet (on the floor, warehouse etc)
        else
        {
            Debug.Log($"{name} is not being carried, resetting...");

            Worker[] workers = FindObjectsByType<Worker>(FindObjectsSortMode.None);
            foreach (Worker worker in workers)
            {
                if (worker == null)
                {
                    continue;
                }

                if (worker.CurrentTask is MoveItemTask moveItemTask && moveItemTask == this)
                {
                    worker.CurrentTask = null;
                    worker.ChangeState(new IdleState());
                    worker.unit.agent.SetDestination(worker.transform.position);

                    Reset();

                    return;
                }
            }
        }

        Reset();
    }

    public void Reset()
    {
        if (target != null)
        {
            lastConsumer = target;
        }

        target = null;
        source = null;

        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }

        StopAllCoroutines();
        StartCoroutine(PollForConsumer());
    }

    public void Execute(Worker worker, System.Action onComplete)
    {
        if (target == null || !target.CanReceive(TaskData))
        {
            Debug.Log($"[MoveItemTask] Execute: target invalid at delivery time for {name} - resetting task.");

            worker.DropItem(gameObject, worker.unit.Position);

            if (isRegisteredToTaskManager && TaskManager.Instance != null)
            {
                TaskManager.Instance.UnregisterTask(this);
                isRegisteredToTaskManager = false;
            }

            StartCoroutine(PollForConsumer());

            return;
        }

        onComplete?.Invoke();

        target.OnReceived(gameObject, TaskData);

        lastConsumer = target;
        source = null;
        itemInstance.carrier = null;

        if (isRegisteredToTaskManager)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }
    }

    public void Cancel(Worker requester)
    {
        HandleCancelTask(target);
    }

    /// <summary>
    /// Mark this task as stored in a Warehouse. 
    /// Stored tasks will not consider
    /// other warehouses when polling for consumers
    /// </summary>
    public void MarkStored()
    {
        isStored = true;

        if (target != null)
        {
            lastConsumer = target;
            target = null;
        }
        source = null;

        if (isRegisteredToTaskManager && TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }

        StopAllCoroutines();
        StartCoroutine(PollForConsumer());
    }

    public void ClearStored()
    {
        isStored = false;
    }
}