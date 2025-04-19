using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Prefabs & Items")]
    [Tooltip("Prefab of log")]
    public GameObject logPrefab;

    [Tooltip("ItemData of log")]
    public ItemData logData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void CreateTronco(Vector3 position)
    {
        if (logPrefab == null || logData == null)
        {
            Debug.LogError("Falta asignar el prefab o el ItemData en el ItemManager.");
            return;
        }

        GameObject newTronco = Instantiate(logPrefab, position, Quaternion.identity);
        ItemInstance itemInstance = newTronco.GetComponent<ItemInstance>();
        if (itemInstance != null)
        {
            itemInstance.itemData = logData;
            //Debug.Log("Se ha creado un tronco en la posición: " + position);
        }
        else
        {
            Debug.LogError("El prefab de tronco no tiene el componente ItemInstance.");
        }
    }
}
