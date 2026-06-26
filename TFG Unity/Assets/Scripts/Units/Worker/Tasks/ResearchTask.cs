using System;
using UnityEngine;

public class ResearchTask : ITask
{
    public Vector3 TaskPosition => bench != null ? bench.TaskPosition : Vector3.zero;
    public Vector3 TaskLookAt => bench != null ? bench.TaskLookAt : Vector3.zero;
    public int Priority => bench != null ? bench.priority : 1;
    public float InteractionRange => bench != null ? bench.interactionRange : 1f;

    private readonly ResearchBench bench;
    public WorkType WorkType => WorkType.Research;


    public ResearchTask(ResearchBench bench)
    {
        this.bench = bench;
    }

    public void Execute(Worker worker, Action onComplete)
    {
        if (bench == null)
        {
            Debug.LogWarning($"ResearchTask: null {nameof(bench)}");
            onComplete?.Invoke();
            return;
        }

        bench.HandleTaskExecute(worker, onComplete);
    }

    public void Cancel(Worker requester)
    {
        if (bench != null)
        {
            bench.HandleTaskCancel(requester);
        }
    }
}