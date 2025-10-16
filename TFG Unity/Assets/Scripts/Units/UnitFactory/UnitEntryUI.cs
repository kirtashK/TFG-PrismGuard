using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text nameLabel;
    public Image iconImage;
    public Button createButton;

    private UnitData data;
    private Action onCreate;

    public void Setup(UnitData unitData, Action onCreateCallback)
    {
        data = unitData;
        onCreate = onCreateCallback;

        if (nameLabel != null)
        {
            nameLabel.text = data != null ? data.Name : "Unknown";
        }
        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
        }
        if (createButton != null)
        {
            createButton.onClick.RemoveAllListeners();
            createButton.onClick.AddListener(() => onCreate?.Invoke());
        }

        RefreshInteractivity();
    }

    public void RefreshInteractivity()
    {
        if (createButton == null || data == null || ScoreManager.Instance == null)
        {
            return;
        }
        int available = ScoreManager.Instance.GetAvailableScore();
        createButton.interactable = (available >= data.scoreCost);
    }

    private string CreateUnitCostString(UnitData data)
    {

        StringBuilder cost = new();

        // Score cost
        cost.Append("Score needed: ");
        if (data.scoreCost > 0)
        {
            cost.Append(data.scoreCost.ToString());
        }
        else
        {
            cost.Append("Free");
        }
        cost.Append("\n");

        // Item cost
        cost.Append("Items needed \n");
        if (data.createCosts == null || data.createCosts.Count == 0)
        {
            cost.Append("None");
        }

        foreach (UnitData.ResourceRequirement requirement in data.createCosts)
        {
            if (requirement.itemData != null)
            {
                cost.Append(requirement.quantity).Append("x ").Append(requirement.itemData.itemName).Append("\n");
            }
        }

        return cost.ToString().TrimEnd('\n');
    }

    // Show a tooltip showing cost to create
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (data == null)
        {
            return;
        }

        string tooltip = CreateUnitCostString(data);
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Show(tooltip);
        }
    }

    // Hide the tooltip
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Hide();
        }
    }
}