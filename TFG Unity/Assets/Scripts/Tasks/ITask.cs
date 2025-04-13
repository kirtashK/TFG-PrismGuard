using UnityEngine;

public interface ITask
{
    Vector3 TaskPosition { get; }
    int Priority { get; }
    void Execute(Worker worker, System.Action onComplete);
}
