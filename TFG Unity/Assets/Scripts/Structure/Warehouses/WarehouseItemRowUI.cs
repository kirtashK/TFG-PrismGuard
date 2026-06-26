using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehouseItemRowUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;

    public void Setup(ItemData itemData, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite = itemData != null ? itemData.icon : null;
        }
        if (nameText != null)
        {
            nameText.text = itemData != null ? itemData.Name : "Unknown";
        }
        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }
}