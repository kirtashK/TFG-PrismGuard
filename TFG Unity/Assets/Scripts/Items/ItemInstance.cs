using UnityEngine;

public class ItemInstance : MonoBehaviour
{
    public ItemData itemData;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError("No se ha asignado ItemData a " + gameObject.name);
        }
    }
}