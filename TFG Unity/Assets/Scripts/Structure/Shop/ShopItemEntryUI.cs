using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameLabel;

    public TMP_Text buyPriceLabel;
    public TMP_Text sellPriceLabel;

    public Button plusButton;
    public Button minusButton;
    public TMP_InputField quantityField;

    private ItemData itemData;
    private int buyPrice = 0;
    private int sellPrice = 0;

    public event Action<ItemData, int, int> OnQuantityChanged;

    private void Awake()
    {
        if (quantityField != null)
        {
            quantityField.text = "0";
            quantityField.onEndEdit.RemoveAllListeners();
            quantityField.onEndEdit.AddListener(OnQuantityFieldEditEnd);
        }

        if (plusButton != null)
        {
            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(() => ChangeQuantityBy(+1));
        }
        if (minusButton != null)
        {
            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(() => ChangeQuantityBy(-1));
        }
    }

    public void Setup(ItemData item, int buyPricePerUnit, int sellPricePerUnit)
    {
        itemData = item;
        buyPrice = buyPricePerUnit;
        sellPrice = sellPricePerUnit;

        if (iconImage != null)
        {
            iconImage.sprite = item != null ? item.icon : null;
        }
        if (nameLabel != null)
        {
            nameLabel.text = item != null ? item.Name : "Unknown";
        }
        if (buyPriceLabel != null)
        {
            if (itemData.canBeBought)
            {
                buyPriceLabel.text = $"Buy: {buyPrice}";
            }
            else
            {
                buyPriceLabel.text = $"Buy: not interested";
            }
        }
        if (sellPriceLabel != null)
        {
            if (itemData.canBeSold)
            {
                sellPriceLabel.text = $"Sell: {sellPrice}";
            }
            else
            {
                sellPriceLabel.text = $"Sell: not interested";
            }
        }

        SetQuantity(0, notify: false);
    }

    private void ChangeQuantityBy(int delta)
    {
        int current = ParseQuantityField();
        int next = current + delta;
        SetQuantity(next, notify: true);
    }

    private void OnQuantityFieldEditEnd(string text)
    {
        if (!int.TryParse(text, out int parsed))
        {
            parsed = 0;
        }
        SetQuantity(parsed, notify: true);
    }

    private int ParseQuantityField()
    {
        if (quantityField == null) { return 0; }

        if (!int.TryParse(quantityField.text, out int value))
        {
            value = 0;
        }
        return value;
    }

    private void SetQuantity(int value, bool notify)
    {
        if (quantityField != null)
        {
            quantityField.text = value.ToString();
        }

        if (notify && itemData != null)
        {
            int buyQuantity = value;
            int sellQuantity = -value;

            OnQuantityChanged?.Invoke(itemData, buyQuantity, sellQuantity);
        }
    }

    public void ResetQuantity()
    {
        SetQuantity(0, notify: true);
    }
}
