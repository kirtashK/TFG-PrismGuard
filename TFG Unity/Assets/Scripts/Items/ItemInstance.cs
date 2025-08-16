using UnityEngine;

public class ItemInstance : MonoBehaviour
{
    public ItemData itemData;

    [HideInInspector]
    public Worker carrier;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError($"{name} has null ItemData");
        }
    }
}