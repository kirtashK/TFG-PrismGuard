using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Shows a tooltip on UI with the provided text around the pointer
/// </summary>
public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    public RectTransform tooltipRect;

    public TextMeshProUGUI tooltipText;

    public Canvas parentCanvas;

    [Tooltip("Inner padding of tooltip")]
    public Vector2 tooltipPadding = new(8f, 6f);

    [Tooltip("Maximum tooltip width (0 = unlimited)")]
    public float maxTooltipWidth = 400f;

    [Tooltip("Padding to keep tooltip within screen")]
    public Vector2 screenPadding = new(8f, 8f);

    [Tooltip("Should the tooltip follow the mouse?")]
    public bool followMouse = true;

    private CanvasGroup canvasGroup;
    private Camera uiCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipRect == null || tooltipText == null)
        {
            Debug.LogError("TooltipController: missing components");
            enabled = false;
            return;
        }

        canvasGroup = tooltipRect.GetComponent<CanvasGroup>();

        // Keep the camera
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera || parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            uiCamera = parentCanvas.worldCamera;
        }
        else
        {
            uiCamera = null;
        }

        Hide();
    }

    private void Update()
    {
        if (canvasGroup != null && canvasGroup.alpha > 0f && followMouse)
        {
            UpdatePositionToMouse();
        }
    }

    private void UpdatePositionToMouse()
    {
        Vector2 mousePos = Pointer.current != null
            ? Pointer.current.position.ReadValue()
            : Vector2.zero;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // Convert screen point to Canvas local point
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, uiCamera, out Vector2 localPoint))
        {
            Vector2 tooltipSize = tooltipRect.rect.size;

            const float horizontalOffset = 12f;
            const float verticalOffset = 8f;

            float pivotToTop = tooltipSize.y * (1f - tooltipRect.pivot.y);

            float anchoredY = localPoint.y + pivotToTop + verticalOffset;
            float anchoredX = localPoint.x + horizontalOffset;
            Vector2 anchored = new(anchoredX, anchoredY);

            // Clamp so tooltip stays inside canvasRect with screenPadding
            Vector2 half = canvasRect.rect.size * 0.5f;

            float minX = -half.x + screenPadding.x + tooltipSize.x * tooltipRect.pivot.x;
            float maxX = half.x - screenPadding.x - tooltipSize.x * (1f - tooltipRect.pivot.x);
            float minY = -half.y + screenPadding.y + tooltipSize.y * tooltipRect.pivot.y;
            float maxY = half.y - screenPadding.y - tooltipSize.y * (1f - tooltipRect.pivot.y);

            anchored.x = Mathf.Clamp(anchored.x, minX, maxX);
            anchored.y = Mathf.Clamp(anchored.y, minY, maxY);

            tooltipRect.anchoredPosition = anchored;
        }
    }

    public void Show(string text)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        tooltipText.text = text;

        float availableWidth = (maxTooltipWidth > 0f) ? Mathf.Max(1f, maxTooltipWidth - tooltipPadding.x * 2f) : 10000f;

        Vector2 preferred = tooltipText.GetPreferredValues(text, availableWidth, 0f);

        // Padding
        Vector2 finalSize = new(preferred.x + tooltipPadding.x * 2f, preferred.y + tooltipPadding.y * 2f);

        tooltipRect.sizeDelta = finalSize;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        UpdatePositionToMouse();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.Hide();
        }
    }
}