using System.Collections.Generic;
using UnityEngine;

public class ItemConsumerManager : MonoBehaviour
{
    public static ItemConsumerManager Instance { get; private set; }

    private readonly List<IItemConsumer> consumers = new();
    public IReadOnlyList<IItemConsumer> Consumers => consumers;

    public event System.Action<IItemConsumer> OnConsumerUnregistered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Register(IItemConsumer consumer)
    {
        if (!consumers.Contains(consumer))
        {
            consumers.Add(consumer);
        }
    }

    public void Unregister(IItemConsumer consumer)
    {
        if (consumer == null)
        {
            return;
        }

        OnConsumerUnregistered?.Invoke(consumer);

        if (consumers.Contains(consumer))
        {
            //Debug.Log($"{name} removed {consumer} from its list");
            consumers.Remove(consumer);
        }
    }

    public IEnumerable<IItemConsumer> ActiveConsumers
    {
        get
        {
            foreach (IItemConsumer consumer in consumers)
            {
                if (consumer is MonoBehaviour monoBehaviour)
                {
                    if (monoBehaviour.isActiveAndEnabled)
                    {
                        yield return consumer;
                    }
                }
                else
                {
                    yield return consumer;
                }
            }
        }
    }
}