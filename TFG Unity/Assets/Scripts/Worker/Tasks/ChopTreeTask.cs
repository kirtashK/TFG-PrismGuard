using UnityEngine;
using System.Collections;
using Mono.Cecil;
using System.Collections.Generic;

public class ChopTreeTask : MonoBehaviour, ITask
{
    [SerializeField]
    private float workDuration = 3f;

    [SerializeField]
    private int priority = 1;

    [SerializeField]
    private float interactionRange = 1.5f;

    [Header("Troncos a generar")]
    [Tooltip("Minimo de troncos a generar")]
    [SerializeField]
    private int minLogs = 1;

    [Tooltip("Maximo de troncos a generar")]
    [SerializeField]
    private int maxLogs = 2;

    [Tooltip("Lista de posiciones donde pueden generarse troncos, dentro del rango de minLogs y maxLogs")]
    [SerializeField]
    private List<Transform> logSpawnPoints = new();

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

        int logsToSpawn = Random.Range(minLogs, maxLogs + 1);
        for (int i = 0; i < logsToSpawn; i++)
        {
            Vector3 spawnPos = logSpawnPoints[i].position;
            ItemManager.Instance.CreateTronco(spawnPos);
        }

        onComplete?.Invoke();

        ResourceManager.Instance.NotifyResourceCollected(
            gameObject.GetComponent<ResourceInstance>());

        enabled = false;
    }
}
