using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // If left mouse button is held, player is trying to select
        if (Input.GetMouseButtonDown(0))
        {
            dragStartScreen = Input.mousePosition;
            isDragging = true;
            if (selectionBoxImage != null)
            {
                selectionBoxImage.gameObject.SetActive(true);
            }
        }

        // While dragging update selection area
        if (isDragging)
        {
            dragEndScreen = Input.mousePosition;
            UpdateSelectionGraphics();
        }

        // On release decide click or drag
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (selectionBoxImage != null)
            {
                selectionBoxImage.gameObject.SetActive(false);
            }

            float dragDistance = (dragEndScreen - dragStartScreen).magnitude;

            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (dragDistance < 6f)
            {
                // Treat as click
                HandleClick(dragEndScreen, additive);
            }
            else
            {
                // Drag selection
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