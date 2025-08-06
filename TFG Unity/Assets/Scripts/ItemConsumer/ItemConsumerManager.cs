using System.Collections.Generic;
using UnityEngine;

public class ItemConsumerManager : MonoBehaviour
{
    public static ItemConsumerManager Instance { get; private set; }

    private readonly List<IItemConsumer> consumers = new();
    public IReadOnlyList<IItemConsumer> Consumers => consumers;

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
        consumers.Remove(consumer);
    }
}