using UnityEngine;

public class ItemInstance : MonoBehaviour
{
    public ItemData itemData;

    private void Start()
    {
        if (itemData == null)
        {
            Debug.LogError("No se ha asignado un ItemData a " + gameObject.name);
        }
        else
        {
            Debug.Log(gameObject.name + " - Cargado item: " + itemData.itemName);
        }
    }

    public void Interact()
    {
        Debug.Log("Interacción con " + itemData.itemName + "."
            + "Rango de interacción requerido: " + itemData.interactionRange + "."
            + "Peso: " + itemData.weight + ".");

        // Implementar la lógica de recoger el ítem, etc.
    }
}