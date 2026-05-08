using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abstract base class for all UI panels in the game.
/// Handles the common boilerplate that every panel needs:
///   - Registering/unregistering with HideElementManager
///   - Pushing/popping the correct InputMode when shown/hidden
///   - Closing on Escape key
///   - Optionally closing when clicking outside the panel
///
/// Subclasses can use virtual hooks: OnPanelReady(), OnPanelShown(), OnPanelHidden()
/// </summary>
public abstract class BasePanel : MonoBehaviour, IHideElement
{
    [SerializeField] protected GameObject panelRoot;

    [Tooltip("Which input mode to push when this panel opens")]
    [SerializeField] private InputManager.InputMode inputModeOnOpen = InputManager.InputMode.UI;

    [Tooltip("If true, clicking anywhere outside the panel will close it")]
    [SerializeField] private bool closeOnClickOutside = true;

    [Tooltip("If true, pressing Cancel key will close the panel")]
    [SerializeField] private bool CloseOnCancelKey = true;

    private bool uiModePushed = false;
    private InputSystem_Actions.UIActions uiActions;
    private bool isReady = false;

    #region Unity methods

    protected virtual void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (HideElementManager.Instance == null
            || InputManager.Instance == null)
        {
            yield return null;
        }

        HideElementManager.Instance.Register(this);

        uiActions = InputManager.Instance.UI;
        isReady = true;

        OnPanelReady();
    }

    protected virtual void OnDisable()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.Unregister(this);
        }

        isReady = false;
    }

    protected virtual void Update()
    {
        if (!isReady || panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        if (CloseOnCancelKey && uiActions.Cancel.WasPressedThisFrame())
        {
            HidePanel();
            return;
        }

        if (closeOnClickOutside && uiActions.Click.WasPressedThisFrame())
        {
            if (!IsPointerInsidePanel())
            {
                HidePanel();
            }
        }
    }

    #endregion

    #region Public interface

    public virtual void TogglePanel()
    {
        if (IsVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    public virtual void ShowPanel()
    {
        if (!isReady)
        {
            Debug.LogWarning($"{name}: called before panel was ready");
            return;
        }

        if (IsVisible)
        {
            return;
        }

        if (panelRoot == null)
        {
            Debug.LogError($"{name}: missing {nameof(panelRoot)}");
            return;
        }

        HideElementManager.Instance.ShowOnly(this);
        panelRoot.SetActive(true);

        if (!uiModePushed)
        {
            InputManager.Instance.PushMode(inputModeOnOpen);
            uiModePushed = true;
        }

        OnPanelShown();
    }

    public virtual void HidePanel()
    {
        if (!IsVisible)
        {
            return;
        }

        if (panelRoot == null)
        {
            Debug.LogError($"{name}: missing {nameof(panelRoot)}");
            return;
        }

        panelRoot.SetActive(false);

        if (uiModePushed)
        {
            InputManager.Instance.PopMode();
            uiModePushed = false;
        }

        OnPanelHidden();
    }

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    #endregion

    #region Hooks

    /// <summary>
    /// Called once after all managers are ready and the panel has registered itself
    /// </summary>
    protected virtual void OnPanelReady() { }

    /// <summary>
    /// Called every time the panel becomes visible
    /// </summary>
    protected virtual void OnPanelShown() { }

    /// <summary>
    /// Called every time the panel is hidden
    /// </summary>
    protected virtual void OnPanelHidden() { }

    #endregion

    #region Private Helpers

    private bool IsPointerInsidePanel()
    {
        if (panelRoot == null)
        {
            return false;
        }

        RectTransform rect = panelRoot.GetComponent<RectTransform>();
        Vector2 pointerPos = uiActions.Point.ReadValue<Vector2>();

        Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, pointerPos, uiCamera);
    }

    #endregion
}