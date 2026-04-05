using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class SelectionController : MonoBehaviour
{
    [Tooltip("Reference to the UI Image used as selection rectangle")]
    public Image selectionBoxImage;

    [Tooltip("Main camera")]
    public Camera targetCamera;

    private Vector2 dragStartScreen;
    private Vector2 dragEndScreen;
    private bool isDragging;

    private RectTransform selectionRectTransform;

    private InputAction pointerAction;
    private InputAction selectAction;
    private InputAction shiftAction;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (selectionBoxImage != null)
        {
            selectionRectTransform = selectionBoxImage.GetComponent<RectTransform>();
            selectionBoxImage.gameObject.SetActive(false);
        }

        pointerAction = new InputAction("Pointer", InputActionType.Value, "<Pointer>/position");
        selectAction = new InputAction("Select", InputActionType.Button);
        selectAction.AddBinding("<Mouse>/leftButton");
        shiftAction = new InputAction("AdditiveSelect", InputActionType.Button);
        shiftAction.AddBinding("<Keyboard>/leftShift");
        shiftAction.AddBinding("<Keyboard>/rightShift");
    }

    private void OnEnable()
    {
        pointerAction?.Enable();
        selectAction?.Enable();
        shiftAction?.Enable();
    }

    private void OnDisable()
    {
        pointerAction?.Disable();
        selectAction?.Disable();
        shiftAction?.Disable();

        isDragging = false;

        if (selectionBoxImage != null)
        {
            selectionBoxImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        pointerAction?.Dispose();
        selectAction?.Dispose();
        shiftAction?.Dispose();
    }

    private void Update()
    {
        if (selectAction == null || pointerAction == null)
        {
            return;
        }

        if (selectAction.WasPressedThisFrame())
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            dragStartScreen = pointerAction.ReadValue<Vector2>();
            dragEndScreen = dragStartScreen;
            isDragging = true;

            if (selectionBoxImage != null)
            {
                selectionBoxImage.gameObject.SetActive(true);
            }
        }

        if (isDragging && selectAction.IsPressed())
        {
            dragEndScreen = pointerAction.ReadValue<Vector2>();
            UpdateSelectionGraphics();
        }

        if (isDragging && selectAction.WasReleasedThisFrame())
        {
            isDragging = false;

            if (selectionBoxImage != null)
            {
                selectionBoxImage.gameObject.SetActive(false);
            }

            float dragDistance = (dragEndScreen - dragStartScreen).magnitude;
            bool additive = shiftAction != null && shiftAction.IsPressed();

            if (dragDistance < 6f)
            {
                HandleClick(dragEndScreen, additive);
            }
            else
            {
                HandleDragSelection(dragStartScreen, dragEndScreen, additive);
            }
        }
    }

    private void UpdateSelectionGraphics()
    {
        if (selectionRectTransform == null)
        {
            return;
        }

        RectTransform parentRect = selectionRectTransform.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Camera uiCamera = null;
        Canvas rootCanvas = selectionBoxImage.canvas;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
        }

        // Convert screen points to local points
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, dragStartScreen, uiCamera, out Vector2 localStart) == false)
        {
            return;
        }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, dragEndScreen, uiCamera, out Vector2 localEnd) == false)
        {
            return;
        }

        Vector2 localMin = Vector2.Min(localStart, localEnd);
        Vector2 localMax = Vector2.Max(localStart, localEnd);
        Vector2 size = localMax - localMin;
        Vector2 center = localMin + size * 0.5f;

        selectionRectTransform.anchoredPosition = center;
        selectionRectTransform.sizeDelta = size;
    }

    private void HandleClick(Vector2 screenPos, bool additive)
    {
        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        int layerMask = ~0;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Try to find a selectable on the collider hit by raycast
            GameObject gameObject = hit.collider.gameObject;
            ISelectable selectable = TryGetSelectableOnGameObject(gameObject);
            if (selectable != null)
            {
                SelectionManager.Instance.Select(selectable, additive);
                return;
            }
        }

        // Nothing was hit, clear selection unless additive
        if (!additive)
        {
            SelectionManager.Instance.ClearSelection();
        }
    }

    private void HandleDragSelection(Vector2 startScreen, Vector2 endScreen, bool additive)
    {
        Rect selectionRect = GetScreenRect(startScreen, endScreen);

        List<ISelectable> matches = new();

        foreach (ISelectable candidate in SelectionManager.Instance.AllSelectables)
        {
            if (candidate == null)
            {
                continue;
            }

            Transform transform = candidate.GetTransform();
            if (transform == null)
            {
                continue;
            }

            Vector3 screenPos = targetCamera.WorldToScreenPoint(transform.position);
            if (screenPos.z < 0f)
            {
                continue;
            }

            if (selectionRect.Contains(screenPos))
            {
                matches.Add(candidate);
            }
        }

        if (matches.Count > 0)
        {
            SelectionManager.Instance.Select(matches, additive);
        }
        else if (!additive)
        {
            SelectionManager.Instance.ClearSelection();
        }
    }

    private Rect GetScreenRect(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return new Rect(min, max - min);
    }

    private ISelectable TryGetSelectableOnGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return null;
        }

        // Try component that implements ISelectable
        if (gameObject.TryGetComponent<ISelectable>(out ISelectable selectable))
        {
            return selectable;
        }

        // Try parents
        Transform transform = gameObject.transform;
        while (transform.parent != null)
        {
            transform = transform.parent;
            selectable = transform.GetComponent<ISelectable>();
            if (selectable != null)
            {
                return selectable;
            }
        }

        return null;
    }
}