using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ConstructionUIManager : MonoBehaviour, IHideElement
{

    [Tooltip("Label used in Addressables for StructureData")]
    public string structureLabel = "Structure";

    public List<StructureData> allStructures = new();

    private AsyncOperationHandle<IList<StructureData>> loadHandle;
    private bool isLoaded = false;

    public event Action<List<StructureData>> OnStructureLoaded;

    [Header("UI refs")]
    [Tooltip("Parent transform where category buttons will be created")]
    public RectTransform categoryBar;

    [Tooltip("Parent transform where entries will be created")]
    public RectTransform blueprintGrid;

    public GameObject categoryButtonPrefab;

    public GameObject blueprintEntryPrefab;

    public GameObject panelRoot;

    private Dictionary<StructureCategory, List<StructureData>> grouped = new();
    private StructureCategory currentCategory = StructureCategory.Storage;

    public Action<StructureData> OnStructureSelected;

    private InputSystem_Actions.UIActions uiActions;
    private bool uiModePushed;
    private bool inputReady;

    #region Unity methods

    private void Start()
    {
        StartCoroutine(LoadStructures());
        BuildCategoryBar();
        ShowCategory(currentCategory);
    }

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (HideElementManager.Instance == null
            || ResearchManager.Instance == null
            || InputManager.Instance == null
            || PlacementController.Instance == null)
        {
            yield return null;
        }

        HideElementManager.Instance.Register(this);
        ResearchManager.Instance.OnEffectApplied += HandleStructureUnlocked;
        uiActions = InputManager.Instance.UI;
        inputReady = true;
    }

    private void OnDisable()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }
        ResearchManager.Instance.OnEffectApplied -= HandleStructureUnlocked;

        inputReady = false;
    }

    private void OnDestroy()
    {
        if (isLoaded && loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
            allStructures.Clear();
            isLoaded = false;
        }
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
    }

    #endregion

    private IEnumerator LoadStructures()
    {
        if (isLoaded)
        {
            yield break;
        }

        loadHandle = Addressables.LoadAssetsAsync<StructureData>(
            structureLabel,
            structureData => { }
        );

        yield return loadHandle;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            allStructures = new List<StructureData>(loadHandle.Result);
            isLoaded = true;
            
            grouped = new Dictionary<StructureCategory, List<StructureData>>();
            foreach (StructureCategory category in Enum.GetValues(typeof(StructureCategory)))
            {
                grouped[category] = new List<StructureData>();
            }

            foreach (StructureData structureData in allStructures)
            {
                if (structureData == null)
                {
                    continue;
                }
                if (!grouped.ContainsKey(structureData.category))
                {
                    grouped[structureData.category] = new List<StructureData>();
                }
                grouped[structureData.category].Add(structureData);
            }

            // Sort by name
            foreach (StructureCategory category in grouped.Keys.ToList())
            {
                grouped[category] = grouped[category].OrderBy(structure => structure.Name).ToList();
            }

            // Notify listeners
            OnStructureLoaded?.Invoke(allStructures);

            PopulateBlueprintGrid(grouped.ContainsKey(currentCategory) ? grouped[currentCategory] : new List<StructureData>());
        }
        else
        {
            Debug.LogWarning($"{name} failed to load {nameof(StructureData)} addressables");
        }
    }

    public void HandleStructureUnlocked(ResearchManager.ResearchEffectEvent researchEffect)
    {
        if (researchEffect.effectType != ResearchManager.ResearchEffectEvent.EffectType.UnlockStructure)
        {
            return;
        }

        // Find the corresponding entry in the grid and update its visuals
        foreach (Transform child in blueprintGrid)
        {
            if (child.TryGetComponent<BlueprintEntry>(out BlueprintEntry entry))
            {
                if (entry.structureData != null && entry.structureData.id == researchEffect.targetId)
                {
                    entry.RefreshLockVisual();
                    break;
                }
            }
        }
    }

    private void BuildCategoryBar()
    {
        if (categoryBar == null || categoryButtonPrefab == null)
        {
            Debug.LogError($"{name}: {nameof(categoryBar)} or {nameof(categoryButtonPrefab)} not assigned.");
            return;
        }

        // Clear previous children
        for (int i = categoryBar.childCount - 1; i >= 0; i--)
        {
            Destroy(categoryBar.GetChild(i).gameObject);
        }

        foreach (StructureCategory category in Enum.GetValues(typeof(StructureCategory)))
        {
            GameObject gameobject = Instantiate(categoryButtonPrefab, categoryBar);
            if (!gameobject.TryGetComponent<CategoryButton>(out CategoryButton categoryButton))
            {
                Debug.LogError($"{name}: {nameof(categoryButtonPrefab)} missing {nameof(CategoryButton)} script");
                continue;
            }

            categoryButton.Setup(category, OnCategoryClicked);
        }
    }

    private void OnCategoryClicked(StructureCategory category)
    {
        ShowCategory(category);
    }

    private void ShowCategory(StructureCategory category)
    {
        currentCategory = category;
        PopulateBlueprintGrid(grouped.ContainsKey(category) ? grouped[category] : new List<StructureData>());
    }

    private void PopulateBlueprintGrid(List<StructureData> list)
    {
        if (blueprintGrid == null || blueprintEntryPrefab == null)
        {
            Debug.LogError($"{name}: {nameof(blueprintGrid)} or {nameof(blueprintEntryPrefab)} not assigned");
            return;
        }

        // Clear previous entries
        for (int i = blueprintGrid.childCount - 1; i >= 0; i--)
        {
            Destroy(blueprintGrid.GetChild(i).gameObject);
        }

        foreach (StructureData structureData in list)
        {
            GameObject gameobject = Instantiate(blueprintEntryPrefab, blueprintGrid);
            if (!gameobject.TryGetComponent<BlueprintEntry>(out BlueprintEntry entry))
            {
                Debug.LogError($"{name}: {nameof(blueprintEntryPrefab)} missing {nameof(BlueprintEntry)}");
                continue;
            }

            // Pass PlacementController.EnterPlacement as the onSelected action
            entry.Setup(structureData, selected =>
            {
                if (PlacementController.Instance == null)
                {
                    Debug.LogError($"{name}: missing {nameof(PlacementController)}");
                    return;
                }

                PlacementController.Instance.EnterPlacement(selected);
            });
        }
    }

    // Fired by button press
    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            return;
        }
        // If not active, hide other elements then get activated
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

        PlacementController.Instance.CancelPlacement();

        if (uiModePushed)
        {
            InputManager.Instance.PopMode();
            uiModePushed = false;
        }
    }
}