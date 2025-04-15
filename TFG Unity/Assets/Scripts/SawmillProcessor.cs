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
        // Detectar troncos en el radio de detección y generar tareas de traslado
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, logLayer);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Log"))
            {
                if (col.GetComponent<MoveLogTask>() == null)
                {
                    MoveLogTask moveTask = col.gameObject.AddComponent<MoveLogTask>();
                    moveTask.Destination = transform.position;
                }
            }
        }

        if (currentPlankCount < maxPlanks && currentLogCount > 0 && currentProcessingCount < maxConcurrentProcessing)
        {
            StartCoroutine(ProcessLogCoroutine());
        }
    }

    // Método llamado desde MoveLogTask cuando el tronco es entregado al aserradero
    public void OnLogDelivered(GameObject log)
    {
        currentLogCount++;
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
}
