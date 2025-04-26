using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SawmillProcessor : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Radio de deteccion")]
    public float detectionRadius = 10f;

    [Tooltip("Capa donde se encuentran los troncos")]
    public LayerMask logLayer;

    [Tooltip("Capa donde se encuentran los árboles")]
    public LayerMask treeLayer;

    [Tooltip("Datos de los troncos")]
    public ItemData logData;

    [Header("Procesamiento")]
    [Tooltip("Máximo de tablones que puede producir el aserradero")]
    public int maxPlanks = 30;

    [Tooltip("Máximo de troncos sin procesar que puede almacenar el aserradero")]
    public int maxLogs = 6;

    [Tooltip("Tiempo en segundos para procesar cada tronco")]
    public float processingTime = 10f;

    [Tooltip("Cantidad de tablones obtenidos por cada tronco procesado")]
    public int planksPerLog = 5;

    [Header("Contadores")]
    public int currentPlankCount = 0;
    public int currentLogCount = 0;

    private int maxConcurrentProcessing = 2;
    private int currentProcessingCount = 0;

    private int reservedPlankCount = 0;

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
    public float dispatchInterval = 3f;
    private float nextDispatchTime = 0f;

    [Tooltip("Lugar donde crear los tablones")]
    [SerializeField]
    private Transform plankDropSpot;


    private void Update()
    {
        if (Time.time >= nextDetectionTime &&
            currentLogCount < maxLogs)
        {
            nextDetectionTime = Time.time + detectionInterval;
            DetectTrees();
            DetectLogs();
        }

        while (currentPlankCount + planksPerLog + reservedPlankCount <= maxPlanks &&
        currentLogCount > currentProcessingCount &&
        currentProcessingCount < maxConcurrentProcessing)
        {
            reservedPlankCount += planksPerLog;
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
        //Debug.Log("Aserradero: Tronco entregado. Total de troncos en aserradero: " + currentLogCount);
    }

    private IEnumerator ProcessLogCoroutine()
    {
        currentProcessingCount++;
        Debug.Log($"Aserradero: Procesando tronco ({processingTime}s)...");
        yield return new WaitForSeconds(processingTime);

        currentLogCount--;
        reservedPlankCount -= planksPerLog;
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
                moveTask.TaskData = logData;
                moveTask.Destination = transform.position;

                moveTask.OnArrivalCallback = (item) =>
                {
                    OnLogDelivered(item);
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
        int remainingPlanks = currentPlankCount;
        while (remainingPlanks > 0)
        {
            Warehouse warehouse = WarehouseManager.Instance.FindNearestForStore(
                transform.position,
                plankData
            );
            if (warehouse == null)
                break;

            int freeSlots = warehouse.FreeSlots;
            if (freeSlots <= 0)
                break;

            int toSend = Mathf.Min(remainingPlanks, freeSlots);
            for (int i = 0; i < toSend; i++)
            {
                warehouse.ReserveSlot();

                GameObject plank = Instantiate(plankPrefab, plankDropSpot.position, plankDropSpot.rotation);
                MoveItemTask task = plank.AddComponent<MoveItemTask>();
                task.TaskData = plankData;
                task.Destination = warehouse.GetStoragePosition();
                task.OnArrivalCallback = itemObj =>
                {
                    itemObj.SetActive(true);
                    warehouse.StoreItem(itemObj, plankData);
                    Destroy(itemObj.GetComponent<MoveItemTask>());
                };
            }

            remainingPlanks -= toSend;
        }

        currentPlankCount = remainingPlanks;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
