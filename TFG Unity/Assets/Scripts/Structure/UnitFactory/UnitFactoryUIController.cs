using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitFactoryUIController : MonoBehaviour, IHideElement
{
    public GameObject panelRoot;

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text concurrentText;

    public GameObject unitEntryPrefab;
    public Transform unitListContainer;

    public GameObject orderEntryPrefab;
    public Transform orderListContainer;

    public UnitRegistryAddressables registry;

    private UnitFactory currentFactory;

    private readonly List<GameObject> unitEntryObjects = new();
    private readonly Dictionary<Guid, GameObject> orderEntryObjects = new();

    private bool isRegistered = false;

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
            || ResearchManager.Instance == null
            || InputManager.Instance == null)
        {
            yield return null;
        }
        SelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        ResearchManager.Instance.OnEffectApplied += HandleUnitUnlocked;
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
        if (isRegistered && ResearchManager.Instance != null)
        {
            ResearchManager.Instance.OnEffectApplied -= HandleUnitUnlocked;
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

        // Close when click outside Factory UI:
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

    private void HandleSelectionChanged(IReadOnlyList<ISelectable> selection)
    {
        // Only show unit creation UI when a single unit factory is selected
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

        Transform transform = selected.GetTransform();
        if (transform == null)
        {
            HidePanel();
            return;
        }

        if (!transform.TryGetComponent<UnitFactory>(out UnitFactory factory))
        {
            HidePanel();
            return;
        }
        
        ShowPanelForFactory(factory);
    }

    private void ShowPanelForFactory(UnitFactory factory)
    {
        HideElementManager.Instance.ShowOnly(this);

        currentFactory = factory;
        SetupTexts(factory);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(InputManager.InputMode.UI);
            uiModePushed = true;
        }

        RefreshUnitList();
        RefreshOrderList();

        // Subscribe to updates
        factory.OnOrderEnqueued += OnFactoryOrderEnqueued;
        factory.OnOrderStateChanged += OnFactoryOrderStateChanged;
        factory.OnOrderCompleted += OnFactoryOrderCompleted;
        factory.OnOrderCancelled += OnFactoryOrderCancelled;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        }
    }

    private void SetupTexts(UnitFactory factory)
    {
        if (nameText != null)
        {
            nameText.text = factory.structure.structureData.Name;
        }
        if (capacityText != null)
        {
            capacityText.text = $"Capacity: {factory.QueueCount}/{factory.maxQueueLength}";
        }
        if (concurrentText != null)
        {
            concurrentText.text = $"Slots: {factory.CurrentConcurrentBatches}/{factory.maxConcurrentBatches}";
        }
    }

    public void HidePanel()
    {
        if (currentFactory != null)
        {
            currentFactory.OnOrderEnqueued -= OnFactoryOrderEnqueued;
            currentFactory.OnOrderStateChanged -= OnFactoryOrderStateChanged;
            currentFactory.OnOrderCompleted -= OnFactoryOrderCompleted;
            currentFactory.OnOrderCancelled -= OnFactoryOrderCancelled;
        }

        currentFactory = null;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (uiModePushed)
        {
            InputManager.Instance.PopMode();
            uiModePushed = false;
        }

        ClearUnitList();
        ClearOrderList();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }

    private void RefreshUnitList()
    {
        ClearUnitList();

        if (currentFactory == null)
        {
            return;
        }

        IEnumerable<UnitData> units = currentFactory.GetProducibleUnits(registry);
        foreach (UnitData unit in units)
        {
            GameObject gameObject = Instantiate(unitEntryPrefab, unitListContainer);
            unitEntryObjects.Add(gameObject);
            if (gameObject.TryGetComponent<UnitEntryUI>(out UnitEntryUI unitEntry))
            {
                unitEntry.Setup(unit, () =>
                {
                    Guid orderId = currentFactory.EnqueueProduction(unit);
                    if (orderId == Guid.Empty)
                    {
                        // TODO show feedback "not enough score" or "queue full"
                    }
                });
            }
        }
    }

    private void ClearUnitList()
    {
        foreach (GameObject gameObject in unitEntryObjects)
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        unitEntryObjects.Clear();
    }

    private void RefreshOrderList()
    {
        ClearOrderList();
        if (currentFactory == null)
        {
            return;
        }
        foreach (UnitProductionOrder order in currentFactory.GetAllOrders())
        {
            AddOrUpdateOrderEntry(order);
        }
    }

    private void ClearOrderList()
    {
        foreach (KeyValuePair<Guid, GameObject> keyValue in orderEntryObjects)
        {
            if (keyValue.Value != null)
            {
                Destroy(keyValue.Value);
            }
        }
        orderEntryObjects.Clear();
    }

    private void AddOrUpdateOrderEntry(UnitProductionOrder order)
    {
        if (order == null)
        {
            return;
        }
        if (!orderEntryObjects.TryGetValue(order.orderId, out GameObject entryGameObject))
        {
            entryGameObject = Instantiate(orderEntryPrefab, orderListContainer);
            orderEntryObjects[order.orderId] = entryGameObject;
        }

        if (entryGameObject.TryGetComponent<OrderEntryUI>(out OrderEntryUI unitEntry))
        {
            unitEntry.Setup(order, () => { currentFactory.CancelOrder(order.orderId); });
        }
    }

    // Factory events
    private void OnFactoryOrderEnqueued(UnitFactory factory, UnitProductionOrder order)
    {
        SetupTexts(factory);
        AddOrUpdateOrderEntry(order);
    }

    private void OnFactoryOrderStateChanged(UnitFactory factory, UnitProductionOrder order)
    {
        SetupTexts(factory);
        AddOrUpdateOrderEntry(order);
    }

    private void OnFactoryOrderCompleted(UnitFactory factory, UnitProductionOrder order)
    {
        SetupTexts(factory);
        if (orderEntryObjects.TryGetValue(order.orderId, out GameObject gameObject))
        {
            Destroy(gameObject);
            orderEntryObjects.Remove(order.orderId);
        }
    }

    private void OnFactoryOrderCancelled(UnitFactory factory, UnitProductionOrder order)
    {
        SetupTexts(factory);
        if (orderEntryObjects.TryGetValue(order.orderId, out GameObject gameObject))
        {
            Destroy(gameObject);
            orderEntryObjects.Remove(order.orderId);
        }
    }

    private void OnScoreChanged(int available)
    {
        // Refresh unit list so create-buttons update
        foreach (GameObject gameObject in unitEntryObjects)
        {
            if (gameObject == null)
            {
                continue;
            }
            if (gameObject.TryGetComponent<UnitEntryUI>(out UnitEntryUI unitEntry))
            {
                unitEntry.RefreshInteractivity();
            }
        }
    }

    public void HandleUnitUnlocked(ResearchManager.ResearchEffectEvent researchEffect)
    {
        if (researchEffect.effectType != ResearchManager.ResearchEffectEvent.EffectType.UnlockUnit)
        {
            return;
        }

        OnScoreChanged(0);
    }
}