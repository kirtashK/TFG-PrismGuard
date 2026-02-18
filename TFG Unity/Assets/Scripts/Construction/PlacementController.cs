using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance { get; private set; }

    [Header("Layers & masks")]
    [Tooltip("Layers that are considered floor for position raycast")]
    public LayerMask groundLayerMask = 1 << 0;

    [Tooltip("Layers that avoid placement")]
    public LayerMask placementObstacleMask = ~0;

    [Header("Preview visuals")]

    [Tooltip("If there is no previewPrefab, use builtPrefab for preview (true) or blueprintPrefab (false)")]
    public bool useBuiltPrefabForPreview = true;

    [Tooltip("Material for valid preview")]
    public Material previewMaterialValid;

    [Tooltip("Material for invalid preview")]
    public Material previewMaterialInvalid;

    [Tooltip("Vertical offset of preview")]
    public float previewYOffset = 0.06f;

    [Header("Raycast / validation")]

    public float boundsPaddingMultiplier = 1.05f;

    public bool instantiateBlueprintOnConfirm = true;

    private GameObject previewPrefab;
    private StructureData currentStructure;
    private bool isPlacing;
    private bool lastValidState;
    private static readonly Collider[] overlapBuffer = new Collider[32];

    private readonly List<Renderer> previewRenderers = new();

    private string placementErrorMessage;

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
            if (previewPrefab != null)
            {
                previewPrefab.transform.position = position + Vector3.up * previewYOffset;

                bool valid = ValidatePlacement(previewPrefab);
                UpdatePreviewVisual(valid);
                lastValidState = valid;

                if (!valid && !string.IsNullOrEmpty(placementErrorMessage))
                {
                    if (TooltipController.Instance != null)
                    {
                        TooltipController.Instance.Show(placementErrorMessage);
                    }
                }
                else
                {
                    if (TooltipController.Instance != null)
                    {
                        TooltipController.Instance.Hide();
                    }
                }
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
        placementErrorMessage = null;

        GameObject previewPrefab;

        // Use the preview if it exists, otherwise use the fallback but it will cause problems with scripts being active...
        if (structureData.previewPrefab != null)
        {
            previewPrefab = structureData.previewPrefab;
        }
        else
        {
            // Fallback
            previewPrefab = useBuiltPrefabForPreview
                ? (structureData.builtPrefab != null ? structureData.builtPrefab : structureData.blueprintPrefab)
                : (structureData.blueprintPrefab != null ? structureData.blueprintPrefab : structureData.builtPrefab);
        }

        if (previewPrefab == null)
        {
            Debug.LogError($"PlacementController: no prefab available for preview in {structureData.structureName}");
            CancelPlacement();
            return;
        }

        this.previewPrefab = Instantiate(previewPrefab, Vector3.zero, Quaternion.identity);

        previewRenderers.Clear();
        foreach (Renderer renderer in this.previewPrefab.GetComponentsInChildren<Renderer>())
        {
            previewRenderers.Add(renderer);
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

        UpdatePreviewVisual(false);

        // Deactivate preview colliders
        foreach (Collider collider in this.previewPrefab.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        foreach(NavMeshObstacle obstacle in this.previewPrefab.GetComponentsInChildren<NavMeshObstacle>())
        {
            obstacle.enabled = false;
        }
    }

    private bool ValidatePlacement(GameObject preview)
    {
        placementErrorMessage = null;

        if (preview == null)
        {
            return false;
        }

        Bounds combined = new(preview.transform.position, Vector3.zero);
        bool anyRenderer = false;
        foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>())
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
            combined = new Bounds(preview.transform.position, Vector3.one * 1f);
        }

        Vector3 center = combined.center;
        Vector3 halfExtents = combined.extents * boundsPaddingMultiplier;
        Quaternion rotation = preview.transform.rotation;

        // Check if there are obstacles
        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, rotation, placementObstacleMask, QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            if (currentStructure != null && currentStructure.isUnique)
            {
                Structure[] existing = FindObjectsByType<Structure>(FindObjectsSortMode.None);
                foreach (Structure structure in existing)
                {
                    if (structure == null || structure.structureData == null)
                    {
                        continue;
                    }
                    if (structure.structureData == currentStructure)
                    {
                        placementErrorMessage = $"Only one {currentStructure.structureName} can exist";
                        return false;
                    }
                }
            }

            placementErrorMessage = null;
            return true;
        }

        // Filter hits: ignore preview's own children
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = overlapBuffer[i];

            if (collider == null || collider.transform.IsChildOf(preview.transform))
            {
                continue;
            }

            placementErrorMessage = "Area obstructed";
            return false;
        }

        placementErrorMessage = null;
        return true;
    }

    private void UpdatePreviewVisual(bool valid)
    {
        Material targetMaterial = valid ? previewMaterialValid : previewMaterialInvalid;

        foreach (Renderer renderer in previewRenderers)
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
        if (!isPlacing || currentStructure == null || previewPrefab == null)
        {
            return;
        }

        GameObject blueprintPrefab = currentStructure.blueprintPrefab;
        if (instantiateBlueprintOnConfirm && blueprintPrefab != null)
        {
            previewPrefab.transform.position -= Vector3.up * previewYOffset;
            Instantiate(blueprintPrefab, previewPrefab.transform.position, previewPrefab.transform.rotation);
        }
        else
        {
            if (currentStructure.builtPrefab != null)
            {
                Instantiate(currentStructure.builtPrefab, previewPrefab.transform.position, previewPrefab.transform.rotation);
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
        if (previewPrefab != null)
        {
            Destroy(previewPrefab);
        }

        currentStructure = null;
        isPlacing = false;
        previewRenderers.Clear();

        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Hide();
        }
    }

    private void OnDisable()
    {
        EndPlacement();
    }

    // Draw a gizmo in the editor to check the bounds:
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (previewPrefab != null)
        {
            DrawBoundsForGameObject(previewPrefab, Color.cyan, "Preview bounds");
            return;
        }
    }

    private void DrawBoundsForGameObject(GameObject gameObject, Color color, string label)
    {
        if (gameObject == null)
        {
            return;
        }

        Bounds combined = CalculateCombinedBounds(gameObject);
        if (combined.size == Vector3.zero)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(combined.center, combined.size);

        float size = Mathf.Max(0.2f, Mathf.Min(Mathf.Min(combined.size.x, combined.size.y, combined.size.z) * 0.1f, 1f));
        Gizmos.DrawLine(combined.center - Vector3.up * size, combined.center + Vector3.up * size);
        Gizmos.DrawLine(combined.center - Vector3.right * size, combined.center + Vector3.right * size);
        Gizmos.DrawLine(combined.center - Vector3.forward * size, combined.center + Vector3.forward * size);

        Handles.color = color;
        Vector3 labelPos = combined.center + Vector3.up * (combined.extents.y + 0.3f);
        Handles.Label(labelPos, $"{label}\nSize: {combined.size.x:F2} × {combined.size.y:F2} × {combined.size.z:F2}");
    }

    private Bounds CalculateCombinedBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.zero);
        }

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
        }

        return combined;
    }
#endif
}