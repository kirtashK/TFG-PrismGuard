using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text countText;

    public void Initialize(ItemData data)
    {
        icon.sprite = data.icon;
        gameObject.SetActive(true);
    }

    public void SetCount(int count)
    {
        countText.text = count.ToString();
    }
}