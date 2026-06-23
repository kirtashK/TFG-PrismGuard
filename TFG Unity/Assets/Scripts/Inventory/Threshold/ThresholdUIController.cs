using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ThresholdUIController : BasePanel
{
    [SerializeField] private TMP_Text structureNameText;
    [SerializeField] private TMP_Text infoText;

    [SerializeField] private RectTransform rowsParent;
    [SerializeField] private ThresholdRowUI rowPrefab;

    private readonly List<ThresholdRowUI> spawnedRows = new();
    private ResourceProcessor currentProcessor;
    private ResourceGatherer currentGatherer;

    private bool isRegistered;

    #region Unity methods

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    protected override void OnPanelReady()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (SelectionManager.Instance == null )
        {
            yield return null;
        }

        SelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        isRegistered = true;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isRegistered && SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        if (currentProcessor != null)
        {
            currentProcessor.OnProcessingCountChanged -= HandleProcessingCountChanged;
        }

        isRegistered = false;
    }

    #endregion

    private void HandleSelectionChanged(IReadOnlyList<ISelectable> selection)
    {
        ClearRows();

        if (currentProcessor != null)
        {
            currentProcessor.OnProcessingCountChanged -= HandleProcessingCountChanged;
        }

        currentProcessor = null;
        currentGatherer = null;

        if (selection == null || selection.Count != 1)
        {
            HidePanel();
            return;
        }

        ISelectable selected = selection[0];
        if (selected == null)
        {
            HidePanel();
            return;
        }

        Transform selectedTransform = selected.GetTransform();
        if (selectedTransform == null)
        {
            HidePanel();
            return;
        }

        if (selectedTransform.TryGetComponent<ResourceProcessor>(out ResourceProcessor processor))
        {
            ShowForProcessor(processor, selected.GetDisplayName());
            return;
        }

        if (selectedTransform.TryGetComponent<ResourceGatherer>(out ResourceGatherer gatherer))
        {
            ShowForGatherer(gatherer, selected.GetDisplayName());
            return;
        }

        HidePanel();
    }

    private void ShowForProcessor(ResourceProcessor processor, string title)
    {
        currentProcessor = processor;
        currentGatherer = null;

        ShowPanel();

        if (structureNameText != null)
        {
            structureNameText.text = title;
        }

        RefreshProcessorInfoText();

        currentProcessor.OnProcessingCountChanged += HandleProcessingCountChanged;

        BuildProcessorRows();
    }

    private void ShowForGatherer(ResourceGatherer gatherer, string title)
    {
        currentGatherer = gatherer;
        currentProcessor = null;

        ShowPanel();

        if (structureNameText != null)
        {
            structureNameText.text = title;
        }

        RefreshGathererInfoText();

        BuildGathererRows();
    }

    private void HandleProcessingCountChanged(int newCount)
    {
        RefreshProcessorInfoText();
    }

    private void RefreshProcessorInfoText()
    {
        if (infoText == null || currentProcessor == null)
        {
            return;
        }

        infoText.text = $"Batches: {currentProcessor.ProcessingCount}/{currentProcessor.MaxConcurrentBatches}\n" +
            $"Processing speed multiplier: {currentProcessor.ProcessingSpeed:0.0}";
    }

    private void RefreshGathererInfoText()
    {
        if (infoText == null || currentGatherer == null)
        {
            return;
        }

        infoText.text = $"Gathering radius: {currentGatherer.GatheringRadius}";
    }

    private void BuildProcessorRows()
    {
        if (currentProcessor == null || rowPrefab == null || rowsParent == null)
        {
            return;
        }

        foreach (ProcessResourceRecipe recipe in currentProcessor.GetRecipes())
        {
            if (recipe == null || recipe.outputItemData == null)
            {
                continue;
            }

            ThresholdRowUI row = Instantiate(rowPrefab, rowsParent);
            spawnedRows.Add(row);

            row.Setup(recipe.outputItemData, recipe.outputItemData.icon, recipe.Name,
                currentProcessor.GetThreshold(recipe.outputItemData), value => currentProcessor.SetThreshold(recipe.outputItemData, value));
        }
    }

    private void BuildGathererRows()
    {
        if (currentGatherer == null || rowPrefab == null || rowsParent == null)
        {
            return;
        }

        foreach (ItemData outputItem in currentGatherer.GetManagedOutputItems())
        {
            if (outputItem == null)
            {
                continue;
            }

            ThresholdRowUI row = Instantiate(rowPrefab, rowsParent);
            spawnedRows.Add(row);

            row.Setup(outputItem, outputItem.icon, outputItem.Name,
                currentGatherer.GetThreshold(outputItem), value => currentGatherer.SetThreshold(outputItem, value));
        }
    }

    private void ClearRows()
    {
        for (int i = rowsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(rowsParent.GetChild(i).gameObject);
        }

        spawnedRows.Clear();
    }
}