using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private List<Task> availableTasks = new List<Task>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterTask(Task task)
    {
        if (!availableTasks.Contains(task))
        {
            availableTasks.Add(task);
        }
    }

    public void UnregisterTask(Task task)
    {
        if (availableTasks.Contains(task))
        {
            availableTasks.Remove(task);
        }
    }

    public Task RequestTask()
    {
        if (availableTasks.Count == 0)
        {
            return null;
        }

        Task task = availableTasks[0];
        availableTasks.RemoveAt(0);
        return task;
    }
}
