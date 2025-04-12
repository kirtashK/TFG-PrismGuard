using UnityEngine;

public class Task : MonoBehaviour
{
    public string taskName = "Default Task";
    public Vector3 taskPosition => transform.position;

    private void Start()
    {
        TaskManager.Instance.RegisterTask(this);
    }

    private void OnDestroy()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
        }
    }
}
