using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Shows a tooltip on UI with the provided text around the pointer
/// </summary>
public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    public RectTransform tooltipRect;

    public TextMeshProUGUI tooltipText;

    public Canvas parentCanvas;

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

        HideImmediate();
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
        Vector2 mousePos = Input.mousePosition;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // Convert screen point to Canvas local point
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, uiCamera, out Vector2 localPoint))
        {
            Vector2 anchored = localPoint;
            Vector2 half = canvasRect.rect.size * 0.5f;

            Vector2 tooltipSize = tooltipRect.rect.size;

            anchored += new Vector2(12f, -12f);

            // Clamp so tooltip stays inside canvasRect with screenPadding
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
        tooltipRect.ForceUpdateRectTransforms();
        UpdatePositionToMouse();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
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

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}