using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

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
    private bool isRegistered;
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
        StartCoroutine(RegisterWhenReady());
    }

    private void OnDisable()
    {
        if (isRegistered)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegistered = false;
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ItemConsumerManager.Instance == null)
        {
            yield return null;
        }
        StartCoroutine(PollForConsumer());
    }

    private IEnumerator PollForConsumer()
    {
        WaitForSeconds wait = new(pollInterval);
        while (target == null)
        {
            foreach (IItemConsumer consumer in ItemConsumerManager.Instance.Consumers)
            {
                if (consumer == lastConsumer)
                {
                    continue;
                }
                if (consumer is not Warehouse
                    && consumer.CanReceive(TaskData) 
                    && consumer.Reserve(TaskData))
                {
                    target = consumer;
                    TaskManager.Instance.RegisterTask(this);
                    isRegistered = true;
                    yield break;
                }
            }

            if (lastConsumer is not Warehouse)
            {
                foreach(IItemConsumer consumer in ItemConsumerManager.Instance.Consumers)
            {
                    if (consumer == lastConsumer)
                    {
                        continue;
                    }
                    if (consumer is Warehouse
                        && consumer.CanReceive(TaskData)
                        && consumer.Reserve(TaskData))
                    {
                        target = consumer;
                        TaskManager.Instance.RegisterTask(this);
                        isRegistered = true;
                        yield break;
                    }
                }
            }

            yield return wait;
        }
    }

    public void Reset()
    {
        lastConsumer = target;
        target = null;

        if (isRegistered)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegistered = false;
        }

        StartCoroutine(PollForConsumer());
    }

    public void Execute(Worker worker, System.Action onComplete)
    {
        onComplete?.Invoke();

        target.OnReceived(gameObject, TaskData);

        lastConsumer = target;

        if (isRegistered)
        {
            TaskManager.Instance.UnregisterTask(this);
            isRegistered = false;
        }
    }
}