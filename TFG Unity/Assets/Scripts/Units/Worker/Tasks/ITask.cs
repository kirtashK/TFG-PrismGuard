using UnityEngine;

public enum WorkType
{
    None = 0,
    Chop = 1,
    Mine = 2,
    Build = 3,
    Research = 4,
}

public interface ITask
{
    Vector3 TaskPosition { get; }
    Vector3 TaskLookAt { get; }
    int Priority { get; }
    float InteractionRange { get; }
    void Execute(Worker worker, System.Action onComplete);
    void Cancel(Worker requester);

    WorkType WorkType { get; }
}