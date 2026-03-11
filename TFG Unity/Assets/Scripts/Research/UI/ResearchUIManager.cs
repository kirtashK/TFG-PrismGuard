using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ResearchUIManager : MonoBehaviour, IHideElement
{
    public GameObject panelRoot;
    public RectTransform nodeListParent;
    public GameObject nodeEntryPrefab;
    public ResearchDetailsPanel detailsPanel;

    private readonly Dictionary<string, ResearchNodeEntryUI> nodeEntries = new();

    private InputAction pointerAction;
    private InputAction clickAction;
    private InputAction cancelAction;

    private void Awake()
    {
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

    private void OnDisable()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }
        if (ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnResearchProgressChanged -= OnResearchProgressChanged;
            ResearchManager.Instance.OnResearchCompleted -= OnResearchCompleted;
            ResearchManager.Instance.OnResearchDataLoaded -= OnResearchDataLoaded;
        }
    }

    private IEnumerator RegisterWhenReady()
    {
        while (HideElementManager.Instance == null)
        {
            yield return null;
        }
        HideElementManager.Instance.Register(this);

        while (ResearchManager.Instance == null || !ResearchManager.Instance.IsLoaded)
        {
            yield return null;
        }

        BuildNodeList();

        ResearchManager.Instance.OnResearchProgressChanged += OnResearchProgressChanged;
        ResearchManager.Instance.OnResearchCompleted += OnResearchCompleted;
        ResearchManager.Instance.OnResearchDataLoaded += OnResearchDataLoaded;
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

        // Close when click outside research UI:
        if (clickAction != null && clickAction.triggered)
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPos = pointerAction.ReadValue<Vector2>();

            bool clickedInside;
            Camera uiCamera = null;
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                uiCamera = canvas.worldCamera;
            }

            clickedInside = RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPos, uiCamera);

            if (!clickedInside)
            {
                HidePanel();
            }
        }
    }

    // Called by button press
    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            Debug.LogError($"{name}: missing {nameof(panelRoot)}");
            return;
        }

        if (!panelRoot.activeSelf)
        {
            HideElementManager.Instance.ShowOnly(this);
        }
        panelRoot.SetActive(!panelRoot.activeSelf);
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

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