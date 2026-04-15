using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThresholdUIController : MonoBehaviour, IHideElement
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text structureNameText;
    [SerializeField] private RectTransform rowsParent;
    [SerializeField] private ThresholdRowUI rowPrefab;

    private readonly List<ThresholdRowUI> spawnedRows = new();
    private ResourceProcessor currentProcessor;
    private ResourceGatherer currentGatherer;

    private InputAction pointerAction;
    private InputAction clickAction;
    private InputAction cancelAction;

    private bool isRegistered;

    #region Unity methods

    private void Awake()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        pointerAction = new InputAction("Pointer", InputActionType.Value, "<Pointer>/position");
        clickAction = new InputAction("LeftClick", InputActionType.Button, "<Mouse>/leftButton");
        cancelAction = new InputAction("CancelUI", InputActionType.Button);
        cancelAction.AddBinding("<Keyboard>/escape");
        cancelAction.AddBinding("<Mouse>/rightButton");

        pointerAction.Enable();
        clickAction.Enable();
        cancelAction.Enable();
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (SelectionManager.Instance == null || HideElementManager.Instance == null)
        {
            yield return null;
        }

        SelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        HideElementManager.Instance.Register(this);
        isRegistered = true;
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
    }

    private void OnDestroy()
    {
        pointerAction?.Dispose();
        clickAction?.Dispose();
        cancelAction?.Dispose();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (cancelAction != null && cancelAction.triggered)
        {
            HidePanel();
            return;
        }

        if (clickAction != null && clickAction.triggered)
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPos = pointerAction.ReadValue<Vector2>();

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