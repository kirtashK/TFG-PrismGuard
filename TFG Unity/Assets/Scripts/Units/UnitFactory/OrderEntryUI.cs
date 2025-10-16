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
            stateLabel.text = ChangeToHumanReadable(order.State);
        }
    }

    /// <summary>
    /// Changes from PascalCase/CamelCase to human readable phrase
    /// example: "WaitingForItems" = "Waiting for items"
    /// </summary>
    private static string ChangeToHumanReadable(Enum value)
    {
        if (value == null) return string.Empty;

        string raw = value.ToString();

        // Split before each capital letter
        // example: "WaitingForItems" => ["Waiting", "For", "Items"]
        string[] parts = System.Text.RegularExpressions.Regex.Split(raw, @"(?<!^)(?=[A-Z])");

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].ToLowerInvariant();
        }

        // First word capitalized
        if (parts.Length > 0)
        {
            parts[0] = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
        }

        return string.Join(" ", parts);
    }
}