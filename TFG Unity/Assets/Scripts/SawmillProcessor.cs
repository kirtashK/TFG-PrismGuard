using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SawmillProcessor : MonoBehaviour
{
    public static SawmillProcessor Instance { get; private set; }

    [Header("Detección de troncos")]
    [Tooltip("Radio en el que se detectan los troncos para ser trasladados al aserradero")]
    public float detectionRadius = 10f;

    [Tooltip("Capa donde se encuentran los troncos")]
    public LayerMask logLayer;

    [Header("Procesamiento")]
    [Tooltip("Máximo de tablones que puede producir el aserradero")]
    public int maxPlanks = 100;

    [Tooltip("Tiempo en segundos para procesar cada tronco")]
    public float processingTime = 10f;

    [Tooltip("Cantidad de tablones obtenidos por cada tronco procesado")]
    public int planksPerLog = 5;

    [Header("Contadores")]
    public int currentPlankCount = 0;
    public int currentLogCount = 0;

    private int maxConcurrentProcessing = 2;
    private int currentProcessingCount = 0;

    private Collider[] overlapResults = new Collider[10];

    // Detectar troncos alrededor cada x tiempo en vez de cada update()
    private float detectionInterval = 2f;
    private float nextDetectionTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (Time.time >= nextDetectionTime)
        {
            nextDetectionTime = Time.time + detectionInterval;
            DetectLogs();
        }

        if (currentPlankCount < maxPlanks && currentLogCount > 0 && currentProcessingCount < maxConcurrentProcessing)
        {
            StartCoroutine(ProcessLogCoroutine());
        }
    }

    public void OnLogDelivered(GameObject log)
    {
        currentLogCount++;
        Destroy(log);
        Debug.Log("Aserradero: Tronco entregado. Total de troncos en aserradero: " + currentLogCount);
    }

    private IEnumerator ProcessLogCoroutine()
    {
        currentProcessingCount++;
        Debug.Log("Aserradero: Procesando tronco. Tiempo: " + processingTime + " segundos.");
        yield return new WaitForSeconds(processingTime);
        currentLogCount--;
        currentPlankCount += planksPerLog;
        Debug.Log("Aserradero: Tronco procesado. Se han producido " + planksPerLog +
                  " tablones. Total de tablones: " + currentPlankCount + " | Troncos restantes: " + currentLogCount);
        currentProcessingCount--;
    }

    private void DetectLogs()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, overlapResults, logLayer);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = overlapResults[i];
            GameObject logObject = col.transform.root.gameObject;
            if (logObject.CompareTag("Log"))
            {
                if (logObject.GetComponent<MoveItemTask>() == null)
                {
                    MoveItemTask moveTask = logObject.AddComponent<MoveItemTask>();
                    moveTask.Destination = transform.position;
                    moveTask.OnArrivalCallback = (item) =>
                    {
                        SawmillProcessor.Instance.OnLogDelivered(item);
                    };
                }
            }
        }
    }
}
