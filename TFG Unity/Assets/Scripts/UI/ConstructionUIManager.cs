using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConstructionUIManager : MonoBehaviour
{
    [Header("Data source")]

    [Tooltip("Path inside Resources to load all structures datas")]
    public string structuresPath = "Structures";

    [Tooltip("Structures' list")]
    public List<StructureData> structures = new();

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

    // Public event
    public System.Action<StructureData> OnStructureSelected;

    private void Start()
    {
        LoadStructures();
        BuildCategoryBar();
        ShowCategory(currentCategory);
    }

    private void LoadStructures()
    {
        StructureData[] loaded = Resources.LoadAll<StructureData>(structuresPath);
        structures = loaded.ToList();

        grouped = new Dictionary<StructureCategory, List<StructureData>>();
        foreach (StructureCategory category in System.Enum.GetValues(typeof(StructureCategory)))
        {
            grouped[category] = new List<StructureData>();
        }

        foreach (StructureData structureData in structures)
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
            grouped[category] = grouped[category].OrderBy(x => x.structureName).ToList();
        }
    }

    private void BuildCategoryBar()
    {
        if (categoryBar == null || categoryButtonPrefab == null)
        {
            Debug.LogError($"{name}: categoryBar or categoryButtonPrefab not assigned.");
            return;
        }

        // Clear previous children
        for (int i = categoryBar.childCount - 1; i >= 0; i--)
        {
            Destroy(categoryBar.GetChild(i).gameObject);
        }

        foreach (StructureCategory category in System.Enum.GetValues(typeof(StructureCategory)))
        {
            GameObject gameobject = Instantiate(categoryButtonPrefab, categoryBar);
            if (!gameobject.TryGetComponent<CategoryButton>(out CategoryButton categoryButton))
            {
                Debug.LogError("categoryButtonPrefab missing CategoryButton script.");
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
            Debug.LogError($"{name}: blueprintGrid or blueprintEntryPrefab not assigned.");
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
                Debug.LogError("blueprintEntryPrefab missing BlueprintEntry script.");
                continue;
            }

            // Pass PlacementController.EnterPlacement as the onSelected action
            entry.Setup(structureData, selected =>
            {
                if (PlacementController.Instance == null)
                {
                    Debug.LogError("No PlacementController in scene.");
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
        panelRoot.SetActive(!panelRoot.activeSelf);
    }
}