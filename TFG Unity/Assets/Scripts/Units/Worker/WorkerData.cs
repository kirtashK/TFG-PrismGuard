using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Data/Worker")]
public class WorkerData : UnitData
{
    [Header("Inventory")]

    public float maxCarryWeight = 10f;
}