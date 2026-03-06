using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Data/Worker")]
public class WorkerData : UnitData
{
    [Header("Worker")]

    [Range(0f, 500f)]
    public float maxCarryWeight = 10f;
}