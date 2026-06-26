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

    public GameObject lockOverlay;

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

        bool availibleScore = ScoreManager.Instance.GetAvailableScore() >= data.scoreCost;
        bool unlocked = data.requiredResearch == null
            || (ResearchManager.Instance != null
            && ResearchManager.Instance.HasCompleted(data.requiredResearch.id));

        createButton.interactable = availibleScore && unlocked;
        lockOverlay.SetActive(!unlocked);
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
                cost.Append(requirement.quantity).Append("x ").Append(requirement.itemData.Name).Append("\n");
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

        if (data.requiredResearch != null
            && ResearchManager.Instance != null && !ResearchManager.Instance.HasCompleted(data.requiredResearch.id))
        {
            string tooltipMissingResearch = $"Missing research";
            if (TooltipController.Instance != null)
            {
                TooltipController.Instance.Show(tooltipMissingResearch);
            }
            return;
        }

        string tooltipValid = CreateUnitCostString(data);
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Show(tooltipValid);
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