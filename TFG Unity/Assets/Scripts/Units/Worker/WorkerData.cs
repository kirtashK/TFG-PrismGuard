using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Data/Worker")]
public class WorkerData : UnitData
{
    [Header("Inventory")]

    [Range(0f, 500f)]
    public float maxCarryWeight = 10f;

    void OnValidate()
    {
        // Fix negative values
        maxCarryWeight = Mathf.Max(0f, maxCarryWeight);
    }
}