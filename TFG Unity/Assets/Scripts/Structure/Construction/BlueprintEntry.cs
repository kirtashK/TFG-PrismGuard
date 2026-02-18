using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlueprintEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TMP_Text nameText;
    public Button button;

    private StructureData boundData;

    public void Setup(StructureData data, System.Action<StructureData> onSelected)
    {
        boundData = data;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
        }

        if (nameText != null)
        {
            nameText.text = data.structureName;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelected?.Invoke(boundData));
        }
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
                cost.Append(requirement.quantity).Append("x ").Append(requirement.itemData.itemName).Append("\n");
            }
        }

        return cost.ToString().TrimEnd('\n');
    }

    // Show a tooltip showing cost to build
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundData == null)
        {
            return;
        }

        string tooltip = BuildCostString(boundData);
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