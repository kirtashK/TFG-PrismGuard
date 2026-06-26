using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SetActiveButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public string researchId;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(researchId) || ResearchManager.Instance == null || TooltipController.Instance == null)
        {
            return;
        }

        if (ResearchManager.Instance.HasCompleted(researchId))
        {
            string tooltipAlreadyCompleted = "Research already completed!";
            TooltipController.Instance.Show(tooltipAlreadyCompleted);
            return;
        }

        if (!ResearchManager.Instance.TryGetResearchData(researchId, out ResearchData data))
        {
            return;
        }

        List<string> missingPrerequisite = new();
        if (data.prerequisiteResearchDatas != null)
        {
            foreach (ResearchData prereqData in data.prerequisiteResearchDatas)
            {
                if (string.IsNullOrEmpty(prereqData.id))
                {
                    continue;
                }
                if (!ResearchManager.Instance.HasCompleted(prereqData.id))
                {
                    missingPrerequisite.Add(prereqData.Name);
                }
            }
        }

        if (missingPrerequisite.Count == 0)
        {
            return;
        }

        string tooltip = "Requires:\n" + string.Join("\n", missingPrerequisite.Select(n => $"- {n}"));
        TooltipController.Instance.Show(tooltip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Hide();
        }
    }
}