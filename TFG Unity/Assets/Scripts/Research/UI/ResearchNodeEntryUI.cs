using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchNodeEntryUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text titleText;
    public Image progressFill;
    public GameObject lockOverlay;
    public Button button;

    private string researchId;

    public void Setup(ResearchData data, System.Action<string> onClick)
    {
        researchId = data.id;
        iconImage.sprite = data.icon;
        titleText.text = data.Name;

        float currentProgress = ResearchManager.Instance.GetProgress(researchId);
        float requiredProgress = ResearchManager.Instance.GetRequiredPoints(researchId);
        SetProgress(requiredProgress > 0f ? currentProgress / requiredProgress : 0f);

        bool canSelect = ResearchManager.Instance.CanSelectResearch(researchId);
        lockOverlay.SetActive(!canSelect);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(researchId));
    }

    public void SetProgress(float fillRatio)
    {
        progressFill.fillAmount = Mathf.Clamp01(fillRatio);
    }

    public void MarkCompleted()
    {
        progressFill.fillAmount = 1f;
    }
}