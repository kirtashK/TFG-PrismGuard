using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchUIManager : MonoBehaviour, IHideElement
{
    public GameObject panelRoot;
    public RectTransform nodeListParent;
    public GameObject nodeEntryPrefab;
    public ResearchDetailsPanel detailsPanel;

    private readonly Dictionary<string, ResearchNodeEntryUI> nodeEntries = new();

    private InputSystem_Actions.UIActions uiActions;
    private bool uiModePushed;
    private bool inputReady;

    #region Unity methods

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (HideElementManager.Instance == null
            || ResearchManager.Instance == null || !ResearchManager.Instance.IsLoaded
            || InputManager.Instance == null)
        {
            yield return null;
        }

        HideElementManager.Instance.Register(this);

        BuildNodeList();

        ResearchManager.Instance.OnResearchProgressChanged += OnResearchProgressChanged;
        ResearchManager.Instance.OnResearchCompleted += OnResearchCompleted;
        ResearchManager.Instance.OnResearchDataLoaded += OnResearchDataLoaded;

        uiActions = InputManager.Instance.UI;
        inputReady = true;
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

        inputReady = false;
    }


    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if(!inputReady)
        {
            return;
        }

        if (uiActions.Cancel.WasPressedThisFrame())
        {
            HidePanel();
            return;
        }

        // Close when click outside research UI:
        if (uiActions.Click.WasPressedThisFrame())
        {
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            Vector2 pointerPos = uiActions.Point.ReadValue<Vector2>();

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

    #endregion

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

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(InputManager.InputMode.UI);
            uiModePushed = true;
        }
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