using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OrdersController : MonoBehaviour
{
    public LayerMask groundLayerMask = 1 << 0;

    private InputSystem_Actions.GameplayActions gameplayActions;
    private bool inputReady;

    #region Unity methods

    private void OnEnable()
    {
        StartCoroutine(RegisterWhenReady());
    }

    private IEnumerator RegisterWhenReady()
    {
        while (InputManager.Instance == null)
        {
            yield return null;
        }

        gameplayActions = InputManager.Instance.Gameplay;
        inputReady = true;
    }

    private void OnDisable()
    {
        inputReady = false;
    }

    private void Update()
    {
        if (!inputReady)
        {
            return;
        }

        if (!gameplayActions.ContextAction.WasPressedThisFrame())
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mousePosition = InputManager.Instance.PointerPosition;
        if (Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            return;
        }

        Vector3 targetPosition = hit.point;

        IReadOnlyList<ISelectable> selection = SelectionManager.Instance.CurrentSelection;
        if (selection == null || selection.Count == 0)
        {
            return;
        }

        List<IOrderable> selectionObjects = new();
        foreach (ISelectable selectable in selection)
        {
            if (selectable is Component component
                && component.TryGetComponent<IOrderable>(out IOrderable orderableComponent))
            {
                selectionObjects.Add(orderableComponent);
            }
        }

        if (selectionObjects.Count == 0)
        {
            return;
        }

        MoveOrderOptions options = MoveOrderOptions.Default;

        if (gameplayActions.MultiSelect.IsPressed())
        {
            options.returnToGuard = false;
        }

        if (gameplayActions.Modifier.IsPressed())
        {
            options.attackMove = false;
        }

        OrderManager.Instance.IssueMoveOrder(selectionObjects, targetPosition, options);
    }

    #endregion
}