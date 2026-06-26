using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

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

    public GameObject CreateItem(Vector3 position, GameObject resource)
    {
        if (resource == null)
        {
            Debug.LogError(name + " received null resource");
            return null;
        }

        return Instantiate(resource, position, Quaternion.identity);
    }
}
