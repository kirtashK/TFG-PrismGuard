using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// Controls the shop UI panel
/// Automatically loads all ItemData assets and creates a ShopItemEntryUI per ItemData
/// </summary>
public class ShopUIManager : BasePanel
{
    public static ShopUIManager Instance { get; private set; }

    [Header("Addressables")]
    [Tooltip("Label used in Addressables for ItemData")]
    public string itemsLabel = "Item";

    [Header("UI refs")]
    public TMP_Text shopNameText;
    public RectTransform itemListContent;
    public GameObject itemEntryUIPrefab;
    public TMP_Text netTotalText;
    public Button confirmButton;
    public Button resetButton;
    public Button closeButton;
    public TMP_Text statusText;

    private List<ItemData> loadedItems = new();
    private readonly List<ShopItemEntryUI> entryInstances = new();

    private readonly Dictionary<string, int> pendingBuys = new();
    private readonly Dictionary<string, int> pendingSells = new();
    private readonly Dictionary<string, ItemData> pendingItemsById = new();

    private AsyncOperationHandle<IList<ItemData>> loadHandle;
    private bool isLoaded = false;

    private bool isRegistered = false;

    private Structure currentStructure;

    #region Unity methods

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    protected override void OnPanelReady()
    {
        StartCoroutine(RegisterWhenReady());
        StartCoroutine(LoadItemsWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (SelectionManager.Instance == null)
        {
            yield return null;
        }

        SelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        isRegistered = true;

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isRegistered && SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        isRegistered = false;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
        }
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
        }

        if (isLoaded && loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
            loadedItems.Clear();
            isLoaded = false;
        }
    }

    #endregion

    private IEnumerator LoadItemsWhenReady()
    {
        if (!isLoaded)
        {
            yield return null;
        }

        loadHandle = Addressables.LoadAssetsAsync<ItemData>(
            itemsLabel, 
            data => { }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedItems = new List<ItemData>(loadHandle.Result);
            isLoaded = true;

            PopulateItemList();
        }
        else
        {
            Debug.LogWarning($"{name}: failed to load ItemData addressables");
        }
    }

    private void PopulateItemList()
    {
        if (itemEntryUIPrefab == null || itemListContent == null)
        {
            Debug.LogError($"{name}: item entry prefab or item List content not assigned");
            return;
        }

        // Clear any existing entries
        foreach (Transform transform in itemListContent)
        {
            Destroy(transform.gameObject);
        }
        entryInstances.Clear();

        List<ItemData> sortedList = loadedItems.OrderBy(item => item.name).ToList();

        foreach (ItemData item in sortedList)
        {
            GameObject gameobject = Instantiate(itemEntryUIPrefab, itemListContent);
            if (!gameobject.TryGetComponent(out ShopItemEntryUI itemEntry))
            {
                Debug.LogError($"{name}: {itemEntry.name} missing ShopItemEntryUI component");
                Destroy(gameobject);
                continue;
            }

            int buyPrice = GetBuyPriceForItem(item);
            int sellPrice = GetSellPriceForItem(item);

            itemEntry.Setup(item, buyPrice, sellPrice);
            itemEntry.OnQuantityChanged += Slot_OnQuantityChanged;

            entryInstances.Add(itemEntry);
        }

        pendingBuys.Clear();
        pendingSells.Clear();
        RefreshTexts();
    }

    private void Slot_OnQuantityChanged(ItemData item, int buyQuantity, int sellQuantity)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
        {
            return;
        }

        string itemId = item.id;
        pendingItemsById[itemId] = item;

        if (buyQuantity > 0)
        {
            if (item.canBeBought)
            {
                pendingBuys[itemId] = buyQuantity;
            }
            else
            {
                statusText.text = $"{item.Name} cannot be bought";
            }
        }
        else
        {
            pendingBuys.Remove(itemId);
        }

        if (sellQuantity > 0)
        {
            if (item.canBeSold)
            {
                pendingSells[itemId] = sellQuantity;
            }
            else
            {
                statusText.text = $"{item.Name} cannot be sold";
            }
        }
        else
        {
            pendingSells.Remove(itemId);
        }

        RefreshTexts();
    }

    private int GetBuyPriceForItem(ItemData item)
    {
        return item.buyPrice;
    }

    private int GetSellPriceForItem(ItemData item)
    {
        return Mathf.Max(0, Mathf.FloorToInt(GetBuyPriceForItem(item) * item.sellFraction));
    }

    private void RefreshTexts()
    {
        int totalBuy = pendingBuys.Sum(pending => pending.Value * GetBuyPriceForItem(pendingItemsById[pending.Key]));
        int totalSell = pendingSells.Sum(pending => pending.Value * GetSellPriceForItem(pendingItemsById[pending.Key]));
        int net = totalSell - totalBuy;

        if (netTotalText != null)
        {
            netTotalText.text = $"Total balance: {net}";
        }
    }

    private void HandleSelectionChanged(IReadOnlyList<ISelectable> list)
    {
        if (list == null || list.Count != 1)
        {
            HidePanel();
            return;
        }

        ISelectable selectable = list[0];
        if (selectable == null)
        {
            HidePanel();
            return;
        }

        Transform transform = selectable.GetTransform();
        if (transform == null)
        {
            HidePanel();
            return;
        }

        Structure structure = transform.GetComponentInParent<Structure>();
        if (structure != null && structure.structureData.Name.ToLower().Contains("shop"))
        {
            currentStructure = structure;
            ShowPanel();
        }
        else
        {
            HidePanel();
        }
    }

    protected override void OnPanelShown()
    {
        if (shopNameText != null)
        {
            if (currentStructure != null && currentStructure.structureData != null && !string.IsNullOrEmpty(currentStructure.structureData.Name))
            {
                shopNameText.text = currentStructure.structureData.Name;
            }
            else
            {
                shopNameText.text = currentStructure != null ? currentStructure.name : "Shop";
            }
        }

        ClearAllSlotsAndTransactions();
    }

    protected override void OnPanelHidden()
    {
        currentStructure = null;
    }

    public void HidePanelImmediate() => HidePanel();

    private void ClearAllSlotsAndTransactions()
    {
        pendingBuys.Clear();
        pendingSells.Clear();
        pendingItemsById.Clear();

        foreach (ShopItemEntryUI entry in entryInstances)
        {
            entry.ResetQuantity();
        }

        RefreshTexts();
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    private void OnResetClicked()
    {
        ClearAllSlotsAndTransactions();
    }

    private void OnConfirmClicked()
    {
        if (pendingBuys.Count == 0 && pendingSells.Count == 0)
        {
            ClearAllSlotsAndTransactions();
            statusText.text = "You didnt add anything to the trade";
            return;
        }

        int totalBuy = pendingBuys.Sum(pending => pending.Value * GetBuyPriceForItem(pendingItemsById[pending.Key]));
        int totalSell = pendingSells.Sum(pending => pending.Value * GetSellPriceForItem(pendingItemsById[pending.Key]));
        int net = totalSell - totalBuy;

        // Not enough score to pay
        if (totalBuy > 0 && !ScoreManager.Instance.CanSpendScore(net))
        {
            statusText.text = "You dont have enough score to complete the trade";
            return;
        }

        if (totalSell > 0)
        {
            // Check if there is stock of the items to sell:
            foreach (KeyValuePair<string, int> sellPair in pendingSells)
            {
                ItemData itemToSell = pendingItemsById[sellPair.Key];
                int quantityToSell = sellPair.Value;
                int totalAvailable = InventoryManager.Instance != null ? InventoryManager.Instance.GetTotal(itemToSell) : 0;

                if (totalAvailable < quantityToSell)
                {
                    if (statusText != null)
                    {
                        statusText.text = $"Not enough {itemToSell.Name} in stock ({totalAvailable}/{quantityToSell})";
                    }
                    return;
                }
            }

            // Destroy sold items from warehouses:
            if (pendingSells.Count > 0)
            {
                foreach (KeyValuePair<string, int> sellPair in pendingSells)
                {
                    ItemData itemToSell = pendingItemsById[sellPair.Key];
                    int quantity = sellPair.Value;

                    List<GameObject> itemsFound = WarehouseManager.Instance.FindItemsInWarehouses(itemToSell, quantity);
                    if (itemsFound.Count <= 0 || itemsFound == null)
                    {
                        continue;
                    }

                    foreach (GameObject item in itemsFound)
                    {
                        Destroy(item);
                    }
                }
            }

            if (net > 0)
            {
                ScoreManager.Instance.AddScore(net);
            }
        }

        // Create bought items at storage spot
        if (totalBuy > 0 && pendingBuys.Count > 0 && currentStructure != null)
        {
            if (net < 0)
            {
                ScoreManager.Instance.SpendScore(net);
            }

            Transform storageTransform = currentStructure.transform.Find("Storage");

            foreach (KeyValuePair<string, int> pending in pendingBuys)
            {
                ItemData item = pendingItemsById[pending.Key];
                int quantity = pending.Value;
                for (int i = 0; i < quantity; i++)
                {
                    GameObject itemGameObject;
                    if (item != null && item.itemPrefab != null)
                    {
                        itemGameObject = Instantiate(item.itemPrefab, storageTransform);
                        itemGameObject.transform.localPosition = Vector3.zero;
                    }
                }
            }
        }

        ClearAllSlotsAndTransactions();
        statusText.text = "Trade completed, come back soon!";
    }
}