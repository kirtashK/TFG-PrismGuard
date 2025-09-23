using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkerData", menuName = "Data/Worker")]
public class WorkerData : ScriptableObject
{
    [Header("Stats")]

    public float maxHealth = 20f;
    public float moveSpeed = 3.5f;

    [Header("Inventory")]

    public float maxCarryWeight = 10f;
}
