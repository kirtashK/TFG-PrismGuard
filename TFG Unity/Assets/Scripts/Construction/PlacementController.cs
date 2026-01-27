using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance { get; private set; }

    [Header("Layers & masks")]
    [Tooltip("Layers that are considered floor for position raycast")]
    public LayerMask groundLayerMask = 1 << 0;

    [Tooltip("Layers that avoid placement")]
    public LayerMask placementObstacleMask = ~0;

    [Header("Ghost visuals")]

    [Tooltip("If there is no previewPrefab, use builtPrefab for preview (true) or blueprintPrefab (false)")]
    public bool useBuiltPrefabForGhost = true;

    [Tooltip("Material for valid ghost")]
    public Material ghostMaterialValid;

    [Tooltip("Material for invalid ghost")]
    public Material ghostMaterialInvalid;

    [Tooltip("Vertical offset of ghost")]
    public float ghostYOffset = 0.06f;

    [Header("Raycast / validation")]

    public float boundsPaddingMultiplier = 1.05f;

    public bool instantiateBlueprintOnConfirm = true;

    private GameObject ghostInstance;
    private StructureData currentStructure;
    private bool isPlacing;
    private bool lastValidState;
    private static readonly Collider[] overlapBuffer = new Collider[32];

    private readonly List<Renderer> ghostRenderers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!isPlacing)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Vector3 position = hit.point;
            if (ghostInstance != null)
            {
                ghostInstance.transform.position = position + Vector3.up * ghostYOffset;

                bool valid = ValidatePlacement(ghostInstance);
                UpdateGhostVisual(valid);
                lastValidState = valid;
            }
        }

        // Confirm (left click)
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                if (lastValidState)
                {
                    ConfirmPlacement();
                }
            }
        }
        // Cancel (right click / Esc)
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    public void EnterPlacement(StructureData structureData)
    {
        if (structureData == null)
        {
            Debug.LogError("PlacementController.EnterPlacement received null StructureData");
            return;
        }

        // Cancel previous placement
        if (isPlacing)
        {
            CancelPlacement();
        }

        currentStructure = structureData;
        isPlacing = true;
        lastValidState = false;

        GameObject prefabForGhost;

        // Use the preview if it exists, otherwise use the fallback but it will cause problems with scripts being active...
        if (structureData.previewPrefab != null)
        {
            prefabForGhost = structureData.previewPrefab;
        }
        else
        {
            // Fallback
            prefabForGhost = useBuiltPrefabForGhost
                ? (structureData.builtPrefab != null ? structureData.builtPrefab : structureData.blueprintPrefab)
                : (structureData.blueprintPrefab != null ? structureData.blueprintPrefab : structureData.builtPrefab);
        }

        if (prefabForGhost == null)
        {
            Debug.LogError($"PlacementController: no prefab available for ghost in {structureData.structureName}");
            CancelPlacement();
            return;
        }

        ghostInstance = Instantiate(prefabForGhost, Vector3.zero, Quaternion.identity);

        ghostRenderers.Clear();
        foreach (Renderer renderer in ghostInstance.GetComponentsInChildren<Renderer>())
        {
            ghostRenderers.Add(renderer);
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(materials[i]);
                Color color = materials[i].color;
                color.a = 0.65f;
                materials[i].color = color;
            }
            renderer.materials = materials;
        }

        UpdateGhostVisual(false);

        // Deactivate ghost colliders
        foreach (Collider collider in ghostInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
    }

    private bool ValidatePlacement(GameObject ghost)
    {
        if (ghost == null)
        {
            return false;
        }

        Bounds combined = new(ghost.transform.position, Vector3.zero);
        bool anyRenderer = false;
        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            if (!anyRenderer)
            {
                combined = renderer.bounds;
                anyRenderer = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }
        if (!anyRenderer)
        {
            combined = new Bounds(ghost.transform.position, Vector3.one * 1f);
        }

        Vector3 center = combined.center;
        Vector3 halfExtents = combined.extents * boundsPaddingMultiplier;
        Quaternion rotation = ghost.transform.rotation;

        // Check if there are obstacles
        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, rotation, placementObstacleMask, QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return true;
        }

        // filter hits: ignore ghost's own children
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = overlapBuffer[i];

            if (collider == null || collider.transform.IsChildOf(ghost.transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void UpdateGhostVisual(bool valid)
    {
        Material targetMaterial = valid ? ghostMaterialValid : ghostMaterialInvalid;

        foreach (Renderer renderer in ghostRenderers)
        {
            Material[] newMats = new Material[renderer.materials.Length];
            for (int i = 0; i < newMats.Length; i++)
            {
                newMats[i] = new Material(targetMaterial);
            }
            renderer.materials = newMats;
        }
    }

    private void ConfirmPlacement()
    {
        if (!isPlacing || currentStructure == null || ghostInstance == null)
        {
            return;
        }

        GameObject blueprintPrefab = currentStructure.blueprintPrefab;
        if (instantiateBlueprintOnConfirm && blueprintPrefab != null)
        {
            Instantiate(blueprintPrefab, ghostInstance.transform.position, ghostInstance.transform.rotation);
        }
        else
        {
            if (currentStructure.builtPrefab != null)
            {
                Instantiate(currentStructure.builtPrefab, ghostInstance.transform.position, ghostInstance.transform.rotation);
            }
        }

        EndPlacement();
    }

    public void CancelPlacement()
    {
        if (!isPlacing)
        {
            return;
        }

        EndPlacement();
    }

    private void EndPlacement()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }

        currentStructure = null;
        isPlacing = false;
        ghostRenderers.Clear();
    }

    private void OnDisable()
    {
        EndPlacement();
    }
}