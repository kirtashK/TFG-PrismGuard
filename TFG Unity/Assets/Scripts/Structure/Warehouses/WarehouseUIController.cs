using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class WarehouseUIController : BasePanel
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private RectTransform rowsParent;
    [SerializeField] private WarehouseItemRowUI rowPrefab;

    private readonly List<WarehouseItemRowUI> spawnedRows = new();
    private Warehouse currentWarehouse;
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
        while (SelectionManager.Instance == null)
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

        UnsubscribeFromWarehouse();

        isRegistered = false;
    }

    #endregion

    private void HandleSelectionChanged(IReadOnlyList<ISelectable> selection)
    {
        ClearRows();
        UnsubscribeFromWarehouse();
        currentWarehouse = null;

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

        if (!selectedTransform.TryGetComponent<Warehouse>(out Warehouse warehouse))
        {
            HidePanel();
            return;
        }

        ShowForWarehouse(warehouse, selected.GetDisplayName());
    }

    private void ShowForWarehouse(Warehouse warehouse, string title)
    {
        currentWarehouse = warehouse;

        ShowPanel();

        if (nameText != null)
        {
            nameText.text = title;
        }

        currentWarehouse.OnItemStored += HandleWarehouseChanged;
        currentWarehouse.OnItemRetrieved += HandleWarehouseChanged;

        RefreshAll();
    }

    private void HandleWarehouseChanged(ItemData itemData)
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshCapacityText();
        BuildItemRows();
    }

    private void RefreshCapacityText()
    {
        if (capacityText == null || currentWarehouse == null)
        {
            return;
        }

        capacityText.text = $"Capacity: {currentWarehouse.CurrentCapacity}/{currentWarehouse.MaxCapacity}";
    }

    private void BuildItemRows()
    {
        ClearRows();

        if (currentWarehouse == null || rowPrefab == null || rowsParent == null)
        {
            return;
        }

        IReadOnlyDictionary<ItemData, int> snapshot = currentWarehouse.GetStoredCountsSnapshot();

        foreach (KeyValuePair<ItemData, int> pair in snapshot.OrderBy(entry => entry.Key.Name))
        {
            if (pair.Key == null || pair.Value <= 0)
            {
                continue;
            }

            WarehouseItemRowUI row = Instantiate(rowPrefab, rowsParent);
            row.Setup(pair.Key, pair.Value);
            spawnedRows.Add(row);
        }
    }

    private void ClearRows()
    {
        for (int i = spawnedRows.Count - 1; i >= 0; i--)
        {
            if (spawnedRows[i] != null)
            {
                Destroy(spawnedRows[i].gameObject);
            }
        }
        spawnedRows.Clear();
    }

    private void UnsubscribeFromWarehouse()
    {
        if (currentWarehouse == null)
        {
            return;
        }

        currentWarehouse.OnItemStored -= HandleWarehouseChanged;
        currentWarehouse.OnItemRetrieved -= HandleWarehouseChanged;
    }

    protected override void OnPanelHidden()
    {
        UnsubscribeFromWarehouse();
        currentWarehouse = null;
        ClearRows();
    }
}