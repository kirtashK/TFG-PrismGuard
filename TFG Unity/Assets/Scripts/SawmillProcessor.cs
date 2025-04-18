using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SawmillProcessor : MonoBehaviour
{
    public static SawmillProcessor Instance { get; private set; }

    [Header("Detección")]
    [Tooltip("Radio de deteccion")]
    public float detectionRadius = 10f;

    [Tooltip("Capa donde se encuentran los troncos")]
    public LayerMask logLayer;

    [Tooltip("Capa donde se encuentran los árboles")]
    public LayerMask treeLayer;

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

    private Collider[] treeResults = new Collider[20];
    private Collider[] logResults = new Collider[10];

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

        SphereCollider detectionCollider = GetComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
    }

    private void Update()
    {
        if (Time.time >= nextDetectionTime)
        {
            nextDetectionTime = Time.time + detectionInterval;
            DetectTrees();
            DetectLogs();
        }

        while (currentPlankCount < maxPlanks &&
        currentLogCount > currentProcessingCount &&
        currentProcessingCount < maxConcurrentProcessing)
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
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 
            detectionRadius, 
            logResults, 
            logLayer
        );

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = logResults[i];
            GameObject logObject = col.transform.parent.gameObject;

            if (logObject.CompareTag("Log") 
                && logObject.GetComponent<MoveItemTask>() == null)
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

    private void DetectTrees()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position,
            detectionRadius,
            treeResults,
            treeLayer
        );

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = treeResults[i];
            GameObject treeObject = col.transform.parent.gameObject;

            if (treeObject.CompareTag("Tree") 
                && treeObject.GetComponent<ChopTreeTask>() != null
                )
            {
                treeObject.GetComponent<ChopTreeTask>().enabled = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
