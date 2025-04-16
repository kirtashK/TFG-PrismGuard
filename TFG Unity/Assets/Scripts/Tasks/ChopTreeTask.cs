using UnityEngine;
using System.Collections;

public class ChopTreeTask : MonoBehaviour, ITask
{
    [SerializeField]
    private float workDuration = 3f;

    [SerializeField]
    private int priority = 1;

    [SerializeField]
    private float interactionRange = 1.5f;

    public Vector3 TaskPosition
    {
        get
        {
            return transform.position;
        }
    }

    public int Priority
    {
        get
        {
            return priority;
        }
    }

    public float InteractionRange
    {
        get
        {
            return interactionRange;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (TaskManager.Instance == null)
        {
            yield return null;
        }
        TaskManager.Instance.RegisterTask(this);
    }

    private void OnDisable()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.UnregisterTask(this);
        }
    }

    public void Execute(Worker worker, System.Action onComplete)
    {
        Debug.Log(name + " - Iniciando tarea de talar árbol para " + worker.name);
        StartCoroutine(WorkCoroutine(onComplete));
    }

    private IEnumerator WorkCoroutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(workDuration);
        Debug.Log(name + " - Árbol talado.");

        ItemManager.Instance.CreateTronco(transform.position);

        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
