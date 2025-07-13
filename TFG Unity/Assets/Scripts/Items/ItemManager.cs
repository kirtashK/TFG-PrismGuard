using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Prefabs & Items")]
    [Tooltip("Prefab del tronco")]
    public GameObject logPrefab;

    [Tooltip("ItemData del tronco")]
    public ItemData logData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    public void CreateTronco(Vector3 position)
    {
        if (logPrefab == null || logData == null)
        {
            Debug.LogError("Falta asignar el prefab o el ItemData.");
            return;
        }

        GameObject newTronco = Instantiate(logPrefab, position, Quaternion.identity);
        ItemInstance itemInstance = newTronco.GetComponent<ItemInstance>();
        if (itemInstance != null)
        {
            itemInstance.itemData = logData;
        }
        else
        {
            Debug.LogError("El prefab de tronco no tiene el componente ItemInstance.");
        }
    }
}
