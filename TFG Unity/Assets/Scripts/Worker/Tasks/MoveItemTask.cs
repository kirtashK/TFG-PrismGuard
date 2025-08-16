using System.Collections;
using UnityEngine;

public class MoveItemTask : MonoBehaviour, ITask
{
    [SerializeField]
    private int priority = 2;

    [SerializeField]
    private float interactionRange = 1f;

    public Vector3 TaskPosition => transform.position;

    public int Priority => priority;

    public float InteractionRange => interactionRange;

    public Vector3 Destination => target?.GetReceivePosition() ?? Vector3.zero;

    public ItemData TaskData { get; set; }

    ItemInstance instance;

    private IItemConsumer target;
    // Some items will come from intermediaries such as warehouses
    // for those items, once picked up we reduce capacity of source
    public IItemConsumer source = null;
    private bool isRegisteredToTaskManager;
    private bool isSubscribedToConsumerEvents;
    private const float pollInterval = 1f;
    public IItemConsumer TargetConsumer => target;

    private IItemConsumer lastConsumer;

    private void Awake()
    {
        instance = GetComponent<ItemInstance>();

        if (instance == null)
        {
            Debug.LogError(name + ": no ItemInstance in this GameObject");
        }
    }

    private void OnEnable()
    {
        if (TaskData == null && instance != null)
        {
            TaskData = instance.itemData;
        }

        if (ItemConsumerManager.Instance != null)
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
        while (ItemConsumerManager.Instance == null)
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

        ItemConsumerManager.Instance.OnConsumerUnregistered += HandleConsumerUnregistered;
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
            ItemConsumerManager.Instance.OnConsumerUnregistered -= HandleConsumerUnregistered;
        }
        isSubscribedToConsumerEvents = false;
    }

    private IEnumerator PollForConsumer()
    {
        WaitForSeconds wait = new(pollInterval);

        while (target == null)
        {
            foreach (IItemConsumer consumer in ItemConsumerManager.Instance.ActiveConsumers)
            {
                if (consumer == lastConsumer)
                {
                    continue;
                }
                if (consumer is MonoBehaviour monoBehaviour && !monoBehaviour.isActiveAndEnabled)
                {
                    continue;
                }
                if (consumer is not Warehouse
                    && consumer.CanReceive(TaskData)
                    && consumer.Reserve(TaskData))
                {
                    source = GetComponentInParent<IItemConsumer>();
                    target = consumer;
                    TaskManager.Instance.RegisterTask(this);
                    isRegisteredToTaskManager = true;
                    yield break;
                }
            }

            if (lastConsumer is not Warehouse)
            {
                foreach (IItemConsumer consumer in ItemConsumerManager.Instance.ActiveConsumers)
                {
                    if (consumer == lastConsumer)
                    {
                        continue;
                    }
                    if (consumer is MonoBehaviour monoBehaviour && !monoBehaviour.isActiveAndEnabled)
                    {
                        continue;
                    }
                    if (consumer is Warehouse
                        && consumer.CanReceive(TaskData)
                        && consumer.Reserve(TaskData))
                    {
                        source = GetComponentInParent<IItemConsumer>();
                        target = consumer;
                        TaskManager.Instance.RegisterTask(this);
                        isRegisteredToTaskManager = true;
                        yield break;
                    }
                }
            }

            yield return wait;
        }
    }

    private void HandleConsumerUnregistered(IItemConsumer consumer)
    {
        if (consumer != target)
        {
            //Debug.Log($"{name} NOT my target consumer {target} unregistered: {consumer}");
            return;
        }

        Debug.Log($"{name} target consumer {target} unregistered: {consumer}");

        // Defensive: tries to release, if there is no release its fine
        try
        {
            Debug.Log(name + " releasing data " + TaskData);
            consumer.Release(TaskData);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Release threw in HandleConsumerUnregistered: {ex.Message}");
        }

        lastConsumer = target;
        target = null;

        if (isRegisteredToTaskManager)
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.UnregisterTask(this);
            }
            isRegisteredToTaskManager = false;
        }

        // Drop item if it was being transported to a consumer that no longer exists
        if (instance != null && instance.carrier != null)
        {
            Worker carrier = instance.carrier;

            Debug.Log(name + " dropping carried item");
            carrier.DropItem(gameObject, instance.carrier.Position);
            Debug.Log($"{name} has been dropped by {carrier}");

            carrier.currentTask = null;
            carrier.ChangeState(new IdleState());
            carrier.agent.SetDestination(carrier.transform.position);

            Reset();

            return;
        }
        // Item is not being carried yet (on the floor, warehouse etc)
        else
        {
            Debug.Log($"{name} is not being carried");

            Worker[] workers = FindObjectsByType<Worker>(FindObjectsSortMode.None);
            foreach (Worker worker in workers)
            {
                if (worker == null)
                {
                    continue;
                }

                if (worker.currentTask is MoveItemTask moveItemTask && moveItemTask == this)
                {
                    Debug.Log($"{name} is being tracked by {worker}, resetting");
                    worker.DropItem(gameObject, worker.Position);
                    Debug.Log($"{name} has been dropped by {worker}");

                    worker.currentTask = null;
                    worker.ChangeState(new IdleState());
                    worker.agent.SetDestination(worker.transform.position);

                    Reset();
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

            worker.DropItem(gameObject, worker.Position);

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

        if (isRegisteredToTaskManager)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegisteredToTaskManager = false;
        }
    }
}