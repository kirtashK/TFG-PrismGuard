using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ThresholdUIController : MonoBehaviour, IHideElement
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text structureNameText;
    [SerializeField] private RectTransform rowsParent;
    [SerializeField] private ThresholdRowUI rowPrefab;

    private readonly List<ThresholdRowUI> spawnedRows = new();
    private ResourceProcessor currentProcessor;
    private ResourceGatherer currentGatherer;

    private bool isRegistered;

    private InputSystem_Actions.UIActions uiActions;
    private bool uiModePushed;
    private bool inputReady;

    #region Unity methods

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (SelectionManager.Instance == null 
            || HideElementManager.Instance == null
            || InputManager.Instance == null)
        {
            yield return null;
        }

        SelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        HideElementManager.Instance.Register(this);
        isRegistered = true;

        uiActions = InputManager.Instance.UI;
        inputReady = true;
    }

    private void OnDisable()
    {
        if (isRegistered && SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
        if (isRegistered && HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }

        isRegistered = false;
        inputReady = false;
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (!inputReady)
        {
            return;
        }

        if (uiActions.Cancel.WasPressedThisFrame())
        {
            HidePanel();
            return;
        }

        if (uiActions.Click.WasPressedThisFrame())
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPos = uiActions.Point.ReadValue<Vector2>();

            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                uiCamera = canvas.worldCamera;
            }

            bool clickedInside = RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPos, uiCamera);
            if (!clickedInside)
            {
                HidePanel();
            }
        }
    }

    #endregion

    private void HandleSelectionChanged(IReadOnlyList<ISelectable> selection)
    {
        ClearRows();
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

        if (panelRoot != null)
        {
            HideElementManager.Instance.ShowOnly(this);
            panelRoot.SetActive(true);
        }

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(InputManager.InputMode.UI);
            uiModePushed = true;
        }

        if (structureNameText != null)
        {
            structureNameText.text = title;
        }

        BuildProcessorRows();
    }

    private void ShowForGatherer(ResourceGatherer gatherer, string title)
    {
        currentGatherer = gatherer;
        currentProcessor = null;

        if (panelRoot != null)
        {
            HideElementManager.Instance.ShowOnly(this);
            panelRoot.SetActive(true);
        }

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(InputManager.InputMode.UI);
            uiModePushed = true;
        }

        if (structureNameText != null)
        {
            structureNameText.text = title;
        }

        BuildGathererRows();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (uiModePushed)
        {
            InputManager.Instance.PopMode();
            uiModePushed = false;
        }
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