using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResearchDetailsPanel : MonoBehaviour
{
    public Image icon;
    public TMP_Text title;
    public TMP_Text description;
    public Image progressFill;
    public TMP_Text progressText;
    public Button setActiveButton;
    public RectTransform effectsParent;
    public GameObject effectRowPrefab;

    private string showingResearchId;

    private void OnEnable()
    {
        setActiveButton.onClick.AddListener(OnSetActiveClicked);
    }

    private void OnDisable()
    {
        setActiveButton.onClick.RemoveListener(OnSetActiveClicked);
    }

    public void Show(ResearchData researchData)
    {
        showingResearchId = researchData.id;

        icon.gameObject.SetActive(true);
        title.gameObject.SetActive(true);

        icon.sprite = researchData.icon;
        title.text = researchData.Name;
        description.text = researchData.description;

        float currentProgress = ResearchManager.Instance.GetProgress(showingResearchId);
        float requiredProgress = ResearchManager.Instance.GetRequiredPoints(showingResearchId);
        SetProgress(currentProgress, requiredProgress);

        for (int i = effectsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(effectsParent.GetChild(i).gameObject);
        }

        foreach (ResearchEffect effect in researchData.effects)
        {
            GameObject gameObject = Instantiate(effectRowPrefab, effectsParent);
            if (gameObject.TryGetComponent<EffectRowUI>(out EffectRowUI effectRow))
            {
                effectRow.Setup(effect);
            }
        }

        setActiveButton.interactable = ResearchManager.Instance.CanSelectResearch(showingResearchId)
            && !ResearchManager.Instance.HasCompleted(showingResearchId);

        SetActiveButtonTooltip tip = setActiveButton.gameObject.GetComponent<SetActiveButtonTooltip>();
        tip.researchId = showingResearchId;
    }

    public bool IsShowing(string researchId)
    {
        return showingResearchId == researchId;
    }

    public void SetProgress(float currentProgress, float requiredProgress)
    {
        float fillRatio = requiredProgress > 0f ? currentProgress / requiredProgress : 0f;
        progressFill.fillAmount = Mathf.Clamp01(fillRatio);
        progressText.text = $"{currentProgress:0}/{requiredProgress:0}";
    }

    public void OnResearchCompleted()
    {
        progressFill.fillAmount = 1f;

        setActiveButton.interactable = false;

        SetActiveButtonTooltip tip = setActiveButton.gameObject.GetComponent<SetActiveButtonTooltip>();
        tip.researchId = showingResearchId;
    }

    private void OnSetActiveClicked()
    {
        if (!ResearchManager.Instance.SetActiveResearch(showingResearchId))
        {
            Debug.LogWarning($"{name}: Failed to set active research {showingResearchId}");
        }
    }
}