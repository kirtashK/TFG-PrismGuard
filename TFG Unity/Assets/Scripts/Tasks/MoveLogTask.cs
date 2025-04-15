using UnityEngine;
using System.Collections;

public class MoveLogTask : MonoBehaviour, ITask
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

    public void Execute(Worker worker, System.Action onComplete)
    {
        Debug.Log(name + " - El worker " + worker.name + " comienza a mover el item a " + Destination);
        worker.StartCoroutine(MoveCoroutine(onComplete));
    }

    private IEnumerator MoveCoroutine(System.Action onComplete)
    {
        //TODO Modificar, que se mueva de verdad
        // Simula el tiempo de traslado, por ejemplo, 2 segundos
        yield return new WaitForSeconds(2f);
        Debug.Log(name + " - Tronco movido a aserradero.");
        // Notifica al aserradero que el tronco ha sido entregado
        SawmillProcessor.Instance.OnLogDelivered(gameObject);
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
