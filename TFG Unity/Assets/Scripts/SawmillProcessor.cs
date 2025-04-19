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

    private Collider[] treeResults = new Collider[5];
    private Collider[] logResults = new Collider[10];

    private float detectionInterval = 2f;
    private float nextDetectionTime = 0f;

    [Header("Producción")]
    [Tooltip("Prefab de los tablones")]
    public GameObject plankPrefab;

    [Tooltip("Datos de los tablones")]
    public ItemData plankData;

    [Tooltip("Intervalo en segundos para intentar enviar tablones a almacén")]
    public float dispatchInterval = 5f;
    private float nextDispatchTime = 0f;

    [Tooltip("Lugar donde crear los tablones")]
    [SerializeField]
    private Transform plankDropSpot;



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
            DetectTrees();
            DetectLogs();
        }

        while (currentPlankCount < maxPlanks &&
        currentLogCount > currentProcessingCount &&
        currentProcessingCount < maxConcurrentProcessing)
        {
            StartCoroutine(ProcessLogCoroutine());
        }

        if (Time.time >= nextDispatchTime)
        {
            nextDispatchTime = Time.time + dispatchInterval;
            DispatchPlanksToWarehouse();
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
        Debug.Log($"Aserradero: Procesando tronco ({processingTime}s)...");
        yield return new WaitForSeconds(processingTime);

        currentLogCount--;
        currentPlankCount += planksPerLog;
        Debug.Log($"Aserradero: +{planksPerLog} tablones internos. Stock interno = {currentPlankCount}, troncos = {currentLogCount}");

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

    private void DispatchPlanksToWarehouse()
    {
        if (currentPlankCount <= 0)
            return;

        var warehouses = WarehouseManager.Instance.GetWarehousesThatCanStore(plankData);

        foreach (Warehouse warehouse in warehouses)
        {
            int freeSlots = warehouse.capacity - warehouse.currentCount;
            int toSend = Mathf.Min(currentPlankCount, freeSlots);

            for (int i = 0; i < toSend; i++)
            {
                GameObject plank = Instantiate(plankPrefab, plankDropSpot.position, Quaternion.identity);

                MoveItemTask task = plank.AddComponent<MoveItemTask>();
                task.Destination = warehouse.GetStoragePosition();
                task.OnArrivalCallback = item =>
                {
                    item.transform.position = warehouse.GetStoragePosition();
                    Destroy(item.GetComponent<MoveItemTask>());
                    item.SetActive(true);
                    warehouse.StoreItem(plankData);
                };
            }
            currentPlankCount -= toSend;
            // Reservar espacio en el almacen
            // TODO si el item se suelta por el camino, liberar el espacio reservado
            warehouse.currentCount += toSend;

            if (currentPlankCount <= 0)
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
