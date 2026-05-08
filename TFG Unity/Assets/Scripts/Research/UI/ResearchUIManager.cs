using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchUIManager : BasePanel
{
    public RectTransform nodeListParent;
    public GameObject nodeEntryPrefab;
    public ResearchDetailsPanel detailsPanel;

    private readonly Dictionary<string, ResearchNodeEntryUI> nodeEntries = new();

    #region Unity methods

    protected override void OnPanelReady()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (ResearchManager.Instance == null 
            || !ResearchManager.Instance.IsLoaded)
        {
            yield return null;
        }

        BuildNodeList();

        ResearchManager.Instance.OnResearchProgressChanged += OnResearchProgressChanged;
        ResearchManager.Instance.OnResearchCompleted += OnResearchCompleted;
        ResearchManager.Instance.OnResearchDataLoaded += OnResearchDataLoaded;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchProgressChanged -= OnResearchProgressChanged;
            ResearchManager.Instance.OnResearchCompleted -= OnResearchCompleted;
            ResearchManager.Instance.OnResearchDataLoaded -= OnResearchDataLoaded;
        }
    }

    #endregion

    void OnResearchDataLoaded()
    {
        BuildNodeList();
    }

    private void BuildNodeList()
    {
        // Clear existing entries
        for (int i = nodeListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(nodeListParent.GetChild(i).gameObject);
        }
        nodeEntries.Clear();

        foreach (ResearchData researchData in ResearchManager.Instance.AllResearchData())
        {
            GameObject gameObject = Instantiate(nodeEntryPrefab, nodeListParent);
            if (!gameObject.TryGetComponent<ResearchNodeEntryUI>(out ResearchNodeEntryUI entry))
            {
                Debug.LogError($"{name}: {nameof(nodeEntryPrefab)} missing {nameof(ResearchNodeEntryUI)}");
                continue;
            }

            entry.Setup(researchData, OnNodeClicked);
            nodeEntries[researchData.id] = entry;
        }
    }

    private void OnNodeClicked(string researchId)
    {
        if (!ResearchManager.Instance.TryGetResearchData(researchId, out ResearchData researchData))
        {
            Debug.LogWarning($"{name}: research not found: {researchId}");
            return;
        }

        detailsPanel.Show(researchData);
    }

    private void OnResearchProgressChanged(string id, float current, float required)
    {
        if (nodeEntries.TryGetValue(id, out ResearchNodeEntryUI entry))
        {
            entry.SetProgress(required > 0f ? current / required : 0f);
        }

        if (detailsPanel.IsShowing(id))
        {
            detailsPanel.SetProgress(current, required);
        }
    }

    private void OnResearchCompleted(string id)
    {
        BuildNodeList();

        if (nodeEntries.TryGetValue(id, out ResearchNodeEntryUI entry))
        {
            entry.MarkCompleted();
        }

        if (detailsPanel.IsShowing(id))
        {
            detailsPanel.OnResearchCompleted();
        }
    }
}