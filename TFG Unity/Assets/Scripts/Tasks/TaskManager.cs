using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private List<ITask> availableTasks = new List<ITask>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterTask(ITask newTask)
    {
        if (!availableTasks.Contains(newTask))
        {
            availableTasks.Add(newTask);
        }
    }

    public void UnregisterTask(ITask task)
    {
        if (availableTasks.Contains(task))
        {
            availableTasks.Remove(task);
        }
    }

    public ITask RequestTask(Vector3 workerPosition)
    {
        if (availableTasks.Count == 0)
        {
            return null;
        }

        ITask bestTask = null;
        int highestPriority = int.MinValue;
        float closestDistance = float.MaxValue;

        foreach (ITask task in availableTasks)
        {
            int taskPriority = task.Priority;
            float taskDistance = GetPathLength(workerPosition, task.TaskPosition);

            //Debug.Log("Task distance: " + taskDistance);

            if (taskDistance < 0f)
            {
                continue;
            }

            if (taskPriority > highestPriority)
            {
                highestPriority = taskPriority;
                closestDistance = taskDistance;
                bestTask = task;
            }
            else if (taskPriority == highestPriority && taskDistance < closestDistance)
            {
                closestDistance = taskDistance;
                bestTask = task;
            }
        }

        if (bestTask != null)
        {
            availableTasks.Remove(bestTask);
        }

        return bestTask;
    }

    private float GetPathLength(Vector3 start, Vector3 end)
    {
        Debug.DrawRay(start, Vector3.up * 2, Color.green, 2f);
        Debug.DrawRay(end, Vector3.up * 2, Color.red, 2f);

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            if (path.status != NavMeshPathStatus.PathComplete)
            {
                return -1f;
            }

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.yellow, 2f);
                }
            }

            float length = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return length;
        }

        return -1f;
    }

    public void CompleteTask(ITask task)
    {
        if (availableTasks.Contains(task))
        {
            availableTasks.Remove(task);
        }
    }

    public MoveItemTask FindNearestMoveTask(Vector3 fromPosition, float maxCarryWeight)
    {
        MoveItemTask best = null;
        float bestDist = float.MaxValue;

        foreach (ITask task in availableTasks)
        {
            if (task is MoveItemTask mit && mit.TaskData.weight <= maxCarryWeight)
            {
                float dist = Vector3.Distance(fromPosition, mit.TaskPosition);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = mit;
                }
            }
        }

        if (best != null)
        {
            availableTasks.Remove(best);
        }

        return best;
    }
}
