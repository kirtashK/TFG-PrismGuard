using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public StructureData structureData;

    [HideInInspector]
    public Image iconImage;

    [HideInInspector]
    public TMP_Text nameText;

    public Button button;

    public GameObject lockOverlay;

    public void Setup(StructureData data, System.Action<StructureData> onSelected)
    {
        structureData = data;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
        }
        if (nameText != null)
        {
            nameText.text = data.Name;
        }
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(structureData));
        }

        RefreshLockVisual();
    }

    private string BuildCostString(StructureData data)
    {
        if (data.buildRequirements == null || data.buildRequirements.Count == 0
            || (data.buildRequirements.Count == 1 && data.buildRequirements[0].quantity == 0))
        {
            return "Free";
        }

        StringBuilder cost = new();
        foreach (StructureData.ResourceRequirement requirement in data.buildRequirements)
        {
            if (requirement.itemData != null)
            {
                cost.Append(requirement.quantity).Append("x ").Append(requirement.itemData.Name).Append("\n");
            }
        }

        return cost.ToString().TrimEnd('\n');
    }

    public void RefreshLockVisual()
    {
        bool unlocked = structureData.requiredResearch == null 
            || (ResearchManager.Instance != null 
            && ResearchManager.Instance.HasCompleted(structureData.requiredResearch.id));

        lockOverlay.SetActive(!unlocked);
        button.interactable = unlocked;
    }

    // Show a tooltip showing cost to build
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (structureData == null)
        {
            return;
        }

        if (structureData.requiredResearch != null 
            && ResearchManager.Instance != null && !ResearchManager.Instance.HasCompleted(structureData.requiredResearch.id))
        {
            string lockedTooltip = $"Missing research";
            if (TooltipController.Instance != null)
            {
                TooltipController.Instance.Show(lockedTooltip);
            }
            return;
        }

        string validTooltip = BuildCostString(structureData);
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Show(validTooltip);
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