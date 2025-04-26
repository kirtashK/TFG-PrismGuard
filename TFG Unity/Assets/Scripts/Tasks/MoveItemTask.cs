using UnityEngine;
using System.Collections;

public class MoveItemTask : MonoBehaviour, ITask
{
    [SerializeField]
    private int priority = 2;

    [SerializeField]
    private float interactionRange = 1f;

    public Vector3 TaskPosition
    {
        get { return transform.position; }
    }

    public int Priority
    {
        get { return priority; }
    }

    public float InteractionRange
    {
        get { return interactionRange; }
    }

    public Vector3 Destination { get; set; }

    // Callback genérico ejecutado cuando el ítem llega a su destino.
    public System.Action<GameObject> OnArrivalCallback { get; set; }

    public ItemData TaskData { get; set; }


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
        //Debug.Log(name + " - El worker " + worker.name + " comienza a mover el item a " + Destination);
    }
}
