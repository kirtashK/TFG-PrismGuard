using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{
    public static PlacementController Instance { get; private set; }

    [Header("Layers & masks")]

    [Tooltip("Layers that are considered floor for position raycast")]
    public LayerMask groundLayerMask = 1 << 0;

    [Tooltip("Layers that avoid placement")]
    public LayerMask placementObstacleMask = ~0;

    [Header("Visuals")]

    private float previewYOffset;

    [Tooltip("Material for valid preview")]
    public Material previewMaterialValid;

    [Tooltip("Material for invalid preview")]
    public Material previewMaterialInvalid;

    [Tooltip("Extra distance between preview's base and the ground")]
    public float previewVerticalPadding = 0.01f;

    [Tooltip("Material for blueprint, duh")]
    public Material blueprintMaterial;

    [Header("Raycast / validation")]

    public bool instantiateBlueprintOnConfirm = true;

    private readonly float rotationStep = 45f;
    private float previewRotationY;
    private Quaternion previewBaseRotation = Quaternion.identity;

    private GameObject previewPrefab;
    private StructureData currentStructure;
    private bool isPlacing;
    private bool lastValidState;
    private static readonly Collider[] overlapBuffer = new Collider[32];

    private string placementErrorMessage;

    private InputAction pointerAction;
    private InputAction confirmAction;
    private InputAction cancelAction;
    private InputAction rotateAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (previewMaterialValid == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(previewMaterialValid)}");
        }
        if (previewMaterialInvalid == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(previewMaterialInvalid)}");
        }
        if (blueprintMaterial == null)
        {
            Debug.LogWarning($"{name}: missing {nameof(blueprintMaterial)}");
        }

        pointerAction = new InputAction("Pointer", InputActionType.Value, "<Pointer>/position");
        confirmAction = new InputAction("Confirm", InputActionType.Button);
        confirmAction.AddBinding("<Mouse>/leftButton");
        cancelAction = new InputAction("Cancel", InputActionType.Button);
        cancelAction.AddBinding("<Mouse>/rightButton");
        cancelAction.AddBinding("<Keyboard>/escape");
        rotateAction = new InputAction("RotatePreview", InputActionType.Button);
        rotateAction.AddBinding("<Keyboard>/r");
    }
    private void OnEnable()
    {
        pointerAction?.Enable();
        confirmAction?.Enable();
        cancelAction?.Enable();
        rotateAction?.Enable();
    }

    private void OnDisable()
    {
        pointerAction?.Disable();
        confirmAction?.Disable();
        cancelAction?.Disable();
        rotateAction?.Disable();

        EndPlacement();
    }

    private void OnDestroy()
    {
        pointerAction?.Dispose();
        confirmAction?.Dispose();
        cancelAction?.Dispose();
        rotateAction?.Dispose();
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

        Vector2 mousePos = pointerAction != null ? pointerAction.ReadValue<Vector2>() : Pointer.current.position.ReadValue();
        Vector3 screenPoint = new(mousePos.x, mousePos.y, 0f);

        Ray ray = Camera.main.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Vector3 ground = hit.point;
            if (previewPrefab != null)
            {
                previewYOffset = AdjustVerticalPositionToGround(previewPrefab, ground);

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

        if (rotateAction != null && rotateAction.triggered)
        {
            RotatePreview();
        }

        // Confirm (left click)
        if (confirmAction != null && confirmAction.triggered)
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                if (lastValidState)
                {
                    ConfirmPlacement();
                }
            }
        }

        // Cancel (right click or Escape)
        if (cancelAction != null && cancelAction.triggered)
        {
            CancelPlacement();
        }
    }

    public void EnterPlacement(StructureData structureData)
    {
        if (structureData == null)
        {
            Debug.LogError($"{name}: null {nameof(StructureData)}");
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

        previewRotationY = 0f;
        previewBaseRotation = structureData.previewPrefab != null
            ? structureData.previewPrefab.transform.rotation
            : Quaternion.identity;

        previewPrefab = Instantiate(structureData.previewPrefab, Vector3.zero, previewBaseRotation);

        UpdatePreviewVisual(false);

        foreach (Collider collider in previewPrefab.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        foreach(NavMeshObstacle obstacle in previewPrefab.GetComponentsInChildren<NavMeshObstacle>())
        {
            obstacle.carving = false;
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
        Vector3 halfExtents = combined.extents;
        Quaternion rotation = preview.transform.rotation;

        // Check if there are obstacles
        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, overlapBuffer, rotation, placementObstacleMask, QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            if (currentStructure != null && currentStructure.isUnique)
            {
                Structure[] existing = FindObjectsByType<Structure>();
                foreach (Structure structure in existing)
                {
                    if (structure == null || structure.structureData == null)
                    {
                        continue;
                    }
                    if (structure.structureData == currentStructure)
                    {
                        placementErrorMessage = $"Only one {currentStructure.Name} can exist";
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
        ApplyMaterialToObject(previewPrefab, targetMaterial);
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
            GameObject blueprintObject = Instantiate(blueprintPrefab, previewPrefab.transform.position, previewPrefab.transform.rotation);

            if (blueprintMaterial != null)
            {
                ApplyMaterialToObject(blueprintObject, blueprintMaterial);
            }

            if (blueprintObject.TryGetComponent<Blueprint>(out Blueprint blueprint))
            {
                blueprint.enabled = true;
            }
        }
        else
        {
            if (currentStructure.builtPrefab != null)
            {
                Instantiate(currentStructure.builtPrefab, previewPrefab.transform.position, previewPrefab.transform.rotation);
            }
        }

        // If not building defenses or storage, end placement, otherwise can keep placing without having to reselect
        if (currentStructure.category != StructureCategory.Defense 
            && currentStructure.category != StructureCategory.Storage)
        {
            EndPlacement();
        }
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

        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Hide();
        }
    }

    private void RotatePreview()
    {
        if (!isPlacing || previewPrefab == null)
        {
            return;
        }

        previewRotationY = (previewRotationY + rotationStep) % 360f;
        previewPrefab.transform.rotation = previewBaseRotation * Quaternion.Euler(0f, previewRotationY, 0f);
    }

    /// <summary>
    /// Applies targetMaterial to all materials of root with an optional alpha
    /// </summary>
    /// <param name="root">GameObject to modify, including children</param>
    /// <param name="targetMaterial">Material to apply</param>
    /// <param name="alpha"></param>
    private void ApplyMaterialToObject(GameObject root, Material targetMaterial, float alpha = 1f)
    {
        if (root == null || targetMaterial == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = new(targetMaterial);
                Color color = material.color;
                color.a = alpha;
                material.color = color;
                materials[i] = material;
            }
            renderer.materials = materials;
        }
    }

    private float AdjustVerticalPositionToGround(GameObject preview, Vector3 groundPoint)
    {
        preview.transform.position = groundPoint;
        Bounds combined = CalculateCombinedBounds(preview);

        float minY = combined.min.y;
        float desiredBaseY = groundPoint.y + previewVerticalPadding;
        float deltaY = desiredBaseY - minY;
        preview.transform.position = preview.transform.position + Vector3.up * deltaY;

        return deltaY;
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