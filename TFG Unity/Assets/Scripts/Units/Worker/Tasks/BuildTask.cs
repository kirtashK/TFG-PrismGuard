using System;
using UnityEngine;

public class BuildTask : ITask
{
    private readonly Blueprint blueprint;

    public Vector3 TaskPosition => blueprint.TaskPosition;
    public Vector3 TaskLookAt => blueprint.TaskLookAt;
    public int Priority => blueprint.Priority;
    public float InteractionRange => blueprint.InteractionRange;
    public WorkType WorkType => WorkType.Build;

    public BuildTask(Blueprint blueprint)
    {
        this.blueprint = blueprint;
    }

    public void Execute(Worker worker, Action onComplete)
    {
        if (blueprint == null)
        {
            Debug.LogWarning($"Missing {nameof(blueprint)}");
            onComplete?.Invoke();
            return;
        }

        blueprint.HandleTaskExecute(worker, onComplete);
    }

    public void Cancel(Worker requester)
    {
        if (blueprint == null)
        {
            Debug.LogWarning($"Missing {nameof(blueprint)}");
        }

        blueprint.HandleTaskCancel(requester);
    }
}