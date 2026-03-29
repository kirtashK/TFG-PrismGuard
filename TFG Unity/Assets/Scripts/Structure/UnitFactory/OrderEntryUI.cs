using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrderEntryUI : MonoBehaviour
{
    public TMP_Text nameLabel;
    public Image iconImage;
    public TMP_Text stateLabel;
    public Button cancelButton;

    private static Color readyColor = Color.green;
    private static Color completedColor = Color.green;
    private static Color buildingColor = new(1f, 0.6f, 0f);
    private static Color waitingColor = Color.red;
    private static Color cancelledColor = Color.red;

    private UnitProductionOrder order;
    private Action onCancel;

    public void Setup(UnitProductionOrder order, Action onCancelCallback)
    {
        this.order = order;
        this.onCancel = onCancelCallback;

        if (nameLabel != null)
        {
            nameLabel.text = order.unitData != null ? order.unitData.Name : "Unknown";
        }
        if (iconImage != null)
        {
            iconImage.sprite = order.unitData != null ? order.unitData.icon : null;
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => onCancel?.Invoke());
        }

        Refresh();
        order.OnStateChanged += (_) => Refresh();
        order.OnCompleted += (_) => Refresh();
        order.OnCancelled += (_) => Refresh();
    }

    private void Refresh()
    {
        if (stateLabel != null)
        {
            stateLabel.color = GetColorForState(order.State);
            stateLabel.text = ChangeToHumanReadable(order.State);
        }
    }

    /// <summary>
    /// Changes from PascalCase/CamelCase to human readable phrase
    /// </summary>
    private static string ChangeToHumanReadable(Enum value)
    {
        if (value == null) return string.Empty;

        string raw = value.ToString();

        // Split before each capital letter
        // example: "WaitingForItems" = "Waiting", "For", "Items"
        string[] parts = System.Text.RegularExpressions.Regex.Split(raw, @"(?<!^)(?=[A-Z])");

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].ToLowerInvariant();
        }

        if (parts.Length > 0)
        {
            parts[0] = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
        }

        return string.Join(" ", parts);
    }

    private static Color GetColorForState(UnitProductionOrder.OrderState state)
    {
        return state switch
        {
            UnitProductionOrder.OrderState.Ready => readyColor,
            UnitProductionOrder.OrderState.Completed => completedColor,
            UnitProductionOrder.OrderState.Building => buildingColor,
            UnitProductionOrder.OrderState.WaitingForItems => waitingColor,
            UnitProductionOrder.OrderState.Cancelled => cancelledColor,
            _ => Color.white,
        };
    }
}